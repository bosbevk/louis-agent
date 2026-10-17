"""The cook check (F25, F26; rewritten for the portal in F31): what does Puls's cook add? The same conversations with
Puls alone (`puls-only`: `web`, so it can't delegate and cooks itself) and Puls with its cook (`puls-cook`:
`specialists` and `web`, Puls as built). Each conversation is one session; a second message is the follow-up.

What is scored is what the agents called, not only what they wrote: the role Puls handed to, the hosts the pages came
from (Puls's own reads and the cook's), the cook's rating (priced ingredients over ingredients), rand amounts, the
"unknown" marks, and whether a follow-up went back to the same cook. The cost is the API's: it reads the usage ledger
for the run's tag, never this script.

Spends money (Claude). The admin portal runs it (Maatstawwe); by hand, with the Puls containers up:
    BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/cook/run_check.py --repeat 1
"""
import argparse
import os
import sys
import time

# Run as a script from the repo root (the portal does): the harness is a package beside it.
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

from benchmarks.puls_check import protocol, scoring, stream  # noqa: E402
from benchmarks.puls_check.client import Client  # noqa: E402

PERSONALITY = "puls/voice-af"
TIME_ZONE = "Africa/Johannesburg"

MODES = {
    "puls-only": ["web"],                 # Puls alone: it can't delegate, so it cooks and prices itself
    "puls-cook": ["specialists", "web"],  # Puls as built: the web itself, cooking to the cook
}

# One session each; a second message is the follow-up. The place goes with the session (F22), as the web app sends it,
# except cook-en, which says its town in the message: the cook's role covers both.
CONVERSATIONS = [
    ("cook-af", "Stellenbosch, Wes-Kaap, Suid-Afrika", [
        "Wat kan ek vanaand vir vier mense kook vir onder R250? Gee die resep en 'n inkopielys met pryse.",
        "Maak dit eerder vir ses mense. Wat kos dit nou?",
    ]),
    ("cook-en", None, [
        "What should I cook tonight in Pretoria for two? I have about an hour. Give me the recipe and what it'll cost at the shops.",
        "Swap the meat for chicken. What does the shopping list look like now?",
    ]),
    # Louis's own question (2026-10-15): two shops' prices with their links, one shop for everything, the branches.
    ("cook-shops", "Stellenbosch, Wes-Kaap, Suid-Afrika", [
        "Ek is in Stellenbosch, kook vanaand vir vier mense en het so 'n uur. Wat moet ek maak? Gee my die resep, elke "
        "bestanddeel se prys by twee winkels hier naby met die skakel waar jy die prys gekry het, by watter winkel ek "
        "alles in een slag kry, die takke se adresse, en wat die goedkoopste mandjie kos.",
    ]),
    # The control: a plain web question, which Puls answers itself (F29: only the cook is on).
    ("control-web", "Stellenbosch, Wes-Kaap, Suid-Afrika", [
        "Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.",
    ]),
]

SHOPS = ("checkers.co.za", "pnp.co.za", "woolworths.co.za", "spar.co.za", "shoprite.co.za", "usave.co.za")


def score(task, turns, cook_on):
    """One conversation as numbers. A meal goes to the cook when the mode has it; otherwise, and for the control
    question, Puls answers itself. An errored first turn is never right."""
    first, follow = turns[0], (turns[1] if len(turns) > 1 else None)
    n_priced, n_items = scoring.priced(first["answer"])
    fetched = stream.hosts([c for t in turns for c in t["calls"] + t["specialist_calls"]])
    local = [h for h in fetched if h.endswith(".za")]
    shops = [h for h in fetched if h.endswith(SHOPS)]
    expected = "puls-cook" if task.startswith("cook") and cook_on else None
    answers = [t["answer"] for t in turns]
    result = {
        "role": first["role"],
        "routed_right": not first["error"] and first["role"] == expected,
        "answered_itself": first["role"] is None and not first["error"],
        "hosts": fetched, "local_hosts": local, "shop_hosts": shops,
        "sources_local": bool(fetched) and len(local) == len(fetched),
        "priced_at_shops": bool(shops),
        "ingredients": n_items, "priced": n_priced,
        "priced_ratio": round(n_priced / n_items, 2) if n_items else None,
        "rand_amounts": sum(scoring.rand_amounts(a) for a in answers),
        "unconfirmed_marks": sum(scoring.unconfirmed_marks(a) for a in answers),
        "errors": sum(1 for t in turns if t["error"]),
        "follow_up": None, "follow_up_handle": None,
    }
    if follow:
        result["follow_up"] = "continued" if follow["continued"] else ("new sub-session" if follow["role"] else "answered itself")
        result["follow_up_handle"] = follow["continued"]
    return result


def converse(client, run, mode, repeat, toolsets, task, location, messages):
    """One conversation in a new session: each message, its stream forwarded as it comes, and what the turn read."""
    session = client.open_session(toolsets=toolsets, personality=PERSONALITY, time_zone=TIME_ZONE, location=location,
                                  task=task, run=protocol.run_tag(run, mode, repeat))
    key = {"mode": mode, "repeat": repeat, "task": task}
    turns = []
    try:
        for n, message in enumerate(messages, 1):
            protocol.emit("question", **key, turn=n, session=session, text=message)
            started, events = time.time(), []
            try:
                for event, data in client.message(session, message):
                    protocol.emit("sse", **key, turn=n, type=event, data=data)
                    events.append((event, data))
                read = stream.read_turn(events)
            except Exception as ex:  # a dead container, a timeout: this turn failed, the run goes on
                read = {**stream.read_turn(events), "error": repr(ex)}
            read["seconds"] = round(time.time() - started, 1)
            turns.append(read)
            protocol.emit("turn", **key, turn=n, session=session, question=message, answer=read["answer"], role=read["role"],
                          continued=read["continued"], puls_calls=len(read["calls"]), specialist_calls=len(read["specialist_calls"]),
                          hosts=stream.hosts(read["calls"] + read["specialist_calls"]), seconds=read["seconds"], error=read["error"])
    finally:
        try:
            client.close(session)
        except Exception:  # the session goes when the API drops idle ones anyway
            pass
    return turns


def main():
    parser = argparse.ArgumentParser(description="The cook check: Puls alone against Puls with its cook.")
    parser.add_argument("--repeat", type=int, default=1)
    args = parser.parse_args()
    run = os.environ.get("BENCHMARK_RUN") or time.strftime("local-%Y%m%d-%H%M%S")
    client = Client()

    protocol.start()
    protocol.say(f"Cook check, run {run}, {args.repeat} repeat(s), {len(CONVERSATIONS)} conversations per mode.")
    for repeat in range(1, args.repeat + 1):
        for mode, toolsets in MODES.items():
            cook_on = "specialists" in toolsets
            protocol.emit("mode", mode=mode, repeat=repeat, toolsets=toolsets, personality=PERSONALITY, llm=None,
                          conversations=len(CONVERSATIONS))
            for task, location, messages in CONVERSATIONS:
                started = time.time()
                try:
                    turns = converse(client, run, mode, repeat, toolsets, task, location, messages)
                except Exception as ex:  # the session couldn't even open: every turn of it failed
                    turns = [{**stream.read_turn([]), "error": repr(ex), "seconds": 0} for _ in messages]
                scores = score(task, turns, cook_on)
                protocol.emit("conversation", mode=mode, repeat=repeat, task=task, role=scores["role"], scores=scores,
                              errors=scores["errors"], seconds=round(time.time() - started, 1))
                protocol.say(f"{mode:9} {task:12} repeat={repeat} role={scores['role']} routed={scores['routed_right']} "
                             f"priced={scores['priced']}/{scores['ingredients']} follow_up={scores['follow_up']}")
            protocol.emit("mode_done", mode=mode, repeat=repeat)
    protocol.say("Cook check done.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
