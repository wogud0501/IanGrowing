"""Explicit operator enrollment and renewal. The server secret never goes to a player."""

import hashlib
import hmac
import json
import os
from pathlib import Path
import subprocess
import time

from archive import issue, master, secure_write

ANDROID_SESSION = "no_backup/IanCloudDiagnostics/session.json"


def provision(platform, phone=None, directory=None):
    ticket = issue(platform)
    raw = json.dumps(ticket).encode()
    if phone:
        from device import PACKAGE

        cmd = (
            "umask 077; mkdir -p no_backup/IanCloudDiagnostics && cat > "
            + ANDROID_SESSION
            + ".tmp && mv "
            + ANDROID_SESSION
            + ".tmp "
            + ANDROID_SESSION
        )
        subprocess.run(
            phone.base + ["shell", "run-as " + PACKAGE + " sh -c '" + cmd + "'"],
            input=raw,
            capture_output=True,
            check=True,
        )
    else:
        secure_write(Path(directory) / "IanCloudDiagnostics/session.json", raw)
    return ticket


def maintain(platform, phone=None, directory=None, renew=False):
    if phone:
        from device import PACKAGE

        result = subprocess.run(
            phone.base + ["exec-out", "run-as", PACKAGE, "cat", ANDROID_SESSION],
            capture_output=True,
            timeout=8,
        )
        raw = result.stdout if result.returncode == 0 else b""
    else:
        path = Path(directory) / "IanCloudDiagnostics/session.json"
        raw = path.read_bytes() if path.is_file() and path.stat().st_size <= 2048 else b""
    if not raw or len(raw) > 2048:
        return False
    try:
        obj = json.loads(raw)
        token = obj["ticket"]
        ver, scope, expiry, nonce, sig = token.split(".")
        if (
            ver != "ian2"
            or scope != platform
            or obj["platform"] != platform
            or str(obj["expires"]) != expiry
        ):
            return False
        expected = hmac.new(
            ("ian-ticket-v2:" + master()).encode(), token.rsplit(".", 1)[0].encode(), hashlib.sha256
        ).hexdigest()
        if not hmac.compare_digest(expected, sig):
            return False
        remaining = int(expiry) - time.time()
        if renew and remaining < 1800:
            provision(platform, phone, directory)
            return True
        return remaining > 0
    except (ValueError, KeyError, AttributeError, TypeError):
        return False
