"""Privately spool filtered developer logs and advance their durable read cursors atomically."""

import datetime as dt
import hashlib
import json
import os
import re
import sqlite3
import subprocess
import sys
import time
import urllib.error
from pathlib import Path

import archive
from privacy import clean

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/AndroidQA"))
STORE = Path(os.getenv("IAN_ERRORS_STATE", Path.home() / ".local/share/ian-errors"))
BUDGET = 96 * 1024**2
EMPTY = re.compile(rb"^\s*$")
ERROR = re.compile(
    rb"(?:\berror CS\d+|:\s*error(?:\s|:)|\b(?:fatal error|compilation failed|build failed|error building player|failed to import|error while importing|error while resolving packages|invalid package|shader error|unhandled exception|assertion failed)\b|^[\w.$+]*(?:Exception|Error):|^\s*\[(?:error|fatal|assert)\])",
    re.I,
)
TRACE = re.compile(
    rb"^\s*(?:#\d+\b|at |\(Filename:|\(at |--- End|Caused by:|InnerException|Rethrow as|[\w.$+/<>\x60\[\]|-]+:[^ \t\r\n]+[ \t]*\(|[\w.$+/<>\x60-]+\.[\w<>.\x60|]+\()"
)
ERROR_CALL = re.compile(rb"\b(?:Debug|Logger)(?:[:.])(?:LogError|LogException|LogAssertion|Assert)\b")
REGULAR = re.compile(
    rb"^\[(?!error|fatal|assert)|^(?:Importing |Refreshing |Exiting |Asset Pipeline|Refreshing native|Platform assembly|Starting |Successfully |COMMAND LINE ARGUMENTS:)",
    re.I,
)


class Journal:
    def __init__(self):
        STORE.mkdir(mode=0o700, parents=True, exist_ok=True)
        if os.name != "nt" and STORE.stat().st_mode & 0o077:
            raise ValueError("Collector storage must be private (0700).")
        path = STORE / "journal.sqlite3"
        os.close(os.open(path, os.O_CREAT | os.O_WRONLY, 0o600))
        self.db = sqlite3.connect(path, timeout=10)
        for attempt in range(20):
            try:
                self.db.execute("PRAGMA journal_mode=WAL")
                break
            except sqlite3.OperationalError as exc:
                if "locked" not in str(exc).lower() or attempt == 19:
                    raise
                time.sleep(0.1)
        self.db.executescript("""
            PRAGMA synchronous=FULL; PRAGMA secure_delete=ON;
            CREATE TABLE IF NOT EXISTS events(id TEXT PRIMARY KEY, day TEXT, src TEXT, body BLOB, kind INTEGER,
               build TEXT, version TEXT, receipt TEXT, queued REAL);
            CREATE TABLE IF NOT EXISTS cursors(name TEXT PRIMARY KEY, value TEXT);
        """)

    def state(self, key):
        row = self.db.execute("SELECT value FROM cursors WHERE name=?", (key,)).fetchone()
        return json.loads(row[0]) if row else None

    def mark(self, key, value):
        self.db.execute("INSERT OR REPLACE INTO cursors VALUES(?,?)", (key, json.dumps(value)))

    def queue(self, source, raw, identity, stamp, kind=False, build="", version="", eid=None):
        if len(raw) > archive.MAX:
            self.mark("loss-count", (self.state("loss-count") or 0) + 1)
            return
        if not eid:
            eid = format(int(stamp * 1000), "012x") + hashlib.sha256(identity + raw).hexdigest()[:20]
        day = dt.datetime.fromtimestamp(stamp, dt.timezone.utc).date().isoformat()
        self.db.execute(
            "INSERT OR IGNORE INTO events VALUES(?,?,?,?,?,?,?,NULL,?)",
            (eid, day, source, raw, int(kind), build, version, time.time()),
        )
        cnt, size = self.db.execute(
            "SELECT count(*),coalesce(sum(length(body)),0) FROM events WHERE receipt IS NULL"
        ).fetchone()
        while cnt > 128 or size > BUDGET:
            old, used = self.db.execute(
                "SELECT id,length(body) FROM events WHERE receipt IS NULL ORDER BY queued LIMIT 1"
            ).fetchone()
            self.db.execute("DELETE FROM events WHERE id=?", (old,))
            self.mark("loss-count", (self.state("loss-count") or 0) + 1)
            cnt -= 1
            size -= used

    def flush(self, verbose=True):
        # The local ledger stores a receipt only after an exact, final server acknowledgement.
        pending = self.db.execute(
            "SELECT id,day,src,body,kind,build,version FROM events WHERE receipt IS NULL ORDER BY queued"
        ).fetchall()
        processed = []
        for eid, day, src, body, kind, build, version in pending:
            ack = archive.upload(body, src, day, eid, bool(kind), build=build, game_version=version)
            with self.db:
                self.db.execute("UPDATE events SET body=NULL, receipt=? WHERE id=?", (json.dumps(ack), eid))
            processed.append(ack)
            if verbose:
                print(ack["key"], flush=True)
        with self.db:
            self.db.execute(
                "DELETE FROM events WHERE receipt IS NOT NULL AND queued<?", (time.time() - 30 * 86400,)
            )
            self.db.execute(
                "DELETE FROM events WHERE id IN (SELECT id FROM events WHERE receipt IS NOT NULL ORDER BY queued DESC LIMIT -1 OFFSET 2048)"
            )
        return processed


def is_project_log(path):
    with path.open("rb") as f:
        header = f.read(256 * 1024)
    match = re.search(
        rb"(?im)^Successfully changed project path to:\s*([^\r\n]+)|^-projectpath\r?\n([^\r\n]+)", header
    )
    return bool(match and Path(os.fsdecode(match[1] or match[2]).strip().strip('"')).resolve() == ROOT)


def blocks(lines):
    """Keep complete error messages, blank lines and call stacks. An ordinary record terminates the block."""
    group = []
    flagged = False
    in_stack = False
    for offset, line in lines:
        is_error = bool(ERROR.search(line))
        is_frame = bool(TRACE.search(line))
        separator = (
            group
            and not is_frame
            and not EMPTY.match(line)
            and (
                in_stack or is_error or REGULAR.search(line) or (EMPTY.match(group[-1][1]) and not is_error)
            )
        )
        if separator:
            yield group, flagged
            group = []
            flagged = False
            in_stack = False
        if not flagged and not is_frame and group and not is_error and not EMPTY.match(line):
            # Keep unclassified multiline messages until their severity is revealed by the call stack.
            if REGULAR.search(group[0][1]):
                yield group, False
                group = []
        group.append((offset, line))
        flagged = flagged or is_error or bool(ERROR_CALL.search(line))
        in_stack = in_stack or is_frame
    if group:
        yield group, flagged


def editor_file(journal, path, from_start=False, settle=2.0, key=None):
    key = key or "editor:" + str(path.resolve())
    if not path.is_file():
        return 0
    st = path.stat()
    ident = [st.st_dev, st.st_ino]
    previous = journal.state(key)
    if previous and previous["id"] != ident:
        for old in path.parent.iterdir():
            try:
                if old.is_file() and [old.stat().st_dev, old.stat().st_ino] == previous["id"]:
                    editor_file(journal, old, True, 0, key)
                    break
            except FileNotFoundError:
                continue
    if not is_project_log(path):
        return 0

    def anchor(file, offset):
        begin = max(0, offset - 128)
        file.seek(begin)
        return hashlib.sha256(file.read(offset - begin)).hexdigest()

    start = (
        previous["offset"]
        if previous and previous["id"] == ident and previous["offset"] <= st.st_size
        else 0
        if previous or from_start
        else st.st_size
    )
    done = start
    epoch = st.st_mtime
    collected = 0
    with path.open("rb") as file, journal.db:
        if start and previous and previous.get("anchor") and anchor(file, start) != previous["anchor"]:
            start = 0
            done = 0
        file.seek(start)
        raw = file.read(archive.MAX + 1)
        # Do not read a continuously growing log into unbounded memory.
        final = start + len(raw) >= st.st_size and time.time() - epoch >= settle
        lines = []
        offset = start
        for ln in raw.splitlines(keepends=True):
            if not ln.endswith((b"\n", b"\r")) and not final:
                break
            lines.append((offset, ln))
            offset += len(ln)
        groups = blocks(lines)
        previous_group = None

        def finish(group):
            nonlocal collected, done
            seq, flag = group
            if flag:
                entry = b"".join(x[1] for x in seq)
                data = clean(entry.decode("utf-8", "replace"), archive.master()).encode()
                anchor = (str(ident) + ":" + str(seq[0][0])).encode()
                journal.queue("editor-log", data, anchor, epoch)
                collected += 1
            done = seq[-1][0] + len(seq[-1][1])

        for group in groups:
            if previous_group:
                finish(previous_group)
            previous_group = group
        if previous_group and final:
            finish(previous_group)
        if len(raw) > archive.MAX and done == start:
            # Strict upper bound per record; count the capacity loss explicitly.
            journal.mark("loss-count", (journal.state("loss-count") or 0) + 1)
            done = offset or start + len(raw)
        journal.mark(key, {"id": ident, "offset": done, "anchor": anchor(file, done)})
    return collected


def adb_once(journal, phone, since=None):
    from device import PACKAGE
    from logs import FORMAT, system_crash_record

    uid = phone.uid()
    # The serial stays off disk. This digest is local cursor state, never transmitted.
    scope = hashlib.sha256((phone.row["_serial"] + ":" + uid).encode()).hexdigest()
    key = "adb:" + scope
    clock = int(phone.shell("date", "+%s"))
    previous = journal.state(key)
    old_mark = previous.get("from") if isinstance(previous, dict) else previous
    emitted = previous.get("through", {}) if isinstance(previous, dict) else {}
    start = float(old_mark if old_mark is not None else since if since is not None else clock)
    if start > clock:
        start, emitted = clock - 3, {}
    next_start = max(start, clock - 3)
    through = dict(emitted)
    streams = [
        ("app", ["-b", "main", "-b", "system", "-b", "crash", "--uid=" + uid, "*:V"]),
        (
            "system",
            [
                "-b",
                "system",
                "-b",
                "events",
                "-b",
                "crash",
                "-b",
                "main",
                "ActivityManager:I",
                "ActivityTaskManager:E",
                "am_crash:I",
                "am_anr:I",
                "am_proc_died:I",
                "DEBUG:F",
                "libc:F",
                "*:S",
            ],
        ),
    ]
    count = 0
    with journal.db:
        for stream, filters in streams:
            args = phone.base + [
                "logcat",
                "-d",
                "-v",
                "epoch",
                "-T",
                format(max(0, start), ".3f"),
                *filters,
            ]
            proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
            pending = {}
            owners = {}

            def complete(k):
                nonlocal count
                begin, when, parts = pending.pop(k)
                through[stream + "|" + "|".join(k)] = when
                raw = clean(b"".join(parts).decode("utf-8", "replace"), archive.master()).encode()
                journal.queue("adb", raw, (PACKAGE + stream + str(begin)).encode(), begin)
                count += 1

            for raw in proc.stdout:
                match = FORMAT.match(raw.decode("utf-8", "replace").rstrip("\r\n"))
                if not match:
                    continue
                secs, pid, tid, level, tag, msg = match.groups()
                at = float(secs)
                if at > clock - 1:
                    continue  # leave the open tail for next poll
                k = (pid, tid, tag.strip())
                if stream == "system":
                    owned = system_crash_record(owners, at, pid, tid, k[2], msg)
                    named = re.search(r"(?<![\w.])" + re.escape(PACKAGE) + r"(?![\w.])", msg)
                    death = named and (
                        k[2] == "am_proc_died" or k[2] == "ActivityManager" and "has died" in msg
                    )
                    if not owned and not death:
                        continue
                if at < start or at <= emitted.get(stream + "|" + "|".join(k), -1):
                    continue
                severe = level in "EF" or stream == "system"
                continuation = bool(TRACE.search(msg.encode())) or msg.strip() in (
                    "Stacktrace:",
                    "Stack trace:",
                    "",
                )
                if not continuation and k in pending and at == pending[k][1] and tag.strip() == "Unity":
                    continuation = True
                if k in pending and (at - pending[k][1] > 2 or not severe and not continuation):
                    complete(k)
                if k in pending and (severe or continuation):
                    pending[k][1] = at
                    pending[k][2].append(raw)
                elif severe:
                    pending[k] = [at, at, [raw]]
            if proc.wait(timeout=20):
                raise IOError("ADB Logcat read failed; retaining cursor.")
            for k in list(pending):
                if pending[k][1] >= clock - 3:
                    next_start = min(next_start, pending[k][0])
                else:
                    complete(k)
        journal.mark(
            key, {"from": next_start, "through": {k: v for k, v in through.items() if v >= next_start}}
        )
    return count


NATIVE_NAMES = {
    "ian growing",
    "ian_growing",
    "iangrowing",
    "이안키우기",
    "이안 키우기",
    "iancloud_isolated_qa",
    "iancloudqa",
}
MEMORY_TEXT = re.compile(
    r"(?im)^\s*(?:raw (?:stack|memory) dump|stack memory|memory near (?:pc|sp)|bytes at (?:cs:eip|pc))\s*[:(]"
)


def native_identity(text):
    """Only the crashed process itself establishes ownership. Incidental paths cannot."""
    from device import PACKAGE

    lower = text[:16384].lower()
    names = r"(?:process|application name|faulting application name|app_name|procname)"
    identities = re.findall(
        r"""(?im)(?:^\s*|["'])""" + names + r"""["']?\s*:\s*["']?([^"'\[\r\n,}]+)""", lower
    )
    if any(
        Path(x.strip().replace("\\", "/")).name.removesuffix(".exe") in NATIVE_NAMES for x in identities
    ):
        return True
    ids = re.findall(
        r"""(?im)(?:^\s*identifier|["'](?:bundleid|cfbundleidentifier)["'])\s*:\s*["']?([\w.-]+)""", lower
    )
    return any(x in (PACKAGE.lower(), "com.iangrowing.qa.internalcloud") for x in ids)


def queue_native(journal, path):
    if not path.is_file() or path.is_symlink() or path.stat().st_size > archive.MAX:
        return False
    if path.suffix.lower() not in (".crash", ".ips", ".log", ".txt"):
        return False
    statekey = "native-file:" + hashlib.sha256(os.fsencode(path.resolve())).hexdigest()
    st = path.stat()
    signature = [st.st_dev, st.st_ino, st.st_size, st.st_mtime_ns]
    if journal.state(statekey) == signature:
        return True
    with path.open("rb") as file:
        header = file.read(16384)
        if b"\0" in header or not native_identity(header.decode("utf-8", "replace")):
            return False
        data = header + file.read(archive.MAX + 1 - len(header))
    if len(data) > archive.MAX or b"\0" in data:
        return False
    try:
        report = data.decode("utf-8-sig")
    except UnicodeDecodeError:
        return False
    if MEMORY_TEXT.search(report):
        return False
    with journal.db:
        journal.queue("native-crashes", clean(report, archive.master()).encode(), b"native", st.st_mtime)
        journal.mark(statekey, signature)
    return True


def watch_native(journal, folder):
    if not folder.is_dir():
        return
    paths = set()
    for pattern in ("Ian*.ips", "Ian*.crash", "이안*.ips", "이안*.crash", "error.log", "Crash_*/error.log"):
        paths.update(folder.glob(pattern))
    for path in paths:
        if path.is_file() and time.time() - path.stat().st_mtime >= 5:
            queue_native(journal, path)
