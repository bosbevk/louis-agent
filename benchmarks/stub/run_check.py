"""The stub check (F31 step 6c): a canned run in the portal's protocol, free, for every hand check of the portal.

It opens no session and calls no model, so a run costs nothing: two modes ("puls-only" with web, "puls-cook" with
specialists and web), two conversations each (bobotie with a follow-up, cottage pie), every turn's stream in the
API's own event shapes (thinking, text, a tool call, the cook's events nested as sub_session), so the portal's live
view and its replay draw them as they draw a real run. Standard library only.

Usage (the portal runs it): python3 benchmarks/stub/run_check.py --repeat N
"""

import argparse
import json
import os
import sys
import time

PROTOCOL = 1

MODES = [
    ("puls-only", ["web"]),
    ("puls-cook", ["specialists", "web"]),
]

CONVERSATIONS = [
    ("bobotie", ["Maak vir my bobotie vir 4 mense.", "En as ek dit vir 6 wil maak?"]),
    ("cottage-pie", ["Hoe maak ek cottage pie?"]),
]


def emit(event, **fields):
    """One protocol line: '@' and one JSON object."""
    print("@" + json.dumps({"event": event, "t": round(time.time(), 3), **fields}, ensure_ascii=False), flush=True)


def stream(mode, repeat, task, turn, cook):
    """The turn's events as the API streams them; with the cook, a StartSubSession call and its nested events."""
    key = {"mode": mode, "repeat": repeat, "task": task, "turn": turn}
    events = [("message_start", {"type": "message_start", "message": {"id": f"msg_{task}_{turn}"}})]
    index = 0

    def block(content_block, deltas=()):
        nonlocal index
        events.append(("content_block_start", {"type": "content_block_start", "index": index, "content_block": content_block}))
        for delta in deltas:
            events.append(("content_block_delta", {"type": "content_block_delta", "index": index, "delta": delta}))
        events.append(("content_block_stop", {"type": "content_block_stop", "index": index}))
        index += 1

    block({"type": "thinking", "thinking": ""}, [{"type": "thinking_delta", "thinking": "Wat vra hulle? "}, {"type": "thinking_delta", "thinking": "'n Resep."}])
    if cook:
        call = f"toolu_{task}_{turn}"
        block({"type": "tool_use", "id": call, "name": "StartSubSession",
               "input": {"agent": "puls", "personality": "puls-cook", "task": task}})
        for inner, data in [
            ("content_block_start", {"type": "content_block_start", "index": 0, "content_block": {"type": "text", "text": ""}}),
            ("content_block_delta", {"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "Die kok se resep."}}),
            ("content_block_stop", {"type": "content_block_stop", "index": 0}),
        ]:
            events.append(("sub_session", {"tool_use_id": call, "agent": "puls", "event": inner, "data": data}))
        block({"type": "tool_result", "tool_use_id": call, "content": "Die kok se resep.", "is_error": False})
    block({"type": "text", "text": ""}, [{"type": "text_delta", "text": f"Hier is {task}, "}, {"type": "text_delta", "text": "stap vir stap."}])
    events.append(("message_delta", {"type": "message_delta", "delta": {"stop_reason": "end_turn"}, "usage": None}))
    events.append(("message_stop", {"type": "message_stop"}))
    for type_, data in events:
        emit("sse", **key, type=type_, data=data)
    return f"Hier is {task}, stap vir stap."


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--repeat", type=int, default=1)
    args = parser.parse_args()
    run = os.environ.get("BENCHMARK_RUN", "local")

    emit("protocol", version=PROTOCOL)
    print(f"Stub check, run {run}, {args.repeat} repeat(s): no session, no model, no cost.", flush=True)
    for repeat in range(1, args.repeat + 1):
        for mode, toolsets in MODES:
            cook = "specialists" in toolsets
            emit("mode", mode=mode, repeat=repeat, toolsets=toolsets, personality="puls/voice-af", llm=None,
                 conversations=len(CONVERSATIONS))
            for task, questions in CONVERSATIONS:
                session = f"sess_stub_{mode}_{repeat}_{task}"
                started = time.time()
                for turn, question in enumerate(questions, start=1):
                    emit("question", mode=mode, repeat=repeat, task=task, turn=turn, session=session, text=question)
                    answer = stream(mode, repeat, task, turn, cook)
                    emit("turn", mode=mode, repeat=repeat, task=task, turn=turn, session=session, question=question,
                         answer=answer, role="puls-cook" if cook else None, continued="sub-1" if cook and turn > 1 else None,
                         puls_calls=1 if cook else 0, specialist_calls=3 if cook else 0,
                         hosts=["www.checkers.co.za"] if cook else [], seconds=0.1, error=None)
                seconds = round(time.time() - started, 1)
                emit("conversation", mode=mode, repeat=repeat, task=task, role="puls-cook" if cook else None,
                     scores={"routed_right": True, "turns": len(questions)}, errors=0, seconds=seconds)
                print(f"{mode:9} {task:12} repeat={repeat} turns={len(questions)}", flush=True)
            emit("mode_done", mode=mode, repeat=repeat)
    print("Stub check done.", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
