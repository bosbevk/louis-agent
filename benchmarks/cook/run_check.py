"""The cook check (F25, F26; rewritten for the portal in F31): what does Puls's cook add? The same conversations with
Puls alone with every tool the cook has (`puls-tools`: `web` and `shops`, so it can't delegate and cooks and prices
itself; F34, until then `puls-only` with `web` alone) and Puls with its cook (`puls-cook`: `specialists` and `web`, Puls
as built). Each conversation is one session; a second message is the follow-up.

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

ROLE = "puls/voice-af"
TIME_ZONE = "Africa/Johannesburg"

MODES = {
    "puls-tools": ["web", "shops"],       # Puls alone with the cook's tools: it can't delegate, so it cooks and prices itself
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
    # F34: Louis's town, a budget meal (the store that serves Malmesbury, the N7 Checkers found by name, its deals).
    ("cook-malmesbury", "Malmesbury, Wes-Kaap, Suid-Afrika", [
        "Ek het R150 vir aandete vir vier mense. Wat kan ek maak, en wat kos dit by die winkel? Gebruik spesiale as daar is.",
    ]),
    # F34: a town with no Checkers in it or its municipality, and none Checkers serves it from (it answers with its
    # fallback stores): no shop the cook has a tool for, so the list comes without prices. Not Darling: it's in
    # Malmesbury's municipality, and Checkers serves it (run dd992620f6cc priced it).
    ("cook-no-shop", "Prince Albert, Wes-Kaap, Suid-Afrika", [
        "Wat kan ek vanaand vir twee kook? Gee die resep en wat dit by die winkel kos.",
    ]),
    # The control: a plain web question, which Puls answers itself (F29: only the cook is on).
    ("control-web", "Stellenbosch, Wes-Kaap, Suid-Afrika", [
        "Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.",
    ]),
]

# The shops' sites, one list with the scorer's (a price links to its own shop).
SHOPS = tuple(sorted(set(scoring.SHOP_HOSTS.values())))

# The conversations whose town has no shop the cook has a tool for (Prince Albert: no Checkers, and none serves it).
# There a `-` is the right answer, and a price came from a store that isn't the town's (Louis, run d94597a51d4d: "accuracy
# is 100%, no shop there mos?").
NO_SHOP = frozenset({"cook-no-shop"})


def score(task, turns, cook_on):
    """One conversation as numbers. A meal goes to the cook when the mode has it; otherwise, and for the control
    question, Puls answers itself. An errored first turn is never right."""
    first, follow = turns[0], (turns[1] if len(turns) > 1 else None)
    n_priced, n_items, n_guessed, n_per_kg = scoring.priced(first["answer"])
    no_shop_rows = wrong_store = 0
    if task in NO_SHOP and n_items:
        wrong_store, n_priced = n_priced, 0
        no_shop_rows = n_items - wrong_store - n_guessed - n_per_kg
    fetched = stream.hosts([c for t in turns for c in t["calls"] + t["specialist_calls"]])
    # A branch looked up on a map (OpenStreetMap) is no source of the answer (F34).
    sources = [h for h in fetched if not h.endswith(scoring.MAP_HOSTS)]
    local = [h for h in sources if h.endswith(".za")]
    shops = [h for h in fetched if h.endswith(SHOPS)]
    expected = "puls-cook" if task.startswith("cook") and cook_on else None
    # F34: the cook reads shops through its tools, never their pages.
    shop_calls = [c for t in turns for c in t["calls"] + t["specialist_calls"] if c["name"] in scoring.SHOP_TOOLS]
    results = [r for t in turns for r in t.get("results", []) + t.get("specialist_results", [])]
    answers = [t["answer"] for t in turns]
    result = {
        "role": first["role"],
        "routed_right": not first["error"] and first["role"] == expected,
        "answered_itself": first["role"] is None and not first["error"],
        "hosts": fetched, "local_hosts": local, "shop_hosts": shops,
        "sources_local": bool(sources) and len(local) == len(sources),
        "priced_at_shops": bool(shops) or bool(shop_calls),
        "shop_tool_calls": len(shop_calls),
        "deal_tool_calls": sum(1 for c in shop_calls if c["name"] in scoring.DEAL_TOOLS),
        "deals_named": scoring.deals_named(first["answer"]),
        "untooled_shops": scoring.untooled_shops(first["answer"]),
        "no_catalogue_line": scoring.no_catalogue_line(first["answer"]),
        "store_for_area": scoring.store_for_area(results),
        "store_default": scoring.store_default(results),
        "ingredients": n_items, "priced": n_priced, "unsourced": n_guessed, "per_kg": n_per_kg,
        "no_shop_rows": no_shop_rows, "wrong_store": wrong_store,
        # Right rows over the list's: priced with their source, or rightly without a price where there's no shop.
        "priced_ratio": round((n_priced + no_shop_rows) / n_items, 2) if n_items else None,
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
    session = client.open_session(toolsets=toolsets, role=ROLE, time_zone=TIME_ZONE, location=location,
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
    parser = argparse.ArgumentParser(description="The cook check: Puls with the cook's tools against Puls with its cook.")
    parser.add_argument("--repeat", type=int, default=1)
    args = parser.parse_args()
    run = os.environ.get("BENCHMARK_RUN") or time.strftime("local-%Y%m%d-%H%M%S")
    client = Client()

    protocol.start()
    protocol.say(f"Cook check, run {run}, {args.repeat} repeat(s), {len(CONVERSATIONS)} conversations per mode.")
    for repeat in range(1, args.repeat + 1):
        for mode, toolsets in MODES.items():
            cook_on = "specialists" in toolsets
            protocol.emit("mode", mode=mode, repeat=repeat, toolsets=toolsets, role=ROLE, llm=None,
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
                             f"priced={scores['priced']}/{scores['ingredients']} guessed={scores['unsourced']} follow_up={scores['follow_up']}")
            protocol.emit("mode_done", mode=mode, repeat=repeat)
    protocol.say("Cook check done.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
