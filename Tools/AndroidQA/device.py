"""Local, package-scoped operations for Ian Growing development players."""
import json
import os
import re
import shlex
import shutil
import statistics
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "QAArtifacts/Android"
STATE = ROOT / "Library/AndroidQA"
APK = ROOT / "Builds/AndroidQA/IanGrowing.apk"
VERSION = re.search(r"m_EditorVersion: (\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text())[1]
ANDROID = Path("/Applications/Unity/Hub/Editor") / VERSION / "PlaybackEngines/AndroidPlayer"
ADB = str(ANDROID / "SDK/platform-tools/adb")
if not Path(ADB).is_file():
    ADB = shutil.which("adb") or "adb"
SETTINGS = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text()
PACKAGE = re.search(r"^  applicationIdentifier:\n(?:    [^\n]+\n)*?    Android: ([\w.]+)$", SETTINGS, re.M)[1]
if not re.fullmatch(r"[A-Za-z][\w]*(?:\.[\w]+)+", PACKAGE):
    raise ValueError("Invalid Android application identifier")


def run(args, timeout=12, check=True):
    result = subprocess.run(args, capture_output=True, timeout=timeout, text=True, errors="replace")
    if check and result.returncode:
        raise ValueError(clean(result.stderr or result.stdout or "Command failed")[:350])
    return result.stdout.strip()


def clean(text):
    text = re.sub(r"(?i)(authorization|bearer|token|api[_-]?key|secret|password|receipt|cookie|account|serial(?:no|number)?|latitude|longitude|광고식별자|결제|비밀번호)\b.*",
                  "[redacted]", text)
    text = re.sub(r"https?://\S+|[\w.+-]+@[\w.-]+\.[\w-]+|(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?|\b[\d-]{10,19}\b",
                  "[redacted]", text)
    text = re.sub(r"(?:/Users/|/home/|[A-Za-z]:[\\/]Users[\\/])[^\s/\\]+", "[home]", text)
    text = re.sub(r"/data/app/[\w.+/=_~-]+", "[installed APK]", text)
    text = re.sub(r"(?<!\d)(?:\d[ -]?){13,19}(?!\d)", "[redacted]", text)
    return re.sub(r"(?i)(?:/sdcard|/storage/emulated/\d+)/(?!Android/data/)[^\n]+", "[private path]", text)[:1800]


def devices():
    rows = []
    for line in run([ADB, "devices", "-l"]).splitlines()[1:]:
        parts = line.split()
        if len(parts) < 2 or parts[0].startswith("*"):
            continue
        fields = dict(p.split(":", 1) for p in parts[2:] if ":" in p)
        target = fields.get("transport_id")
        if not target:
            continue
        emulator = parts[0].startswith("emulator-")
        if parts[1] == "device" and not emulator:
            emulator = run([ADB, "-t", target, "shell", "getprop", "ro.kernel.qemu"]) == "1"
        rows.append({"target": target, "state": parts[1], "model": fields.get("model", "unknown"),
                     "connection": "USB" if "usb" in fields else "wireless/other",
                     "emulator": emulator, "_serial": parts[0]})
    return rows


class Phone:
    def __init__(self, target=None):
        valid = [r for r in devices() if r["state"] == "device" and not r["emulator"]]
        if target:
            valid = [r for r in valid if r["target"] == target]
        if len(valid) != 1:
            raise ValueError("Select one authorized physical device using its current transport ID.")
        self.row = valid[0]
        self.base = [ADB, "-t", self.row["target"]]

    def shell(self, *parts, **kw):
        return run(self.base + ["shell", *parts], **kw)

    def user(self):
        user = self.shell("am", "get-current-user")
        if not user.isdigit():
            raise ValueError("Active Android user unavailable.")
        return user

    def stop(self):
        return self.shell("am", "force-stop", "--user", self.user(), PACKAGE)

    def installed(self):
        return self.shell("pm", "path", "--user", self.user(), PACKAGE, check=False).startswith("package:")

    def uid(self):
        items = self.shell("cmd", "package", "list", "packages", "-U", "--user", self.user(), PACKAGE)
        match = re.search(r"^package:" + re.escape(PACKAGE) + r" uid:(\d+)$", items, re.M)
        if not match:
            raise ValueError("Package is not installed for the active test user.")
        uid = match[1]
        owners = self.shell("cmd", "package", "list", "packages", "--user", self.user(), "--uid", uid).splitlines()
        if [owner.split()[0] for owner in owners] != ["package:" + PACKAGE]:
            raise ValueError("Cannot isolate this shared UID.")
        return uid

    def active(self):
        # Never return other foreground application names or capture the lock screen.
        windows = self.display_zero()
        return bool(re.search(r"mCurrentFocus=[^\n]*?\s" + re.escape(PACKAGE) + r"/", windows))

    def display_zero(self):
        text = self.shell("dumpsys", "window", "displays")
        for section in text.split("Display: mDisplayId=")[1:]:
            if section.startswith("0 "):
                return section
        return ""

    def screen(self):
        display = self.shell("dumpsys", "display")
        rotation = re.search(r"mCurrentOrientation=(\d)", display)
        # Input coordinates follow the active display, including rotated and folded configurations.
        windows = self.display_zero()
        area = re.search(r"\bcur=(\d+)x(\d+)", windows)
        sizes = re.findall(r"(\d+)x(\d+)", self.shell("wm", "size"))
        width, height = map(int, area.groups() if area else sizes[-1])
        if not area and rotation and int(rotation[1]) % 2:
            width, height = height, width
        fold = self.shell("cmd", "device_state", "state", check=False)
        name = re.search(r"name='([A-Za-z0-9_ -]+)'", fold)
        return {"width": width, "height": height, "rotation": int(rotation[1]) if rotation else None,
                "dpi": self.shell("wm", "density").splitlines()[-1], "fold": name[1] if name else None}

    def info(self):
        pkg = self.shell("dumpsys", "package", PACKAGE) if self.installed() else ""
        version = re.search(r"versionName=(\S+)", pkg)
        code = re.search(r"versionCode=(\d+)", pkg)
        return {"model": self.row["model"], "android": self.shell("getprop", "ro.build.version.release"),
                "package": PACKAGE, "installed": bool(pkg), "version": version[1] if version else None,
                "build": code[1] if code else None, "screen": self.screen(), "physical": True}

    def launch(self):
        component = self.shell("cmd", "package", "resolve-activity", "--brief", "--user", self.user(), PACKAGE).splitlines()[-1]
        if not component.startswith(PACKAGE + "/") or not re.fullmatch(r"[\w.$/]+", component):
            raise ValueError("Game launcher activity unavailable.")
        result = self.shell("am", "start", "--user", self.user(), "-W", "-n", component, timeout=45)
        if "Error" in result or "Status: timeout" in result:
            raise ValueError("Android did not launch the game.")
        return "Launch requested"

    def install(self):
        if not APK.is_file():
            raise ValueError("Build the Android QA APK first.")
        tools = sorted((ANDROID / "SDK/build-tools").glob("*/aapt"))
        identity = run([str(tools[-1]), "dump", "badging", str(APK)], timeout=60)
        if not identity.startswith("package: name='" + PACKAGE + "'") or "application-debuggable" not in identity:
            raise ValueError("APK identity or Development setting does not match this project.")
        result = run(self.base + ["install", "--user", self.user(), "-r", str(APK)], timeout=180)
        if "Success" not in result:
            raise ValueError(clean(result))
        return result

    def guard(self):
        if not self.active():
            raise ValueError("Unlock the phone and bring Ian Growing to the foreground.")

    def capture(self, dest):
        self.guard()
        data = subprocess.check_output(self.base + ["shell", "-T", "screencap", "-p"], stderr=subprocess.PIPE, timeout=12)
        if not data.startswith(b"\x89PNG\r\n\x1a\n"):
            raise ValueError("No PNG received.")
        self.guard()
        dest.write_bytes(data)
        return str(dest)

    def gesture(self, action, numbers):
        self.guard()
        if action == "key":
            if numbers not in (["BACK"], ["ENTER"], ["SPACE"]):
                raise ValueError("Supported keys: BACK, ENTER, SPACE")
            return self.shell("input", "keyevent", "KEYCODE_" + numbers[0])
        expected = {"tap": 2, "swipe": 5, "long-press": 3}[action]
        if len(numbers) != expected:
            raise ValueError("Use normalized coordinates 0..1 and a duration in ms for swipes/holds.")
        screen = self.screen()
        values = [float(v) for v in numbers[:4 if action == "swipe" else 2]]
        if not all(0 <= v <= 1 for v in values):
            raise ValueError("Coordinates must be between 0 and 1.")
        coords = [str(round(v * (screen[["width", "height"][i % 2]] - 1))) for i, v in enumerate(values)]
        if action == "tap":
            return self.shell("input", "tap", *coords)
        ms = int(numbers[-1])
        if not 80 <= ms <= 4000:
            raise ValueError("Duration must be 80..4000 ms.")
        return self.shell("input", "swipe", *(coords * 2 if action == "long-press" else coords), str(ms))

    def performance(self):
        mem = self.shell("dumpsys", "meminfo", PACKAGE)
        pss = re.search(r"TOTAL PSS:\s*([\d,]+)", mem)
        gfx = self.shell("dumpsys", "gfxinfo", PACKAGE)
        metrics = re.findall(r"^\s*((?:50th|90th|95th|99th) percentile):\s*(\d+)ms", gfx, re.M)
        # Use the actual game SurfaceView. gfxinfo measures the host Activity's UI.
        layers = self.shell("dumpsys", "SurfaceFlinger", "--list", check=False)
        surface = None
        for line in layers.splitlines():
            if "SurfaceView[" + PACKAGE + "/" not in line or "Background for " in line:
                continue
            label = line.split("{")[-1].split(" parentId=")[0].strip()
            if "(BLAST)" in label or surface is None:
                surface = label
        intervals = []
        if surface:
            latency = self.shell("dumpsys", "SurfaceFlinger", "--latency", shlex.quote(surface), check=False)
            presented = []
            for row in latency.splitlines():
                fields = row.split()
                if len(fields) == 3 and fields[1].isdigit():
                    ns = int(fields[1])
                    if 0 < ns < 2**63 - 1 and (not presented or ns > presented[-1]):
                        presented.append(ns)
            intervals = sorted((b - a) / 1e6 for a, b in zip(presented, presented[1:]))
        return {"unity_surface_intervals": len(intervals),
                "present_ms_median": round(statistics.median(intervals), 2) if intervals else None,
                "present_ms_p95": round(intervals[int(.95 * (len(intervals) - 1))], 2) if intervals else None,
                "present_fps_mean": round(1000 / statistics.mean(intervals), 1) if intervals else None,
                "pss_kib": int(pss[1].replace(",", "")) if pss else None,
                "android_ui_frame_ms": dict(metrics),
                "note": "Display presentation intervals from the game SurfaceView. GPU/CPU simulation times require Unity Profiler."}
