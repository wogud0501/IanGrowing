#!/usr/bin/env python3
"""Ian Growing local device QA. Pass --help for commands; no uninstall/reset operations."""
import argparse
import datetime as dt
import fcntl
import ipaddress
import json
import os
import re
import secrets
import shutil
import subprocess
import sys
import time
from pathlib import Path

from device import ADB, APK, ART, PACKAGE, ROOT, STATE, Phone, clean, devices, run
from logs import PRESETS, alive, capture, prune, query, sessions

CHECKS = ("First launch", "Update and launch", "Main menu", "Terms and privacy", "Consent persists",
          "New game", "Continue existing save", "Save compatibility", "Movement", "Touch", "NPC activity",
          "Animation", "Furniture interaction", "Scene transition", "Mission progress", "Mission completed",
          "Reward claim after update", "No duplicate reward", "UI navigation", "Safe Area", "Fold/unfold",
          "Background and resume", "Force stop and relaunch", "Memory", "Unity frame time", "Crash", "ANR")
ACTIONS = "devices info status reconnect install launch stop restart screenshot record mirror tap swipe long-press key screen performance start end tail watch search errors crash stack export report smoke regression mark diagnostics reward-check background-resume pair connect".split()


def save(path, value):
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2))
    tmp.replace(path)


def stamp():
    return dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def folder_for(phone, specified=None):
    pointer = STATE / ("device-" + phone.row["target"])
    name = specified or (pointer.read_text().strip() if pointer.exists() else "")
    if not re.fullmatch(r"\d{8}T\d{6}Z-[0-9a-f]{8}", name) or not (ART / name).is_dir():
        raise ValueError("Start a QA log session first.")
    return ART / name


def begin(phone):
    STATE.mkdir(parents=True, exist_ok=True)
    with (STATE / "start.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        try:
            current = folder_for(phone)
            if alive(current):
                return current
        except ValueError:
            pass
        phone.uid()
        prune()
        folder = ART / (stamp() + "-" + secrets.token_hex(4))
        folder.mkdir(parents=True, mode=0o700)
        for sub in ("screenshots", "recordings"):
            (folder / sub).mkdir()
        for name in ("logcat.log", "errors.log", "crash.log"):
            (folder / name).touch()
        save(folder / "device-info.json", phone.info())
        save(folder / "checks.json", {name: "NOT TESTED" for name in CHECKS})
        (folder / "git-status.txt").write_text(run(["git", "-C", str(ROOT), "status", "--short"]))
        with (folder / "collector-status.txt").open("w") as log:
            proc = subprocess.Popen([sys.executable, __file__, "_collect", "--device", phone.row["target"],
                                     "--session", folder.name], stdout=log, stderr=log, start_new_session=True)
        for _ in range(60):
            if alive(folder) and proc.poll() is None:
                (STATE / ("device-" + phone.row["target"])).write_text(folder.name)
                return folder
            if proc.poll() is not None:
                break
            time.sleep(.1)
        raise ValueError("Collector failed to start: " + str(folder / "collector-status.txt"))


def memo(folder, name, result):
    path = folder / "checks.json"
    with (folder / ".checks.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        data = json.loads(path.read_text())
        if name not in data:
            raise ValueError("Unknown check. See report for the list.")
        data[name] = result
        save(path, data)


def report(folder):
    data = json.loads((folder / "device-info.json").read_text())
    checks = json.loads((folder / "checks.json").read_text())
    counts = (folder / "counts.json")
    text = [f"# Ian Growing · {folder.name}", "", f"{data['model']} · Android {data['android']} · {PACKAGE}", "",
            "Logcat counts (deduplicated): " + (counts.read_text() if counts.exists() else "pending"), "",
            "| Check | Result / evidence |", "|---|---|"]
    for key, value in checks.items():
        text.append(f"| {key} | {value} |")
    text.extend(["", "Latest package errors", "", *query(folder, "Error", limit=8)])
    path = folder / "report.md"
    path.write_text("\n".join(text) + "\n")
    return str(path)


def visual(phone, folder, record, seconds):
    phone.guard()
    tool = shutil.which("scrcpy") or "/opt/homebrew/bin/scrcpy"
    args = [tool, "-s", phone.row["_serial"], "--no-audio", "--no-clipboard-autosync",
            "--no-power-on", "--max-size=1600", "--max-fps=30"]
    dest = folder / "recordings" / (stamp() + ".mp4")
    if record:
        args += ["--no-playback", "--no-control", "--record=" + str(dest)]
    proc = subprocess.Popen(args, env=dict(os.environ, ADB=ADB), stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    start = time.monotonic()
    try:
        while proc.poll() is None and (not record or time.monotonic() - start < seconds):
            time.sleep(.4)
            phone.guard()
        if proc.poll() not in (None, 0):
            raise ValueError("scrcpy stopped unexpectedly.")
    except BaseException:
        proc.terminate()
        proc.wait(timeout=5)
        if record:
            dest.unlink(missing_ok=True)
        raise
    finally:
        if proc.poll() is None:
            import signal
            proc.send_signal(signal.SIGINT)
            proc.wait(timeout=8)
    if record and (not dest.is_file() or dest.stat().st_size < 512):
        raise ValueError("Recording is empty.")
    return str(dest) if record else "Mirror closed"


def wait_active(phone):
    deadline = time.monotonic() + 35
    while time.monotonic() < deadline:
        if phone.active() and phone.shell("pidof", "-s", PACKAGE, check=False):
            return
        time.sleep(.6)
    raise ValueError("Game did not reach the foreground.")


def perform(a):
    action = a.action
    if action == "devices":
        return [{k:v for k,v in r.items() if k != "_serial"} for r in devices()]
    if action in ("pair", "connect"):
        host, port = a.values[0].rsplit(":", 1)
        addr = ipaddress.ip_address(host.strip("[]"))
        if not (addr.is_private or addr.is_link_local) or not 1 <= int(port) <= 65535:
            raise ValueError("Use the phone's private wireless debugging address and port.")
        return subprocess.run([ADB, action, a.values[0]], check=True).returncode
    if action == "reconnect" and a.device:
        if not any(r["target"] == a.device and not r["emulator"] and r["state"] in ("device", "offline") for r in devices()):
            raise ValueError("Selected transport is unavailable. Reattach the cable and list devices.")
        return run([ADB, "-t", a.device, "reconnect"])
    offline = ("tail", "search", "errors", "crash", "stack", "export", "report", "mark", "end", "watch")
    phone = None if a.session and action in offline else Phone(a.device)
    if action == "status":
        return {"authorized": True, "installed": phone.installed(), "foreground": phone.active()}
    if action == "info":
        return phone.info()
    if action == "install":
        was_installed = phone.installed()
        result = phone.install()
        if was_installed:
            try:
                current = folder_for(phone)
            except ValueError:
                current = None
            if current and alive(current):
                save(current / "update-checkpoint.json", {"device_epoch": int(phone.shell("date", "+%s"))})
        return result
    if action == "reconnect":
        return run(phone.base + ["reconnect"])
    if action in ("launch", "stop", "restart"):
        if action != "launch":
            phone.stop()
        return phone.launch() if action != "stop" else "Stopped"
    if action == "screen":
        return phone.screen()
    if action in ("tap", "swipe", "long-press", "key"):
        return phone.gesture(action, a.values) or "Input sent"
    if action in ("start", "smoke", "regression"):
        folder = begin(phone)
    elif not phone:
        if not re.fullmatch(r"\d{8}T\d{6}Z-[0-9a-f]{8}", a.session):
            raise ValueError("Invalid session ID.")
        folder = ART / a.session
    else:
        folder = folder_for(phone, a.session)
    if action == "_collect":
        return capture(phone, folder)
    if action == "start":
        return str(folder)
    if action == "end":
        (folder / ".stop").touch()
        for _ in range(35):
            if not alive(folder):
                break
            time.sleep(.2)
        return report(folder)
    if action == "mark":
        if len(a.values) < 3 or a.values[1] not in ("PASS", "FAIL", "NOT TESTED", "BLOCKED"):
            raise ValueError('mark "check name" PASS "observed evidence"')
        memo(folder, a.values[0], clean(" ".join(a.values[1:])))
        return report(folder)
    if action in ("errors", "stack", "crash", "tail", "search", "watch", "export"):
        preset = {"errors":"Error", "stack":"Stack", "crash":"Crash"}.get(action, a.preset)
        def read():
            return query(folder, preset, " ".join(a.values), a.limit, a.since, a.until)
        if action == "watch":
            previous = []
            while alive(folder):
                rows = read()
                idx = max((i+1 for i,v in enumerate(rows) if previous and v == previous[-1]), default=0)
                for row in rows[idx:]:
                    print(row, flush=True)
                previous = rows
                time.sleep(1)
            return "Session ended"
        if action == "export":
            path = folder / "filtered.txt"
            path.write_text("\n".join(read())+"\n")
            return str(path)
        return "\n".join(read())
    if action == "report":
        return report(folder)
    if action == "screenshot":
        return phone.capture(folder / "screenshots" / (stamp() + "-" + secrets.token_hex(2) + ".png"))
    if action in ("mirror", "record"):
        return visual(phone, folder, action == "record", min(120, max(1, int(a.values[0]) if a.values else 10)))
    if action == "reward-check":
        if len(a.values) != 1 or not re.fullmatch(r"M[0-9]+", a.values[0]):
            raise ValueError("Specify the pending mission ID, e.g. M001.")
        checkpoint = folder / "update-checkpoint.json"
        if not checkpoint.is_file():
            raise ValueError("Start recording before the APK update to verify a post-update claim.")
        since = json.loads(checkpoint.read_text())["device_epoch"]
        claimed = query(folder, "Quest", "[REWARD] Claimed ", 200, since=since)
        hits = [row for row in claimed if row.endswith("Claimed " + a.values[0])]
        result = "FAIL" if len(hits) > 1 else "PASS" if hits else "NOT TESTED"
        memo(folder, "Reward claim after update", f"{result} · {a.values[0]} committed {len(hits)} time(s) since install")
        if len(hits) > 1:
            memo(folder, "No duplicate reward", "FAIL · repeated mission claim in recorded update scenario")
        return report(folder)
    if action == "background-resume":
        phone.guard()
        prior = phone.shell("pidof", "-s", PACKAGE)
        phone.shell("input", "keyevent", "KEYCODE_HOME")
        time.sleep(1)
        left = not phone.active()
        phone.launch()
        wait_active(phone)
        same = phone.shell("pidof", "-s", PACKAGE) == prior
        memo(folder, "Background and resume", ("PASS" if left and same else "FAIL") + " · foreground transition and PID")
        return report(folder)
    if action == "performance":
        stats = phone.performance()
        save(folder / "performance.json", stats)
        return stats
    if action == "diagnostics":
        # Read only the game's already-sanitized text reports, never the save or native memory.
        base = "/sdcard/Android/data/" + PACKAGE + "/files/Diagnostics/Errors"
        names = phone.shell("ls", "-1", base, check=False).splitlines()
        valid = sorted(n for n in names if re.fullmatch(r"ERR-[0-9]{8}-[0-9]{6}-[0-9a-f]{32}", n))
        if not valid:
            return "No local error reports"
        if a.values == ["list"]:
            return valid
        name = a.values[0] if a.values else valid[-1]
        if name not in valid or len(a.values) > 1:
            raise ValueError("Unknown diagnostic report ID.")
        dest = folder / ("diagnostic-" + name + ".txt")
        dest.write_text(clean_report(phone.shell("cat", base + "/" + name + "/report.txt")))
        return str(dest)
    if action in ("smoke", "regression"):
        phone.launch()
        wait_active(phone)
        time.sleep(2)
        phone.capture(folder / "screenshots" / (stamp() + "-launch.png"))
        perf = phone.performance()
        save(folder / "performance.json", perf)
        if perf["unity_surface_intervals"]:
            memo(folder, "Unity frame time", f"PASS · SurfaceView present intervals, p95 {perf['present_ms_p95']} ms")
        if perf["pss_kib"]:
            memo(folder, "Memory", "PASS · " + str(perf["pss_kib"]) + " KiB PSS")
        if action == "regression":
            # Observe real reward transactions; lack of a claim is never evidence of success.
            claims = query(folder, "Quest", "[REWARD] Claimed ", 200)
            ids = [v.split("Claimed ")[-1] for v in claims]
            if len(ids) != len(set(ids)):
                memo(folder, "No duplicate reward", "FAIL · repeated claim ID; investigate within this save")
            before = phone.shell("pidof", "-s", PACKAGE)
            phone.stop()
            phone.launch()
            wait_active(phone)
            after = phone.shell("pidof", "-s", PACKAGE)
            memo(folder, "Force stop and relaunch", ("PASS" if before != after else "FAIL") + " · checked process and focus")
        if query(folder, "Crash"):
            memo(folder, "Crash", "FAIL · review crash.log")
        return report(folder)


def clean_report(body):
    def scrub(line):
        if re.fullmatch(r"Report ID: ERR-[0-9]{8}-[0-9]{6}-[0-9a-f]{32}", line):
            return line
        return clean(line)
    return "\n".join(scrub(line) for line in body.splitlines()) + "\n"


if __name__ == "__main__":
    os.umask(0o077)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=ACTIONS + ["_collect"])
    parser.add_argument("values", nargs="*")
    parser.add_argument("--device", help="ADB transport ID (see devices); never recorded in reports")
    parser.add_argument("--session")
    parser.add_argument("--preset", choices=PRESETS, default="Live")
    parser.add_argument("--limit", type=int, default=30)
    parser.add_argument("--since", type=float, help="UTC Unix seconds")
    parser.add_argument("--until", type=float, help="UTC Unix seconds")
    try:
        result = perform(parser.parse_intermixed_args())
        if result is not None:
            print(result if isinstance(result, str) else json.dumps(result, ensure_ascii=False, indent=2))
    except (ValueError, RuntimeError, OSError, IndexError, subprocess.SubprocessError) as error:
        sys.exit(clean(str(error)))
