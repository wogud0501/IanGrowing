"""Mask sensitive substrings before any developer spool write. Keep line terminators and stack frames."""

import ipaddress
import re

MASK = "[redacted]"
FLAGS = re.IGNORECASE
PRIVATE = re.compile(
    r"-----BEGIN [A-Z0-9 ]*PRIVATE KEY-----[\s\S]*?(?:-----END [A-Z0-9 ]*PRIVATE KEY-----|\Z)"
)
HEADER = re.compile(
    r"(\b(?:authorization|proxy-authorization|set-cookie|cookie)\s*[:=][ \t]*)[^\r\n]+", FLAGS
)
FIELD = re.compile(
    r"""(["']?(?:access[_-]?token|refresh[_-]?token|api[_-]?key|secret|password|passwd|token|e-?mail|phone|crashreporter[ _-]?key|anonymous[ _-]?uuid|incident[ _-]?(?:id|identifier)|device[ _-]?(?:id|identifier|serial)|serial[ _-]?number|host[ _-]?name|machine[ _-]?id|user[ _-]?(?:id|name|input)|account[_-]?id|chat|payment|card[_-]?number|address|gps|latitude|longitude|비밀번호|전화번호|이메일|사용자입력)["']?[ \t]*[:=][ \t]*)(?:"(?:\\.|[^"\\\r\n])*"|'(?:\\.|[^'\\\r\n])*'|[^\r\n,;&}]+)""",
    FLAGS,
)
CREDENTIAL = re.compile(
    r"""\b(?:Bearer[ \t]+[^\s,;]+|ian2\.[^\s"',;]+|(?:sk[-_]|gh[pousr]_|AKIA|eyJ)[A-Za-z0-9_./+=-]{12,})""",
    FLAGS,
)
HOME = re.compile(r"((?:/Users/|/home/|[A-Z]:[\\/]+Users[\\/]+))[^/\\:\r\n\"')]+", FLAGS)
EMAIL = re.compile(r"(?<![\w.+%-])[\w.+%-]{1,128}@[\w.-]{1,189}\.[a-z]{2,24}", FLAGS)
URL = re.compile(r"""(https?://)[^/\s@]+:[^/\s@]+@|([?&][^=\s&]{1,80}=)[^&\s"']+""", FLAGS)
NET = re.compile(
    r"(?<![\w:.])(?:[\da-f]{0,4}:){2,7}[\da-f.]{0,15}(?![\w:.])|(?<![\w.])(?:\d{1,3}\.){3}\d{1,3}(?![\w.])",
    FLAGS,
)
MAC = re.compile(r"(?<!\w)(?:[\da-f]{2}[:-]){5}[\da-f]{2}\b", FLAGS)
PHONE = re.compile(
    r"(?<!\w)(?:(?:\+82[- .]?|0)1[016789][- .]?\d{3,4}[- .]?\d{4}|\+1[- .]?\(?\d{3}\)?[- .]?\d{3}[- .]?\d{4})(?!\d)"
)
CARD = re.compile(r"(?<![\w.-])\d(?:[ -]?\d){12,18}(?![\w.-])")


def hide_ip(match):
    try:
        ipaddress.ip_address(match.group())
    except ValueError:
        return match.group()
    return MASK


def hide_card(match):
    digits = [int(x) for x in match.group() if x.isdigit()][::-1]
    return (
        MASK
        if sum(x if i % 2 == 0 else (2 * x if x < 5 else 2 * x - 9) for i, x in enumerate(digits)) % 10 == 0
        else match.group()
    )


def clean(value, secret=None):
    if secret:
        value = value.replace(secret, MASK)
    value = PRIVATE.sub("[redacted private key]", value)
    for pattern in (HEADER, FIELD, HOME):
        value = pattern.sub(lambda m: m[1] + MASK, value)
    value = CREDENTIAL.sub(MASK, value)
    value = URL.sub(lambda m: (m[1] or m[2]) + MASK, value)
    if "@" in value:
        value = EMAIL.sub(MASK, value)
    value = MAC.sub(MASK, value)
    value = NET.sub(hide_ip, value)
    return CARD.sub(hide_card, PHONE.sub(MASK, value))
