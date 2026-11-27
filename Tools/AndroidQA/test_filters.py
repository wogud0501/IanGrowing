"""Regression cases for scoped crash capture and bounded, offline Logcat queries."""
import tempfile
import unittest
from pathlib import Path

from device import PACKAGE, clean
from logs import PRESETS, matches, query, system_crash_record


class Filters(unittest.TestCase):
    def test_presets(self):
        rows = [
            ("Live", "I Unity: Ready"), ("Debug", "V Engine: detail"),
            ("Warning", "W Unity: warning"), ("Error", "E Unity: error"),
            ("Crash", "F libc: Fatal signal 6 (SIGABRT)"),
            ("Quest", "I Unity: [IAN][REWARD] Claimed M001"),
            ("Save", "I Unity: [IAN][SAVE] SaveCompleted"),
            ("UI", "I Unity: [IAN][UI] WindowOpened"),
            ("Network", "E Unity: SocketException"),
            ("Performance", "I Choreographer: Skipped 10 frames"),
            ("Legal", "I Unity: [IAN][LEGAL] RequiredDocumentsAcknowledged")]
        self.assertEqual(set(PRESETS), set(preset for preset, _ in rows))
        for preset, body in rows:
            self.assertTrue(matches("1234.001 1 2 " + body, preset), preset)
        self.assertFalse(matches("1234.001 1 2 V Other: noise", "Live"))
        self.assertFalse(matches("1234.001 1 2 I Unity: saved menu layout", "Save"))

    def test_system_correlation_after_process_exit(self):
        known = {}
        def accepted(t, msg, tag="DEBUG"):
            return system_crash_record(known, t, "22", "33", tag, msg)
        self.assertFalse(accepted(0, "ANR in " + PACKAGE + ".another", "ActivityManager"))
        self.assertTrue(accepted(0, "ANR in " + PACKAGE, "ActivityManager"))
        self.assertTrue(accepted(1, ">>> " + PACKAGE + " <<<"))
        self.assertTrue(accepted(2, "  #00 pc 000001 libc"))
        self.assertFalse(accepted(12, "  #01 pc 000002 libc"))
        self.assertFalse(accepted(13, ">>> another.app <<<"))
        self.assertFalse(accepted(14, "  #00 pc 000001 libc"))

    def test_redaction(self):
        for data in ["Authorization: Bearer secret-value", "api_key=secret-value", "qa@example.invalid",
                     "/Users/privateuser/test.cs", "192.0.2.5", "4111 1111 1111 1111"]:
            self.assertNotIn(data, clean(data))
        self.assertIn("Operation.cs:36", clean("at /Users/privateuser/Assets/Operation.cs:36"))

    def test_ranges_rotations_and_complete_fatal_stack(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)
            (path / "logcat.log.1").write_text("101.00 1 1 I Unity: previous\n")
            (path / "logcat.log").write_text("102.00 2 2 I Unity: current\n")
            self.assertEqual(len(query(path, "Live")), 2)
            self.assertEqual(len(query(path, "Live", since=101.5, until=102.2)), 1)
            (path / "crash.log").write_text("101.00 1 1 E AndroidRuntime:    at game.start()\n")
            self.assertEqual(len(query(path, "Crash")), 1)


if __name__ == "__main__":
    unittest.main()
