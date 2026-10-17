"""The line protocol's writer (F31 step 7a): what benchmarks/puls_check/protocol.py prints is what the API's
BenchmarkProtocol reads."""
import io
import json
import unittest
from contextlib import redirect_stdout

from benchmarks.puls_check import protocol


def printed(action):
    out = io.StringIO()
    with redirect_stdout(out):
        action()
    return out.getvalue().splitlines()


class TheWriter(unittest.TestCase):
    def test_an_event_is_at_and_one_json_object_with_its_time(self):
        [line] = printed(lambda: protocol.emit("mode", mode="puls-cook", repeat=1, toolsets=["web"]))
        self.assertTrue(line.startswith("@"))
        event = json.loads(line[1:])
        self.assertEqual((event["event"], event["mode"], event["repeat"], event["toolsets"]), ("mode", "puls-cook", 1, ["web"]))
        self.assertIsInstance(event["t"], float)

    def test_the_first_line_names_the_version(self):
        [line] = printed(protocol.start)
        self.assertEqual(json.loads(line[1:])["version"], protocol.VERSION)

    def test_a_plain_line_never_starts_with_at(self):
        [line] = printed(lambda: protocol.say("@ not an event"))
        self.assertFalse(line.startswith("@"))

    def test_the_run_tag_is_the_run_mode_and_repeat(self):
        self.assertEqual(protocol.run_tag("abc123", "puls-cook", 2), "abc123:puls-cook:2")


class AnyConsole(unittest.TestCase):
    """F31 step 7b's replay found it: run by hand on Windows, stdout is cp1252, and a recorded turn's curly quote or
    emoji raised mid-stream, ending its conversation. The runner sets PYTHONIOENCODING, but the writer can't rely on it."""

    def _on(self, encoding, action):
        raw = io.BytesIO()
        console = io.TextIOWrapper(raw, encoding=encoding)
        with redirect_stdout(console):
            action()
        console.flush()
        return raw.getvalue().decode(encoding).splitlines()

    def test_an_event_with_any_character_is_written_and_reads_back(self):
        [line] = self._on("cp1252", lambda: protocol.emit("question", text="Die kok s’n pannekoek 🍳"))
        self.assertEqual(json.loads(line[1:])["text"], "Die kok s’n pannekoek 🍳")

    def test_a_plain_line_with_any_character_never_stops_the_run(self):
        [line] = self._on("cp1252", lambda: protocol.say("Klaar 🍳"))
        self.assertTrue(line.startswith("Klaar"))


if __name__ == "__main__":
    unittest.main()
