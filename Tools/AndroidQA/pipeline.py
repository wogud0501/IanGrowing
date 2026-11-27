#!/usr/bin/env python3
"""Run an update-safe physical test from the existing loopback Unity MCP editor."""
import argparse
import json
import os
import subprocess
import sys
import time
import secrets
import urllib.request
from pathlib import Path

from device import APK, ART, PACKAGE, ROOT, Phone, clean
from qa import CHECKS, begin, memo, report, save, stamp, wait_active


class Editor:
    def __init__(self, port):
        self.endpoint = f"http://127.0.0.1:{port}/mcp"
        self.session = None
        self.call("initialize", {"protocolVersion": "2025-06-18", "capabilities": {},
                                 "clientInfo": {"name": "Ian local Android QA", "version": "1"}})
        self.call("notifications/initialized", {}, notify=True)
        project = self.resource("mcpforunity://project/info")
        if Path(project["projectRoot"]).resolve() != ROOT or project["platform"] != "Android":
            raise ValueError("Select this project and Android in the current Unity MCP instance.")

    def call(self, method, params, notify=False):
        payload = {"jsonrpc": "2.0", "method": method, "params": params}
        if not notify:
            payload["id"] = 1
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
        req = urllib.request.Request(self.endpoint, json.dumps(payload).encode(), headers)
        with urllib.request.urlopen(req, timeout=60) as response:
            self.session = response.headers.get("Mcp-Session-Id", self.session)
            body = response.read().decode().strip()
        if notify:
            return
        if not body.startswith("{"):
            body = next(line[6:] for line in body.splitlines() if line.startswith("data: "))
        value = json.loads(body)
        if "error" in value:
            raise ValueError(str(value["error"]))
        return value["result"]

    def resource(self, uri):
        result = self.call("resources/read", {"uri": uri})
        return json.loads(result["contents"][0]["text"])["data"]

    def tool(self, action):
        result = self.call("tools/call", {"name": "execute_custom_tool",
                           "arguments": {"tool_name": "ian_android_qa", "parameters": {"action": action}}})
        body = json.loads(result["content"][0]["text"])
        if not body.get("success") or not body["data"].get("success"):
            raise ValueError(str(body.get("error") or body.get("data")))
        return body["data"]["result"]

    def build(self):
        self.tool("build")
        status = ROOT / "Library/AndroidQA-build.txt"
        deadline = time.monotonic() + 1800
        while time.monotonic() < deadline:
            result = status.read_text()
            if result.startswith("Succeeded"):
                return result
            if result and result not in ("BUILDING", "QUEUED"):
                raise ValueError(result)
            time.sleep(2)
        raise ValueError("Timed out waiting for the Editor build. Inspect Build status.")


def pipeline(args):
    editor = Editor(args.port)
    ready = editor.tool("preflight")
    phone = Phone(args.device)
    before = phone.info()
    # State evidence is existence only. Never read or copy a player's save.
    base = f"/sdcard/Android/data/{PACKAGE}/files/"
    slot = "ian-black-comedy-v4.json"
    has_save = phone.shell("ls", base + slot, check=False).endswith("/" + slot) if before["installed"] else False
    try:
        status = editor.build() if args.build or not APK.exists() else "Reused local development APK"
        install = phone.install()
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError) as exc:
        failed = ART / (stamp() + "-" + secrets.token_hex(4))
        failed.mkdir(parents=True, mode=0o700)
        save(failed / "device-info.json", before)
        save(failed / "checks.json", dict.fromkeys(CHECKS, "NOT TESTED"))
        memo(failed, "Update and launch" if before["installed"] else "First launch",
             "BLOCKED · deployment failed: " + clean(str(exc)))
        raise ValueError(f"Deployment failed. Evidence: {report(failed)}") from exc
    folder = begin(phone)
    if before["installed"]:
        save(folder / "update-checkpoint.json", {"device_epoch": int(phone.shell("date", "+%s"))})
    save(folder / "deployment.json", {"before": before, "existing_v4_slot": has_save,
         "after": phone.info(), "build": status, "compile": ready, "install": install})
    phone.launch()
    wait_active(phone)
    memo(folder, "Update and launch" if before["installed"] else "First launch",
         "PASS · package installer succeeded and launcher Activity has foreground focus")
    # Smoke and regression use this exact selected transport and retain the package data.
    for step in ("smoke", "regression") if args.regression else ("smoke",):
        subprocess.run([sys.executable, str(ROOT / "Tools/AndroidQA/qa.py"), step,
                        "--device", phone.row["target"]], check=True)
    return report(folder)


if __name__ == "__main__":
    os.umask(0o077)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--device")
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--build", action="store_true", help="Rebuild even if a local APK exists")
    parser.add_argument("--regression", action="store_true", help="Also test force-stop/relaunch")
    try:
        print(pipeline(parser.parse_args()))
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError) as exc:
        sys.exit(clean(str(exc)))
