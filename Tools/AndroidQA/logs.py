"""Only package UID records and correlated system crash evidence enter the session files."""
import datetime as dt
import json
import os
import queue
import re
import subprocess
import threading
import time
from collections import deque
from pathlib import Path

from device import ART, PACKAGE, STATE, clean

PRESETS = ("Live", "Debug", "Warning", "Error", "Crash", "Quest", "Save", "UI", "Network", "Performance", "Legal")
FORMAT = re.compile(r"^\s*(\d+\.\d+)\s+(\d+)\s+(\d+)\s+([VDIWEF])\s+([^:]+?)\s*:\s?(.*)$")
CRASH = re.compile(r"FATAL EXCEPTION|Fatal signal|SIGSEGV|SIGABRT|OutOfMemoryError|ANR in |am_anr|native crash", re.I)
LEVEL = "VDIWEF"


def matches(line, preset):
    m = FORMAT.match(line)
    if not m:
        return False
    _, _, _, priority, tag, msg = m.groups()
    if preset == "Debug":
        return True
    if preset == "Live":
        return priority in "IWEF" and tag.strip() in ("Unity", "AndroidRuntime", "libc", "DEBUG", "IanQA")
    if preset in ("Warning", "Error"):
        return priority in ("WEF" if preset == "Warning" else "EF")
    if preset == "Crash":
        return bool(CRASH.search(msg) or tag.strip() == "IanCrash" or priority == "F")
    patterns = {"Quest": r"\[IAN\]\[(QUEST|REWARD)\]", "Save": r"\[IAN\]\[SAVE\]",
                "UI": r"\[IAN\]\[(UI|INPUT)\]", "Legal": r"\[IAN\]\[LEGAL\]",
                "Network": r"UnityWebRequest|UnknownHostException|SocketException|ConnectException",
                "Performance": r"\b(?:Skipped \d+ frames|GC freed|FrameTiming|Choreographer)\b"}
    return bool(re.search(patterns[preset], msg + " " + tag))


def put(path, line):
    # Two generations of each log, with a maximum of ~4 MiB per generation.
    if path.exists() and path.stat().st_size >= 4 * 1024 * 1024:
        path.replace(path.with_suffix(".log.1"))
    with path.open("a") as f:
        f.write(line.rstrip() + "\n")


def sessions():
    return sorted(p for p in ART.glob("*") if p.is_dir() and re.fullmatch(r"\d{8}T\d{6}Z-[0-9a-f]{8}", p.name))


def alive(folder):
    heart = folder / ".heartbeat"
    return heart.exists() and time.time() - heart.stat().st_mtime < 12


def prune():
    import shutil
    used = 0
    for folder in reversed(sessions()):
        size = sum(p.stat().st_size for p in folder.rglob("*") if p.is_file())
        if not alive(folder) and (time.time() - folder.stat().st_mtime > 14 * 86400 or used + size > 1024**3):
            shutil.rmtree(folder)
        else:
            used += size


def capture(phone, folder):
    uid = phone.uid()
    started = phone.shell("date", "+%s")
    jobs = []
    messages = queue.Queue(maxsize=512)

    def stream(source, args):
        proc = subprocess.Popen(phone.base + ["logcat", "-T", started + ".000", "-v", "epoch", *args],
                                stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, errors="replace")
        jobs.append(proc)

        def read():
            for line in proc.stdout:
                messages.put((source, line.rstrip()))
        threading.Thread(target=read, daemon=True).start()

    recent = {}
    trace_owner = {}
    pid = None
    previous_screen = None
    next_poll = 0
    errors = warnings = repeats = 0
    folder.joinpath(".heartbeat").touch()
    try:
        stream("app", ["-b", "main", "-b", "system", "-b", "crash", "--uid=" + uid, "*:V"])
        stream("system", ["-b", "crash", "-b", "system", "-b", "events", "-b", "main",
                          "AndroidRuntime:E", "ActivityManager:E", "ActivityTaskManager:E", "am_anr:I",
                          "DEBUG:F", "libc:F", "*:S"])
        while not (folder / ".stop").exists():
            now = time.monotonic()
            if now >= next_poll:
                current = phone.shell("pidof", "-s", PACKAGE, check=False)
                if current != pid:
                    put(folder / "process.log", f"{time.time():.3f} pid={current or 'stopped'}")
                    pid = current
                screen = phone.screen()
                if screen != previous_screen:
                    put(folder / "screen.log", f"{time.time():.3f} " + json.dumps(screen))
                    previous_screen = screen
                folder.joinpath(".heartbeat").touch()
                (folder / "counts.json").write_text(json.dumps(dict(errors=errors, warnings=warnings, repeats=repeats)))
                if any(p.poll() is not None for p in jobs):
                    raise RuntimeError("ADB log stream ended; reconnect and start a new session.")
                next_poll = time.monotonic() + 1.5
            try:
                source, raw = messages.get(timeout=.25)
            except queue.Empty:
                continue
            m = FORMAT.match(raw)
            if not m:
                continue
            stamp, writer, tid, priority, tag, msg = m.groups()
            tag = tag.strip()
            if source == "system":
                # Tombstones come from crash_dump, not the dead app PID. Only admit frames
                # after its explicit package marker, on that writer/TID, for this tombstone.
                key = (writer, tid)
                if ">>>" in msg or "*** ***" in msg:
                    trace_owner.pop(key, None)
                owned = bool(re.search(r"(?<![\w.])" + re.escape(PACKAGE) + r"(?![\w.])", msg))
                if owned and tag == "DEBUG" and ">>>" in msg:
                    trace_owner[key] = now
                frame = now - trace_owner.get(key, -1000) < 8 and re.search(r"^\s*(#\d+ pc|signal \d+|backtrace:)", msg)
                if not ((owned and (CRASH.search(msg) or tag == "am_anr" or ">>>" in msg)) or frame):
                    continue
                tag = "IanCrash"
            safe = clean(msg)
            # Exact repeats in a short window; keep separate frames of a stack.
            signature = (source, writer, priority, tag, safe)
            if abs(float(stamp) - recent.get(signature, 0)) < 1:
                repeats += 1
                continue
            if len(recent) >= 512:
                recent.clear()
            recent[signature] = float(stamp)
            line = f"{stamp} {writer} {tid} {priority} {tag}: {safe}"
            put(folder / "logcat.log", line)
            if priority in "EF":
                errors += 1
                put(folder / "errors.log", line)
            if priority == "W":
                warnings += 1
            if matches(line, "Crash"):
                put(folder / "crash.log", line)
    finally:
        for proc in jobs:
            proc.terminate()
            try:
                proc.wait(timeout=2)
            except subprocess.TimeoutExpired:
                proc.kill()
        (folder / "counts.json").write_text(json.dumps(dict(errors=errors, warnings=warnings, repeats=repeats)))
        (folder / ".heartbeat").unlink(missing_ok=True)


def query(folder, kind, term="", limit=30, since=None, until=None):
    if kind not in PRESETS + ("Stack",):
        raise ValueError("Unknown preset")
    name = "crash.log" if kind == "Crash" else "logcat.log"
    result = deque(maxlen=max(1, min(200, limit)))
    for path in [folder / (name + ".1"), folder / name]:
        if not path.exists():
            continue
        with path.open(errors="replace") as f:
            for line in f:
                if kind == "Stack":
                    if not re.search(r"\(at |\.cs:\d+|Exception|\bat [\w.]+\(|#\d+ pc", line):
                        continue
                elif not matches(line, kind):
                    continue
                stamp = float(line.split()[0])
                if since is not None and stamp < since or until is not None and stamp > until:
                    continue
                if term.lower() in line.lower():
                    result.append(line.rstrip())
    return list(result)
