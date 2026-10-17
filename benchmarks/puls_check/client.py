"""Puls's API, as a check calls it (F31): open a session with its toolsets, personality and tags, send it messages
and read their streams, delete it. The API is PULS_API (the runner sets it to the container's own port) and, when
the API wants one, the key is AGENT_API_KEY, which the script has from the container's environment."""
import json
import os
import urllib.request

from benchmarks.puls_check.stream import parse_sse

TIMEOUT = 600   # a cook turn can take minutes


class Client:
    def __init__(self, api=None, key=None):
        self.api = (api or os.environ.get("PULS_API") or "http://127.0.0.1:5070/").rstrip("/") + "/"
        self.key = key if key is not None else os.environ.get("AGENT_API_KEY") or None

    def _request(self, method, path, body=None):
        headers = {"Content-Type": "application/json"}
        if self.key:
            headers["x-api-key"] = self.key
        data = json.dumps(body).encode() if body is not None else None
        return urllib.request.urlopen(urllib.request.Request(self.api + path, data=data, method=method, headers=headers), timeout=TIMEOUT)

    def open_session(self, **body):
        """A new session; fields left None aren't sent, so the API's defaults hold. Returns its id."""
        with self._request("POST", "sessions", {k: v for k, v in body.items() if v is not None}) as response:
            return json.loads(response.read())["session_id"]

    def message(self, session, text):
        """The message's stream, as (event, data) pairs as they arrive."""
        with self._request("POST", f"sessions/{session}/messages", {"message": text}) as response:
            yield from parse_sse(response)

    def close(self, session):
        with self._request("DELETE", f"sessions/{session}") as response:
            response.read()
