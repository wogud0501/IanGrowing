"""Login agents for explicitly selected internal QA hosts and devices."""

import os
from pathlib import Path
import plistlib
import subprocess
import sys

from archive import secure_write

PREFIX = "studio.iangrowing.diagnostics."


def manage(action, mode, log=None, enroll=None):
    if sys.platform != "darwin":
        raise ValueError("Login agents are available on macOS. Use the foreground watch command here.")
    label = PREFIX + mode
    target = "gui/" + str(os.getuid())
    plist = Path.home() / "Library/LaunchAgents" / (label + ".plist")

    def launch(*args, check=True):
        return subprocess.run(["launchctl", *args], capture_output=True, check=check)

    if action == "status":
        result = launch("print", target + "/" + label, check=False)
        print(label, "loaded" if result.returncode == 0 else "unloaded")
        return
    if action == "remove":
        launch("bootout", target + "/" + label, check=False)
        plist.unlink(missing_ok=True)
        print(label, "removed")
        return
    tool = Path(__file__).with_name("ian-errors").resolve()
    argv = [sys.executable, str(tool), "watch-" + mode, "--renew"]
    if mode == "editor":
        if not log or not enroll:
            raise ValueError("Supply the active Editor --log and --enrollment-directory.")
        argv += ["--log", str(log.resolve()), "--enrollment-directory", str(enroll.resolve())]
    if mode == "native":
        if not log or not log.is_dir():
            raise ValueError("Supply the approved crash report directory with --log.")
        argv = [sys.executable, str(tool), "watch-native", "--directory", str(log.resolve())]
    params = {
        "Label": label,
        "RunAtLoad": True,
        "KeepAlive": True,
        "ThrottleInterval": 30,
        "ProgramArguments": argv,
        "ProcessType": "Background",
        "LowPriorityIO": True,
        "EnvironmentVariables": {
            "PATH": os.environ.get("PATH", "/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin")
        },
    }
    contents = plistlib.dumps(params)
    if not plist.exists() or plist.read_bytes() != contents:
        secure_write(plist, contents)
        launch("bootout", target + "/" + label, check=False)
    if launch("print", target + "/" + label, check=False).returncode:
        launch("bootstrap", target, str(plist))
    print(label, "loaded")
