"""Writes only synthetic integration fixtures to the configured private R2 bucket."""

import argparse
import hashlib
import json
from pathlib import Path
import sys
import urllib.error

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import archive as a


def rejected(status, fn):
    try:
        fn()
    except urllib.error.HTTPError as exc:
        assert exc.code == status, (exc.code, status)
        return status
    raise AssertionError("Expected HTTP " + str(status))


def run():
    results = {}
    day = a.utc()[:10]
    ticket = a.issue("osxeditor")["ticket"]
    for name, trace in (
        ("ordinary", "first stack frame\r\nlast stack frame"),
        ("oversize", "frame.WithParameters() (at Assets/Scripts/Qa.cs:19)\n" * 110000),
    ):
        eid = a.event_id()
        doc = {
            "timestamp": a.utc(),
            "eventId": eid,
            "platform": "osxeditor",
            "gameVersion": "wire-qa",
            "buildId": "test-protocol",
            "sessionId": a.event_id(),
            "level": "Exception",
            "message": "IAN_CLOUD_PROTOCOL_" + name,
            "stackTrace": trace,
        }
        data = json.dumps(doc, ensure_ascii=False, separators=(",", ":")).encode()
        ack = a.upload(data, "osxeditor", day, eid, True, ticket)
        replay = a.upload(data, "osxeditor", day, eid, True, ticket)
        assert ack["status"] == 201 and replay["status"] == 200 and ack["key"] == replay["key"]
        assert a.object_bytes(ack["key"]) == data
        results[name] = {**ack, "replay": 200, "bytes": len(data), "downloadExact": True}
        if name == "ordinary":
            doc["message"] += "_DIFFERENT"
            results["differentBytesSameId"] = rejected(
                409, lambda: a.upload(json.dumps(doc).encode(), "osxeditor", day, eid, True, ticket)
            )
    results["wrongAuth"] = rejected(
        401,
        lambda: a.http(
            "POST", a.ORIGIN + "/v1/errors", {"Authorization": "Bearer synthetic-invalid"}, b"{}"
        ),
    )
    results["wrongScope"] = rejected(
        403, lambda: a.upload(b"file", "android", day, a.event_id(), credential=ticket)
    )
    results["ticketCannotIssue"] = rejected(
        403,
        lambda: a.http(
            "POST",
            a.ORIGIN + "/v1/session",
            {"Authorization": "Bearer " + ticket},
            b'{"platform":"android"}',
        ),
    )
    results["publicRead"] = rejected(404, lambda: a.http("GET", a.ORIGIN + "/v1/errors", {}))
    file_data = b"IAN_CLOUD_LARGE_FILE\n" + b" frame\n" * 760000
    file_id = a.event_id()
    file_ack = a.upload(file_data, "adb", day, file_id)
    assert a.object_bytes(file_ack["key"]) == file_data
    results["oversizeFile"] = {**file_ack, "bytes": len(file_data), "downloadExact": True}
    # Legacy callers don't supply an ID or hash and expect 201.
    legacy = json.dumps(
        {
            "platform": "OSXEditor",
            "timestamp": a.utc(),
            "message": "IAN_CLOUD_LEGACY_QA_" + a.event_id(),
            "stackTrace": "one\ntwo",
        }
    ).encode()
    headers = {"Authorization": "Bearer " + a.master(), "Content-Type": "application/json"}
    first = a.http("POST", a.ORIGIN + "/v1/errors", headers, legacy)
    second = a.http("POST", a.ORIGIN + "/v1/errors", headers, legacy)
    assert first[0] == second[0] == 201 and json.loads(first[1])["key"] == json.loads(second[1])["key"]
    results["legacyCompatible"] = True
    return results


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--live",
        required=True,
        action="store_true",
        help="Perform real writes with the operator's enrollment",
    )
    parser.parse_args()
    print(json.dumps(run(), indent=2))
