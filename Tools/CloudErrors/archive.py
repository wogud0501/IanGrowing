"""Private HTTPS ingest and bounded, authenticated R2 reads. No public read route."""

import datetime as dt
import functools
import hashlib
import json
import os
import re
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

ORIGIN = "https://iangrowing-error-api.iangrowing-error-api.workers.dev"
PROJECT = Path(os.getenv("IAN_WORKER_PROJECT", Path.home() / "iangrowing-error-cloud"))
CONFIG = Path(os.getenv("IAN_ERRORS_CONFIG", Path.home() / ".config/ian-errors"))
SOURCES = (
    "android",
    "ios",
    "windows",
    "osxplayer",
    "osxeditor",
    "windowseditor",
    "linuxeditor",
    "linuxplayer",
    "editor-log",
    "adb",
    "native-crashes",
)
PART = 1024 * 1024
MAX = 64 * PART
ID = re.compile(r"(?:[a-f0-9]{32}|[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12})")


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, hdrs, newurl):
        return None


def http(method, url, headers, body=None, maximum=None):
    request = urllib.request.Request(
        url, data=body, method=method, headers={"User-Agent": "IanGrowingDiagnostics/2.0", **headers}
    )
    with urllib.request.build_opener(NoRedirect).open(request, timeout=35) as response:
        data = response.read() if maximum is None else response.read(maximum)
        return response.status, data


def secure_write(path, data, replace=True):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    tmp = path.with_name(path.name + "." + uuid.uuid4().hex + ".tmp")
    try:
        with os.fdopen(os.open(tmp, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600), "wb") as f:
            f.write(data)
            f.flush()
            os.fsync(f.fileno())
        if replace:
            os.replace(tmp, path)
        else:
            os.link(tmp, path)
    finally:
        tmp.unlink(missing_ok=True)


def event_id():
    return format(int(time.time() * 1000), "012x") + uuid.uuid4().hex[12:]


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="milliseconds")


def master():
    value = os.getenv("IAN_INGEST_TOKEN")
    if value:
        return value
    f = CONFIG / "ingest-token"
    if os.name != "nt" and f.stat().st_mode & 0o077:
        raise ValueError("Operator credential permissions must be 0600.")
    return f.read_text().strip()


def issue(platform):
    if platform not in SOURCES:
        raise ValueError("Unknown platform.")
    _, raw = http(
        "POST",
        ORIGIN + "/v1/session",
        {"Authorization": "Bearer " + master(), "Content-Type": "application/json"},
        json.dumps({"platform": platform}).encode(),
    )
    result = json.loads(raw)
    if result["platform"] != platform or not result["ticket"].startswith("ian2." + platform + "."):
        raise ValueError("Unexpected provisioning response.")
    return result


_tickets = {}


def upload_ticket(source):
    ticket = _tickets.get(source)
    if not ticket or ticket["expires"] <= time.time() + 60:
        ticket = _tickets[source] = issue(source)
    return ticket["ticket"]


def upload(body, source, day, eid, is_json=False, credential=None, build="", game_version=""):
    if source not in SOURCES or not ID.fullmatch(eid) or len(body) > MAX:
        raise ValueError("Invalid source, event ID or size.")
    dt.date.fromisoformat(day)
    digest = hashlib.sha256(body).hexdigest()
    url = ORIGIN + ("/v1/errors" if is_json else "/v1/files")
    key = source + "/" + day + "/" + eid + (".json" if is_json else ".log")
    headers = {
        "Authorization": "Bearer " + (credential or upload_ticket(source)),
        "X-Log-Source": source,
        "X-Event-Id": eid,
        "X-Report-Date": day,
        "X-Content-Sha256": digest,
        "X-Total-Bytes": str(len(body)),
        "X-Build-Id": build,
        "X-Game-Version": game_version,
        "Content-Type": "application/json" if is_json else "text/plain; charset=utf-8",
    }

    def post(suffix, data, index=None):
        code, response = http("POST", url + suffix, headers, data)
        ack = json.loads(response)
        if code not in (200, 201):
            raise IOError("No storage acknowledgement.")
        if ack.get("stored") is True and (ack.get("key"), ack.get("eventId"), ack.get("sha256")) == (
            key,
            eid,
            digest,
        ):
            return {"key": key, "sha256": digest, "status": code, "eventId": eid}
        if index is not None and ack.get("partStored") is True and ack.get("index") == index:
            return None
        raise IOError("Unverified storage acknowledgement.")

    if len(body) <= 5 * PART:
        return post("", body)
    for index, start in enumerate(range(0, len(body), PART)):
        receipt = post("?phase=part&index=" + str(index), body[start : start + PART], index)
        if receipt:
            return receipt
    return post("?phase=commit", b"")


def wrangler(*args):
    program = PROJECT / "node_modules/.bin" / ("wrangler.cmd" if os.name == "nt" else "wrangler")
    result = subprocess.run([str(program), *args], cwd=PROJECT, capture_output=True, text=True)
    if result.returncode:
        raise RuntimeError("Wrangler authentication unavailable; check the active developer login.")
    return json.loads(result.stdout)


@functools.lru_cache(maxsize=1)
def admin():
    account = os.getenv("CLOUDFLARE_ACCOUNT_ID")
    if not account:
        accounts = wrangler("whoami", "--json")["accounts"]
        if len(accounts) != 1:
            raise ValueError("Set CLOUDFLARE_ACCOUNT_ID for the selected account.")
        account = accounts[0]["id"]
    if not re.fullmatch("[a-f0-9]{32}", account):
        raise ValueError("Invalid account ID.")
    token = os.getenv("CLOUDFLARE_API_TOKEN") or wrangler("auth", "token", "--json")["token"]
    return (
        "https://api.cloudflare.com/client/v4/accounts/"
        + account
        + "/r2/buckets/iangrowing-errors/objects",
        token,
    )


def read_api(suffix, headers=None, maximum=None):
    url, token = admin()
    return http(
        "GET", url + suffix, {"Authorization": "Bearer " + token, **(headers or {})}, maximum=maximum
    )[1]


def object_bytes(key, prefix_only=False):
    parts = key.split("/")
    if len(parts) != 3 or parts[0] not in SOURCES or not re.fullmatch("[a-zA-Z0-9_.-]+", parts[2]):
        raise ValueError("Expected platform/YYYY-MM-DD/object.json or object.log.")
    dt.date.fromisoformat(parts[1])
    return read_api(
        "/" + urllib.parse.quote(key, safe="/"),
        {"Range": "bytes=0-8191"} if prefix_only else {},
        maximum=8192 if prefix_only else MAX + 1,
    )


def resolve(value, platform=None, date=None):
    if "/" in value:
        return value, object_bytes(value)
    if not ID.fullmatch(value):
        raise ValueError("Use a full object key for legacy objects.")
    if date is None and len(value) == 32:
        date = dt.datetime.fromtimestamp(int(value[:12], 16) / 1000, dt.timezone.utc).date().isoformat()
    if not date:
        raise ValueError("Supply --date for a non-timestamped event ID.")
    for src in [platform] if platform else SOURCES:
        ext = ".log" if src in ("adb", "editor-log", "native-crashes") else ".json"
        key = src + "/" + date + "/" + value + ext
        try:
            return key, object_bytes(key)
        except urllib.error.HTTPError as exc:
            if exc.code != 404:
                raise
    raise ValueError("Object not found in the selected date and platforms.")


def recent(source, date, limit, build=None, days=3):
    if not 1 <= limit <= 100 or not 1 <= days <= 14:
        raise ValueError("Limit 1–100, days 1–14.")
    start = dt.date.fromisoformat(date) if date else dt.datetime.now(dt.timezone.utc).date()
    result = []
    for n in range(1 if date else days):
        prefix = source + "/" + (start - dt.timedelta(days=n)).isoformat() + "/"
        items = []
        cursor = ""
        for _ in range(8):
            q = {"prefix": prefix, "per_page": "1000"}
            if cursor:
                q["cursor"] = cursor
            batch = json.loads(read_api("?" + urllib.parse.urlencode(q)))
            if batch.get("success") is not True:
                raise IOError("R2 list failed.")
            values = batch["result"]
            if isinstance(values, dict):
                values = values.get("objects", [])
            items.extend(values)
            info = batch.get("result_info") or {}
            cursor = info.get("cursor") or (info.get("cursors") or {}).get("after")
            if not cursor:
                break
        else:
            raise ValueError("This date exceeds the bounded metadata scan; use an object key.")
        for item in sorted(
            items, key=lambda i: (i.get("last_modified") or i.get("uploaded") or "", i["key"]), reverse=True
        ):
            key = item["key"]
            if key.count("/") != 2 or not key.startswith(prefix):
                continue
            if build:
                meta = item.get("customMetadata") or item.get("custom_metadata") or {}
                found = [meta.get("buildId"), meta.get("gameVersion")]
                if not any(found) and key.endswith(".json"):
                    beginning = object_bytes(key, True).decode("utf-8", "replace")
                    found = re.findall(r'"(?:gameVersion|buildId)"\s*:\s*"([^"]*)"', beginning)
                if build not in found:
                    continue
            result.append(
                {
                    "key": key,
                    "bytes": item.get("size"),
                    "uploaded": item.get("last_modified", item.get("uploaded")),
                }
            )
            if len(result) == limit:
                return result
    return result
