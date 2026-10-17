"""The routing check (F5, F21, F29; rewritten for the portal in F31): does Puls send each kind of question to the right
place? Twelve questions, two of each kind, each in a session of its own, in two modes: `baseline`, one agent with
every toolset this instance has but the delegating ones (it can't hand anything on), and `delegated`, Puls as built
(`specialists` and `web`). Only the cook is on (F29), so a meal must go to `puls-cook` and every other question is
right when Puls answers it itself, the web with its own `web`, the rest saying what it can't do yet.

What is scored is the call made, not the words: the role Puls passed to StartSubSession, how many delegations, the
hosts read, errors. The cost of each mode is the API's, from the usage ledger.

Spends money (Claude). The admin portal runs it (Maatstawwe); by hand, with the Puls containers up:
    BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/routing/run_check.py --repeat 1
"""
import argparse
import os
import sys
import time

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

from benchmarks.puls_check import protocol, stream  # noqa: E402
from benchmarks.puls_check.client import Client  # noqa: E402

ROLE = "puls/voice-af"
TIME_ZONE = "Africa/Johannesburg"
LOCATION = "Stellenbosch, Wes-Kaap, Suid-Afrika"

DELEGATED = ["specialists", "web"]   # Puls as built (F29)
DELEGATING = ("specialists", "minions")

# Two of each kind, so one odd answer doesn't decide a kind.
QUESTIONS = [
    ("web", "Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne."),
    ("files", "Lees README.md in die werkruimte en som in drie punte op wat die projek doen."),
    ("git", "Wat het die laaste drie commits in hierdie repository verander?"),
    ("numbers", "Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?"),
    ("chat", "Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog."),
    ("cook", "Ek is in Stellenbosch. Wat kan ek vanaand vir vier mense kook? Gee die resep en 'n inkopielys met pryse."),
    ("web-2", "Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne."),
    ("files-2", "Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking."),
    ("git-2", "Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?"),
    ("numbers-2", "Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?"),
    ("chat-2", "Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans."),
    ("cook-2", "What should I cook tonight in Pretoria for two? I have about an hour. Give me the recipe and what it'll cost at the shops."),
]

# The role each kind goes to when Puls delegates; None where Puls answers itself (F29: only the cook is on).
KINDS = {"web": None, "files": None, "git": None, "numbers": None, "chat": None, "cook": "puls-cook"}


def expected_role(task):
    return KINDS[task.split("-")[0]]


def baseline(available):
    """One agent with every toolset this instance has, but none it could delegate with."""
    return [t for t in available if t not in DELEGATING]


def score(task, turn, delegating):
    """One question as numbers. Without delegation (the baseline) answering itself is all it can do, so it's right
    unless it failed; with it, the role must be the kind's."""
    expected = expected_role(task) if delegating else None
    return {
        "role": turn["role"],
        "expected_role": expected,
        "routed_right": not turn["error"] and turn["role"] == expected,
        "answered_itself": turn["role"] is None and not turn["error"],
        "delegations": sum(1 for c in turn["calls"] if c["name"] in stream.DELEGATIONS),
        "puls_calls": len(turn["calls"]),
        "specialist_calls": len(turn["specialist_calls"]),
        "hosts": stream.hosts(turn["calls"] + turn["specialist_calls"]),
        "errors": 1 if turn["error"] else 0,
    }


def ask(client, run, mode, repeat, toolsets, task, question):
    """One question in a new session, its stream forwarded as it comes; what the turn read."""
    key = {"mode": mode, "repeat": repeat, "task": task, "turn": 1}
    session = client.open_session(toolsets=toolsets, role=ROLE, time_zone=TIME_ZONE, location=LOCATION,
                                  task=task, run=protocol.run_tag(run, mode, repeat))
    started, events = time.time(), []
    try:
        protocol.emit("question", **key, session=session, text=question)
        try:
            for event, data in client.message(session, question):
                protocol.emit("sse", **key, type=event, data=data)
                events.append((event, data))
            read = stream.read_turn(events)
        except Exception as ex:  # a dead container, a timeout: this question failed, the run goes on
            read = {**stream.read_turn(events), "error": repr(ex)}
        read["seconds"] = round(time.time() - started, 1)
        protocol.emit("turn", **key, session=session, question=question, answer=read["answer"], role=read["role"],
                      continued=read["continued"], puls_calls=len(read["calls"]), specialist_calls=len(read["specialist_calls"]),
                      hosts=stream.hosts(read["calls"] + read["specialist_calls"]), seconds=read["seconds"], error=read["error"])
        return read
    finally:
        try:
            client.close(session)
        except Exception:
            pass


def main():
    parser = argparse.ArgumentParser(description="The routing check: one agent with every toolset against Puls as built.")
    parser.add_argument("--repeat", type=int, default=1)
    args = parser.parse_args()
    run = os.environ.get("BENCHMARK_RUN") or time.strftime("local-%Y%m%d-%H%M%S")
    client = Client()
    modes = {"baseline": baseline(client.toolsets()), "delegated": DELEGATED}

    protocol.start()
    protocol.say(f"Routing check, run {run}, {args.repeat} repeat(s), {len(QUESTIONS)} questions per mode; "
                 f"baseline: {', '.join(modes['baseline'])}.")
    for repeat in range(1, args.repeat + 1):
        for mode, toolsets in modes.items():
            delegating = "specialists" in toolsets
            protocol.emit("mode", mode=mode, repeat=repeat, toolsets=toolsets, role=ROLE, llm=None,
                          conversations=len(QUESTIONS))
            right = 0
            for task, question in QUESTIONS:
                started = time.time()
                try:
                    read = ask(client, run, mode, repeat, toolsets, task, question)
                except Exception as ex:  # the session couldn't even open
                    read = {**stream.read_turn([]), "error": repr(ex)}
                scores = score(task, read, delegating)
                right += scores["routed_right"]
                protocol.emit("conversation", mode=mode, repeat=repeat, task=task, role=scores["role"], scores=scores,
                              errors=scores["errors"], seconds=round(time.time() - started, 1))
                protocol.say(f"{mode:9} {task:10} repeat={repeat} role={scores['role']} expected={scores['expected_role']} "
                             f"right={scores['routed_right']}")
            protocol.emit("mode_done", mode=mode, repeat=repeat)
            protocol.say(f"{mode:9} repeat={repeat}: {right} of {len(QUESTIONS)} routed right")
    protocol.say("Routing check done.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
