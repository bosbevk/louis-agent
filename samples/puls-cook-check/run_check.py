"""The cook check (F25, F26): does Puls hand a meal question to the cook, does the cook price it from South African
shops' own pages, and does a follow-up go back to the same cook? Each conversation runs in one Puls session (the
follow-up is the second message), in two modes: Puls as built (`agents` only) and Puls with `web` itself (Louis: does
it still delegate the meal, and what does it save on the control question?). The usage ledger (logs/puls) is read back
for each run's cost, per conversation and per turn.

What is scored is what the agents called, not only what they wrote: the role Puls passed to StartSubSession, the hosts
the specialist fetched (from the sub_session events the API streams), rand amounts and "unknown" in the answer, and
whether the follow-up was a ContinueSubSession with the first answer's handle.

Spends money (Claude). Run from the repo root with the Puls containers up (rebuilt after a core change):
    docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
    python samples/puls-cook-check/run_check.py samples/sample-output-puls-cook
Several runs (each in run-N/, with summary.json and summary.md over them):
    python samples/puls-cook-check/run_check.py samples/sample-output-puls-cook --repeat 3
Scores and totals again from a saved run, without model calls:
    python samples/puls-cook-check/run_check.py --report samples/sample-output-puls-cook
"""
import json
import os
import re
import statistics
import sys
import time
from datetime import datetime, timezone
from urllib.parse import urlparse

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "puls-check"))
import run_check as delegation_check  # noqa: E402  (the delegation check's API calls and ledger reading)

call, ledger, total = delegation_check.call, delegation_check.ledger, delegation_check.total

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
    # The control: a plain web question, which must still go to the researcher.
    ("control-web", "Stellenbosch, Wes-Kaap, Suid-Afrika", [
        "Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.",
    ]),
]

MODES = {
    "delegated": ["agents"],        # Puls as built
    "puls-web": ["agents", "web"],  # Puls with the web itself
}

SHOPS = ("checkers.co.za", "pnp.co.za", "woolworths.co.za", "spar.co.za", "shoprite.co.za")
RAND = re.compile(r"\bR\s?\d[\d\s,.]*")
# A price the cook didn't confirm: the role says "unknown"; the first run showed it writes "geskat" / "skatting" too.
# The cook's own tally, a fixed line at the end of its shopping list (the role asks for it): "Geprys: 7 van 9 bestanddele".
PRICED = re.compile(r"\b(?:Geprys|Priced)\s*:\s*(\d+)\s+(?:van|of)\s+(\d+)", re.IGNORECASE)
ESTIMATE = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b", re.IGNORECASE)
TABLE_ROW = re.compile(r"^\|(?!\s*-)(?!\s*(?:Item|Bestanddeel|Ingredient|Produk)\b)[^|]+\|", re.IGNORECASE | re.MULTILINE)
UNCONFIRMED = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b", re.IGNORECASE)


progress = delegation_check.progress   # the same '@' + JSON progress lines, so the portal draws both checks the same way


def converse(toolsets, run, task, location, messages, mode=""):
    """One conversation in a new session: for each message, the answer, Puls's tool calls, its delegations (start or
    continue, with toolsets, role and handle), the specialist's own tool calls, the time, and an error if any."""
    body = {"toolsets": toolsets, "personality": "puls", "time_zone": "Africa/Johannesburg", "task": task, "run": run}
    if location:
        body["location"] = location
    session = call("POST", "sessions", body)["session_id"]
    turns = []
    try:
        for n, message in enumerate(messages, 1):
            started = time.time()
            progress("question", mode=mode, task=task, turn=n, text=message)
            turn = {"question": message, **send(session, message, mode, task, n)}
            turn["seconds"] = round(time.time() - started, 1)
            turns.append(turn)
            starts = [d for d in turn["delegations"] if d["call"] == "start"]
            continues = [d for d in turn["delegations"] if d["call"] == "continue"]
            progress("turn", mode=mode, task=task, turn=n, question=message,
                     role=starts[0]["personality"] if starts else None,
                     continued=continues[0]["handle"] if continues else None,
                     puls_calls=len(turn["tools"]), specialist_calls=len(turn["specialist_calls"]),
                     hosts=hosts(turn["specialist_calls"]), seconds=turn["seconds"], error=turn["error"])
    finally:
        call("DELETE", f"sessions/{session}")
    return turns


def send(session, message, mode="", task="", turn=0):
    text, tools, delegations, specialist_calls, error = [], [], [], [], None
    thinking, specialist_thinking = [], []   # what Puls and its specialist thought before answering (Louis: "I need to see the thinking")
    with call("POST", f"sessions/{session}/messages", {"message": message}, stream=True) as reply:
        event = None
        for raw in reply:
            line = raw.decode("utf-8").rstrip("\n")
            if line.startswith("event: "):
                event = line[7:]
                continue
            if not line.startswith("data: ") or not event:
                continue
            data = json.loads(line[6:])
            # Every stream event, forwarded as it arrives, so the portal can show the conversation unfolding the way the
            # chat does (Louis: "show me as if I'm typing it").
            progress("sse", mode=mode, task=task, turn=turn, type=event, data=data)
            if event == "content_block_start" and data.get("content_block", {}).get("type") == "tool_use":
                block = data["content_block"]
                tools.append(block["name"])
                arguments = block.get("input") or {}
                if block["name"] == "StartSubSession":
                    delegations.append({"call": "start", "toolsets": arguments.get("toolsets"), "personality": arguments.get("personality")})
                elif block["name"] == "ContinueSubSession":
                    delegations.append({"call": "continue", "handle": arguments.get("handle")})
            elif event == "content_block_delta" and (data.get("delta") or {}).get("type") == "text_delta":
                text.append(data["delta"]["text"])
            elif event == "content_block_delta" and (data.get("delta") or {}).get("type") == "thinking_delta":
                thinking.append(data["delta"].get("thinking", ""))
            elif event == "sub_session":
                inner = data.get("data") or {}
                if data.get("event") == "content_block_start" and inner.get("content_block", {}).get("type") == "tool_use":
                    block = inner["content_block"]
                    specialist_calls.append({"name": block["name"], "input": block.get("input") or {}})
                elif data.get("event") == "content_block_delta" and (inner.get("delta") or {}).get("type") == "thinking_delta":
                    specialist_thinking.append(inner["delta"].get("thinking", ""))
            elif event == "error":
                error = json.dumps(data)
    answer = "".join(text).strip()
    # The handle a kept sub-session got, from the footer Puls's tool result carries into its answer only if it quotes
    # it; the reliable source is the next turn's ContinueSubSession call, scored in `score`.
    return {"answer": answer, "tools": tools, "delegations": delegations, "specialist_calls": specialist_calls, "error": error,
            "thinking": "".join(thinking).strip(), "specialist_thinking": "".join(specialist_thinking).strip()}


def hosts(specialist_calls):
    """The hosts the specialist read pages from (FetchUrl), in order, without repeats."""
    seen = []
    for c in specialist_calls:
        if c["name"] in ("FetchUrl", "ReadRenderedPage") and (url := c["input"].get("url")):
            host = (urlparse(url).hostname or "").lower().removeprefix("www.")
            if host and host not in seen:
                seen.append(host)
    return seen


def priced(answer):
    """The cook's rating (Louis): ingredients with a price over ingredients on the list. From the cook's own tally line
    when it wrote one; else counted from the list's table rows, a row with a rand amount and no estimate mark being
    priced. (None, None) when there is no list at all."""
    if m := PRICED.search(answer or ""):
        n, total = int(m.group(1)), int(m.group(2))
        return (min(n, total), total) if total else (None, None)
    rows = [r for r in re.findall(r"^\|(?!\s*-)[^\n]*$", answer or "", re.MULTILINE) if not re.match(r"^\|\s*(Item|Bestanddeel|Ingredient|Produk)\b", r, re.IGNORECASE)]
    if not rows:
        return None, None
    # A price from the search index counts (Louis's option 1); an estimate or an unknown doesn't.
    good = [r for r in rows if RAND.search(r) and not ESTIMATE.search(r) and "±" not in r]
    return len(good), len(rows)


def score(task, turns):
    """What the run says about one conversation, as numbers: the role of each turn, the hosts, rand, unknown, follow-up,
    and the cook's rating: priced ingredients over ingredients."""
    first, follow = turns[0], (turns[1] if len(turns) > 1 else None)
    n_priced, n_items = priced(first["answer"])
    starts = [d for d in first["delegations"] if d["call"] == "start"]
    fetched = hosts(first["specialist_calls"] + (follow["specialist_calls"] if follow else []))
    local = [h for h in fetched if h.endswith(".za")]        # South African sites: a recipe site counts, a US one doesn't
    shops = [h for h in fetched if h.endswith(SHOPS)]        # where the prices must come from
    expected_role = "puls-cook" if task.startswith("cook") else "puls-researcher"
    result = {
        "role": starts[0]["personality"] if starts else None,
        "routed_right": bool(starts) and starts[0]["personality"] == expected_role,
        "answered_itself": not starts and not first["error"],
        "hosts": fetched, "local_hosts": local, "shop_hosts": shops,
        "sources_local": bool(fetched) and len(local) == len(fetched),
        "priced_at_shops": bool(shops),
        "ingredients": n_items, "priced": n_priced,
        "priced_ratio": round(n_priced / n_items, 2) if n_items else None,
        "rand_amounts": len(RAND.findall(first["answer"])) + (len(RAND.findall(follow["answer"])) if follow else 0),
        "unconfirmed_marks": len(UNCONFIRMED.findall(first["answer"])) + (len(UNCONFIRMED.findall(follow["answer"])) if follow else 0),
        "errors": [t["error"] for t in turns if t["error"]],
    }
    if follow:
        continues = [d for d in follow["delegations"] if d["call"] == "continue"]
        result["follow_up"] = "continued" if continues else ("new sub-session" if any(d["call"] == "start" for d in follow["delegations"]) else "answered itself")
        result["follow_up_handle"] = continues[0]["handle"] if continues else None
    return result


def summarise(records, tasks):
    """A run's totals from the ledger: all, Puls's own, its specialists', per conversation and per turn of it."""
    records = [r for r in records if r.get("purpose") != "sub_session"]   # the roll-up repeats the child's own rows
    own = [r for r in records if not r.get("parent")]
    sub = [r for r in records if r.get("parent")]
    session_of = {r["session"]: r.get("task") for r in own}

    def task_of(r):
        return r.get("task") if not r.get("parent") else session_of.get(r["parent"].split("#")[0])

    def turn_of(r):
        return r.get("turn") if not r.get("parent") else int(r["parent"].split("#")[1])

    per_task = {}
    for key in tasks:
        mine = [r for r in records if task_of(r) == key]
        per_task[key] = {"all": total(mine), "turns": {str(t): total([r for r in mine if turn_of(r) == t]) for t in sorted({turn_of(r) for r in mine if turn_of(r)})}}
    return {"all": total(records), "own": total(own), "specialists": total(sub), "per_task": per_task}


def run_once(out):
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    results = {}
    for mode, toolsets in MODES.items():
        run = f"puls-cook-{mode}-{stamp}"
        os.makedirs(os.path.join(out, mode), exist_ok=True)
        conversations = []
        delegation_check.events_file(out, mode)
        progress("mode", mode=mode, toolsets=toolsets, conversations=len(CONVERSATIONS))
        for key, location, messages in CONVERSATIONS:
            try:
                turns = converse(toolsets, run, key, location, messages, mode)
            except Exception as ex:  # a dead container, a timeout: the run goes on and the README says so
                turns = [{"question": m, "answer": "", "tools": [], "delegations": [], "specialist_calls": [], "error": repr(ex), "seconds": 0} for m in messages]
            scored = score(key, turns)
            conversations.append({"task": key, "location": location, "turns": turns, "score": scored})
            print(f"{mode:9} {key:12} role={scored['role']} routed={scored['routed_right']} hosts={scored['hosts']} "
                  f"follow_up={scored.get('follow_up')} seconds={[t['seconds'] for t in turns]}", flush=True)
            progress("conversation", mode=mode, task=key, role=scored["role"], routed_right=scored["routed_right"],
                     hosts=scored["hosts"], shop_hosts=scored["shop_hosts"], sources_local=scored["sources_local"],
                     follow_up=scored.get("follow_up"), rand_amounts=scored["rand_amounts"],
                     ingredients=scored["ingredients"], priced=scored["priced"], priced_ratio=scored["priced_ratio"],
                     unconfirmed_marks=scored["unconfirmed_marks"], errors=len(scored["errors"]),
                     seconds=round(sum(t["seconds"] for t in turns), 1))
        time.sleep(2)   # the ledger's last lines
        records = ledger(run)
        results[mode] = {"run": run, "toolsets": toolsets, "conversations": conversations,
                         **summarise(records, [k for k, _, _ in CONVERSATIONS])}
        totals = results[mode]["all"]
        progress("mode_done", mode=mode, cost=totals["cost"], tokens=totals["tokens"], requests=totals["requests"],
                 per_task={k: v["all"]["cost"] for k, v in results[mode]["per_task"].items()})
        delegation_check.EVENTS.close()
        with open(os.path.join(out, mode, "answers.json"), "w", encoding="utf-8") as file:
            json.dump(conversations, file, ensure_ascii=False, indent=2)
        with open(os.path.join(out, mode, "usage.jsonl"), "w", encoding="utf-8") as file:
            for r in records:
                file.write(json.dumps(r, ensure_ascii=False) + "\n")
        transcript(os.path.join(out, mode, "transcript.md"), conversations)
    write_results(out, results)
    return results


def transcript(path, conversations):
    with open(path, "w", encoding="utf-8") as file:
        for c in conversations:
            file.write(f"# {c['task']}" + (f" ({c['location']})" if c["location"] else "") + "\n\n")
            for n, t in enumerate(c["turns"], 1):
                calls = ", ".join(f"{s['name']}({s['input'].get('url') or s['input'].get('query') or ''})" for s in t["specialist_calls"])
                file.write(f"## Turn {n}: {t['question']}\n\n*Puls:* {', '.join(t['tools']) or '—'} · "
                           f"*Delegations:* {json.dumps(t['delegations'], ensure_ascii=False) if t['delegations'] else '—'} · *{t['seconds']} s*\n\n"
                           f"*Specialist:* {calls or '—'}\n\n")
                # The thinking, as blockquotes under a label: the portal renders markdown without HTML, so no <details>.
                for label, thought in (("Puls se denke", t.get("thinking")), ("Spesialis se denke", t.get("specialist_thinking"))):
                    if thought:
                        file.write(f"*{label}:*\n\n" + "\n".join("> " + line for line in thought.splitlines()) + "\n\n")
                file.write(f"{t['answer'] or t['error']}\n\n")


def write_results(out, results):
    with open(os.path.join(out, "results.json"), "w", encoding="utf-8") as file:
        json.dump(results, file, ensure_ascii=False, indent=2)
    lines = ["# The cook check: one run", ""]
    for mode, r in results.items():
        lines += [f"## {mode} (toolsets: {', '.join(r['toolsets'])}): ${r['all']['cost']:.4f}, {r['all']['tokens']:,} tokens, {r['all']['requests']} requests", "",
                  "| Conversation | Role | Routed right | Hosts fetched (SA shops / SA / all) | Priced ingredients | Rand amounts | Unconfirmed marks | Follow-up | Cost turn 1 | Cost turn 2 | Seconds |",
                  "|---|---|---|---|---:|---:|---:|---|---:|---:|---|"]
        for c in r["conversations"]:
            s, turns = c["score"], r["per_task"][c["task"]]["turns"]
            rating = f"{s['priced']} / {s['ingredients']} ({s['priced_ratio']:.0%})" if s.get("ingredients") else "—"
            lines.append(f"| {c['task']} | {s['role'] or ('— (answered itself)' if s['answered_itself'] else '— (error)')} | {'yes' if s['routed_right'] else 'no'} | "
                         f"{len(s['shop_hosts'])} / {len(s['local_hosts'])} / {len(s['hosts'])}: {', '.join(s['hosts']) or '—'} | {rating} | {s['rand_amounts']} | {s['unconfirmed_marks']} | "
                         f"{s.get('follow_up', '—')}{' (' + s['follow_up_handle'] + ')' if s.get('follow_up_handle') else ''} | "
                         f"${turns.get('1', {}).get('cost', 0):.4f} | {('$%.4f' % turns['2']['cost']) if '2' in turns else '—'} | {', '.join(str(t['seconds']) for t in c['turns'])} |")
        lines.append("")
    with open(os.path.join(out, "results.md"), "w", encoding="utf-8") as file:
        file.write("\n".join(lines))
    print("\n".join(lines), flush=True)


def main(out, repeat):
    if repeat == 1:
        run_once(out)
        return
    runs = []
    for n in range(1, repeat + 1):
        print(f"--- run {n} of {repeat}", flush=True)
        runs.append(run_once(os.path.join(out, f"run-{n}")))
    summarise_runs(out, runs)


def summarise_runs(out, runs):
    """summary.json and summary.md over the runs: how often each conversation routed right, fetched only local shops
    and continued its specialist, and the cost's spread."""
    def spread(values):
        return {"mean": round(statistics.mean(values), 6), "min": round(min(values), 6), "max": round(max(values), 6)}

    summary = {}
    for mode in runs[0]:
        per_task = {}
        for key, _, _ in CONVERSATIONS:
            scores = [next(c["score"] for c in r[mode]["conversations"] if c["task"] == key) for r in runs]
            ratios = [s["priced_ratio"] for s in scores if s.get("priced_ratio") is not None]
            per_task[key] = {
                "routed_right": sum(s["routed_right"] for s in scores),
                "sources_local": sum(s["sources_local"] for s in scores),
                "priced_at_shops": sum(s["priced_at_shops"] for s in scores),
                "priced_ratio": round(statistics.mean(ratios), 2) if ratios else None,
                "continued": sum(s.get("follow_up") == "continued" for s in scores),
                "errors": sum(bool(s["errors"]) for s in scores),
                "cost": spread([r[mode]["per_task"][key]["all"]["cost"] for r in runs]),
            }
        summary[mode] = {"cost": spread([r[mode]["all"]["cost"] for r in runs]), "per_task": per_task}
    with open(os.path.join(out, "summary.json"), "w", encoding="utf-8") as file:
        json.dump(summary, file, indent=2)
    n = len(runs)
    lines = [f"# {n} runs per mode", "", "| Mode | Cost, mean (lowest–highest) |", "|---|---:|"]
    lines += [f"| {mode} | ${s['cost']['mean']:.4f} ({s['cost']['min']:.4f}–{s['cost']['max']:.4f}) |" for mode, s in summary.items()]
    lines += ["", f"| Conversation | Mode | Routed right | Only SA sources | Priced at a shop | Priced ingredients, mean | Follow-up continued | Errors | Cost, mean (lowest–highest) |", "|---|---|---:|---:|---:|---:|---:|---:|---:|"]
    for key, _, messages in CONVERSATIONS:
        for mode, s in summary.items():
            t = s["per_task"][key]
            rating = f"{t['priced_ratio']:.0%}" if t.get("priced_ratio") is not None else "—"
            lines.append(f"| {key} | {mode} | {t['routed_right']}/{n} | {t['sources_local']}/{n} | {t['priced_at_shops']}/{n} | {rating} | {(str(t['continued']) + '/' + str(n)) if len(messages) > 1 else '—'} | "
                         f"{t['errors']}/{n} | ${t['cost']['mean']:.4f} ({t['cost']['min']:.4f}–{t['cost']['max']:.4f}) |")
    with open(os.path.join(out, "summary.md"), "w", encoding="utf-8") as file:
        file.write("\n".join(lines) + "\n")
    print("\n".join(lines), flush=True)


def report(out):
    """Scores and totals again from a saved run (or its run-N folders), without model calls."""
    if os.path.exists(os.path.join(out, "results.json")):
        write_results(out, rescore(out))
        return
    runs = []
    for n in range(1, 100):
        folder = os.path.join(out, f"run-{n}")
        if not os.path.exists(os.path.join(folder, "results.json")):
            break
        runs.append(rescore(folder))
        write_results(folder, runs[-1])
    summarise_runs(out, runs)


def rescore(out):
    with open(os.path.join(out, "results.json"), encoding="utf-8") as file:
        results = json.load(file)
    for mode in results:
        with open(os.path.join(out, mode, "usage.jsonl"), encoding="utf-8") as file:
            records = [json.loads(line) for line in file]
        for c in results[mode]["conversations"]:
            c["score"] = score(c["task"], c["turns"])
        results[mode].update(summarise(records, [k for k, _, _ in CONVERSATIONS]))
    return results


if __name__ == "__main__":
    arguments = sys.argv[1:]
    if arguments and arguments[0] == "--report":
        report(arguments[1])
    else:
        repeat = 1
        if "--repeat" in arguments:
            i = arguments.index("--repeat")
            repeat = int(arguments[i + 1])
            del arguments[i:i + 2]
        main(arguments[0] if arguments else "samples/sample-output-puls-cook", repeat)
