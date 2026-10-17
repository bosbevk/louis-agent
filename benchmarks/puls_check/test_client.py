"""The API client (F31 step 7a) against a local stand-in for Puls's API: a session is opened with its body and the
key, a message's stream comes back as events, and the session is deleted. No network beyond 127.0.0.1, no model."""
import json
import threading
import unittest
from http.server import BaseHTTPRequestHandler, HTTPServer

from benchmarks.puls_check.client import Client


class Puls(BaseHTTPRequestHandler):
    seen = []

    def log_message(self, *args):
        pass

    def _body(self):
        length = int(self.headers.get("Content-Length") or 0)
        return json.loads(self.rfile.read(length) or b"null")

    def do_POST(self):
        Puls.seen.append((self.command, self.path, self.headers.get("x-api-key"), self._body()))
        if self.path == "/sessions":
            payload = json.dumps({"session_id": "sess_1"}).encode()
            self.send_response(201)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(payload)
        else:
            self.send_response(200)
            self.send_header("Content-Type", "text/event-stream")
            self.end_headers()
            self.wfile.write(b'event: content_block_delta\ndata: {"delta":{"type":"text_delta","text":"Hallo"}}\n\n'
                             b'event: message_stop\ndata: {"type":"message_stop"}\n\n')

    def do_GET(self):
        Puls.seen.append((self.command, self.path, self.headers.get("x-api-key"), None))
        payload = json.dumps({"toolsets": [{"name": "web", "available": True}, {"name": "paymo", "available": False}]}).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(payload)

    def do_DELETE(self):
        Puls.seen.append((self.command, self.path, self.headers.get("x-api-key"), None))
        self.send_response(200)
        self.end_headers()


class TheClient(unittest.TestCase):
    def setUp(self):
        Puls.seen = []
        self.server = HTTPServer(("127.0.0.1", 0), Puls)
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        self.client = Client(f"http://127.0.0.1:{self.server.server_port}/", key="k3y")

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()

    def test_a_session_is_opened_asked_and_deleted_with_the_key(self):
        session = self.client.open_session(toolsets=["web"], personality="puls/voice-af", run="r1:puls-only:1", task="bobotie")
        events = list(self.client.message(session, "Bobotie?"))
        self.client.close(session)

        self.assertEqual(session, "sess_1")
        self.assertEqual([e for e, _ in events], ["content_block_delta", "message_stop"])
        self.assertEqual(Puls.seen[0][:3], ("POST", "/sessions", "k3y"))
        self.assertEqual(Puls.seen[0][3], {"toolsets": ["web"], "personality": "puls/voice-af", "run": "r1:puls-only:1", "task": "bobotie"})
        self.assertEqual(Puls.seen[1][:2], ("POST", "/sessions/sess_1/messages"))
        self.assertEqual(Puls.seen[1][3], {"message": "Bobotie?"})
        self.assertEqual(Puls.seen[2][:2], ("DELETE", "/sessions/sess_1"))

    def test_a_field_left_none_isnt_sent(self):
        self.client.open_session(toolsets=["web"], location=None)
        self.assertEqual(Puls.seen[0][3], {"toolsets": ["web"]})

    def test_the_toolsets_are_the_ones_this_instance_has(self):
        self.assertEqual(self.client.toolsets(), ["web"])


if __name__ == "__main__":
    unittest.main()
