"""Reading a session's stream (F31): the API's server-sent events, Claude's shape plus our own sub_session (a
specialist's events, F5; a minion's nested inside them, F30) and error. One turn becomes what the checks score: the
answer, Puls's calls, the role it handed to or the sub-session it continued, the specialist's calls, the thinking
and the error. Held to the same counts as the web app's TurnStream by fixtures/session-stream.sse (CL11: one C#
reader later)."""
import json
from urllib.parse import urlparse

DELEGATIONS = ("StartSubSession", "ContinueSubSession")
PAGE_READERS = ("FetchUrl", "ReadRenderedPage")


def parse_sse(lines):
    """(event, data) for each event of a stream: its 'event:' line, then its 'data:' JSON, ended by a blank line."""
    event, data = None, []
    for raw in lines:
        line = raw.decode("utf-8") if isinstance(raw, bytes) else raw
        line = line.rstrip("\r\n")
        if not line:
            if event and data:
                yield event, json.loads("\n".join(data))
            event, data = None, []
        elif line.startswith("event:"):
            event = line[6:].strip()
        elif line.startswith("data:"):
            data.append(line[5:].lstrip(" ") if line.startswith("data: ") else line[5:])
    if event and data:
        yield event, json.loads("\n".join(data))


def read_turn(events):
    """What one turn's events say. A specialist's events arrive as sub_session; deeper ones (a minion's) nest again."""
    turn = {"answer": "", "calls": [], "role": None, "continued": None, "specialist_calls": [],
            "thinking": "", "specialist_thinking": "", "error": None}
    text, thinking, specialist_thinking = [], [], []
    for event, data in events:
        if event == "content_block_start" and (data.get("content_block") or {}).get("type") == "tool_use":
            block = data["content_block"]
            arguments = block.get("input") or {}
            turn["calls"].append({"name": block["name"], "input": arguments})
            if block["name"] == "StartSubSession" and turn["role"] is None:
                turn["role"] = arguments.get("role") or arguments.get("personality")   # CL14: recorded before, personality
            elif block["name"] == "ContinueSubSession" and turn["continued"] is None:
                turn["continued"] = arguments.get("handle")
        elif event == "content_block_delta":
            delta = data.get("delta") or {}
            if delta.get("type") == "text_delta":
                text.append(delta.get("text", ""))
            elif delta.get("type") == "thinking_delta":
                thinking.append(delta.get("thinking", ""))
        elif event == "sub_session":
            _specialist(data, turn["specialist_calls"], specialist_thinking)
        elif event == "error":
            turn["error"] = ((data.get("error") or {}).get("message")) or json.dumps(data, ensure_ascii=False)
    turn["answer"] = "".join(text).strip()
    turn["thinking"] = "".join(thinking).strip()
    turn["specialist_thinking"] = "".join(specialist_thinking).strip()
    return turn


def _specialist(data, calls, thinking):
    """A sub_session event: the specialist's own call or thinking; a nested sub_session (a minion's) goes one deeper."""
    inner_event, inner = data.get("event"), data.get("data") or {}
    if inner_event == "content_block_start" and (inner.get("content_block") or {}).get("type") == "tool_use":
        block = inner["content_block"]
        calls.append({"name": block["name"], "input": block.get("input") or {}})
    elif inner_event == "content_block_delta" and (inner.get("delta") or {}).get("type") == "thinking_delta":
        thinking.append(inner["delta"].get("thinking", ""))
    elif inner_event == "sub_session":
        _specialist(inner, calls, thinking)


def hosts(calls):
    """The hosts pages were read from (FetchUrl, ReadRenderedPage), in order, without repeats, without 'www.'."""
    seen = []
    for c in calls:
        if c["name"] in PAGE_READERS and (url := (c.get("input") or {}).get("url")):
            host = (urlparse(url).hostname or "").lower().removeprefix("www.")
            if host and host not in seen:
                seen.append(host)
    return seen
