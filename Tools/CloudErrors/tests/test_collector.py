import hashlib
import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import collect
import privacy


class DeveloperJournalTests(unittest.TestCase):
    def setUp(self):
        self.home = tempfile.TemporaryDirectory()
        self.root = Path(self.home.name).resolve()
        self.patchers = [
            patch.object(collect, "STORE", self.root / "private"),
            patch.object(collect, "ROOT", self.root),
            patch.dict(os.environ, IAN_INGEST_TOKEN="synthetic-fixture-never-transmit"),
        ]
        for p in self.patchers:
            p.start()
        self.journal = collect.Journal()
        self.file = self.root / "Editor.log"
        self.header = ("Successfully changed project path to: " + str(self.root) + "\r\n").encode()
        self.file.write_bytes(self.header)

    def tearDown(self):
        self.journal.db.close()
        for p in self.patchers:
            p.stop()
        self.home.cleanup()

    def payloads(self):
        return [r[0] for r in self.journal.db.execute("select body from events order by queued")]

    def test_scope_restart_rotation_truncation(self):
        self.assertEqual(collect.editor_file(self.journal, self.file), 0)
        error = b"Assets/X.cs(7,2): error CS1001: first\r\n at Sample.X()\r\n"
        with self.file.open("ab") as f:
            f.write(error)
        self.assertEqual(collect.editor_file(self.journal, self.file, settle=0), 1)
        self.journal.db.close()
        self.journal = collect.Journal()
        self.assertEqual(collect.editor_file(self.journal, self.file, settle=0), 0)
        with self.file.open("ab") as f:
            f.write(error.replace(b"first", b"late"))
        self.file.rename(self.root / "Editor-prev.log")
        self.file.write_bytes(self.header + error.replace(b"first", b"rotated"))
        collect.editor_file(self.journal, self.file, settle=0)
        self.file.write_bytes(
            self.header + error.replace(b"first", b"same inode copy-truncated and already regrown")
        )
        collect.editor_file(self.journal, self.file, settle=0)
        self.assertEqual(len(self.payloads()), 4)
        self.assertEqual(self.payloads()[0], error)
        self.assertIn(b"late", self.payloads()[1])
        self.assertIn(b"rotated", self.payloads()[2])
        self.assertIn(b"regrown", self.payloads()[3])
        self.file.write_bytes(b"Successfully changed project path to: /different/project\n" + error)
        self.assertEqual(collect.editor_file(self.journal, self.file, True, 0), 0)

    def test_multiline_frames_and_ordinary_neighbor(self):
        ordinary = b"An ordinary gameplay log\r\nUnityEngine.Debug:Log (object)\r\n\r\n"
        block = (
            b"First part of a message\r\nSecond part\r\nUnityEngine.Debug:LogError (object)\r\n"
            b"Example/<>c:<Do>b__1_0 ()\r\nUnityEngine.UnitySynchronizationContext/WorkRequest:Invoke ()\r\n"
            b"\r\n(Filename: Assets/X.cs Line: 5)\r\n\r\n"
        )
        self.file.write_bytes(self.header + ordinary + block + b"Importing file\r\n")
        self.assertEqual(collect.editor_file(self.journal, self.file, True, 0), 1)
        self.assertEqual(self.payloads(), [block])

    def test_masking_and_frame_fidelity(self):
        confidential = (
            "Authorization: Bearer demo-secret\r\nuserInput='private words'\n"
            "test@example.org /Users/Some Person/Assets/Qa.cs:41 C:\\Users\\Some Person\\Assets\\Qa.cs:41 "
            "10.0.0.1 2001:db8::1 ::1 00:1A:2B:3C:4D:5E 010-1234-5678 4111111111111111\n"
            "CrashReporter Key: fake-os-identifier"
        )
        cleaned = privacy.clean(confidential)
        for value in (
            "demo-secret",
            "private words",
            "example.org",
            "Some Person",
            "10.0.0.1",
            "2001:db8::1",
            "::1",
            "00:1A:2B:3C:4D:5E",
            "010-1234-5678",
            "4111111111111111",
            "fake-os-identifier",
        ):
            self.assertNotIn(value, cleaned)
        self.assertIn("Assets/Qa.cs:41", cleaned)
        self.assertEqual(cleaned.count("\n"), confidential.count("\n"))
        line = "at Ian.Example(Assets/Scripts/Qa.cs:41)\r\n"
        self.assertEqual(privacy.clean(line * 300), line * 300)

    def test_native_report_identity_and_memory(self):
        path = self.root / "Ian_Growing.crash"
        path.write_text("Process: OtherApp [124]\nLoaded Library: Ian_Growing\n")
        self.assertFalse(collect.queue_native(self.journal, path))
        path.write_text("Process: Ian_Growing [124]\nStack memory:\n00000000\n")
        self.assertFalse(collect.queue_native(self.journal, path))
        path.write_text(
            "Process: Ian_Growing [124]\nCrashReporter Key: sensitive-fixture\nThread 0 Crashed:\n0 Ian.Sample\n"
        )
        self.assertTrue(collect.queue_native(self.journal, path))
        self.assertTrue(collect.queue_native(self.journal, path))
        self.assertEqual(len(self.payloads()), 1)
        self.assertIn(b"0 Ian.Sample", self.payloads()[0])
        self.assertNotIn(b"sensitive-fixture", self.payloads()[0])

    def test_offline_ack_and_dedupe(self):
        before = b"synthetic\n with a full frame\n"
        with self.journal.db:
            for _ in range(2):
                self.journal.queue("adb", before, b"test", 123456)
        self.assertEqual(self.payloads(), [before])
        with patch.object(collect.archive, "upload", side_effect=OSError("unavailable")):
            with self.assertRaises(OSError):
                self.journal.flush(False)
        self.assertEqual(self.payloads(), [before])
        with patch.object(
            collect.archive,
            "upload",
            return_value={"key": "fixture", "sha256": hashlib.sha256(before).hexdigest()},
        ):
            self.journal.flush(False)
        self.assertEqual(self.payloads(), [None])


if __name__ == "__main__":
    unittest.main()
