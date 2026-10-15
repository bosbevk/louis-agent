"""The Puls delegation check (F5, F21, F22, F29): the same questions asked twice through Puls's API, first by one agent
with every toolset (the baseline), then by Puls as built (`agents` and `web`: it answers the web itself and hands only
cooking to the cook, the one role that's on, F29), each question in a session of its own. The
usage ledger (logs/puls) is read back for each run's tokens by kind and cost, Puls's own and its specialists', per
question; each delegation's toolsets and role are recorded from the tool call.

Spends money (Claude). Run from the repo root with the Puls containers up:
    docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
    python samples/puls-check/run_check.py samples/sample-output-puls-check
Several runs per mode, since one run swings by 15-20% (each in run-N/, with summary.json and summary.md over them):
    python samples/puls-check/run_check.py samples/sample-output-puls-check-repeat --repeat 3
Totals again from a saved run, without model calls:
    python samples/puls-check/run_check.py --report samples/sample-output-puls-check
"""
import statistics
import json
import os
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime, timezone

API = os.environ.get("PULS_API", "http://127.0.0.1:5070/")
LEDGER = os.path.join("logs", "puls", f"usage-{datetime.now(timezone.utc):%Y-%m}.jsonl")

# Each kind of task a Puls specialist has a role for, and one Puls should answer itself.
QUESTIONS = [
    ("web", "Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne."),
    ("files", "Lees README.md in die werkruimte en som in drie punte op wat die projek doen."),
    ("git", "Wat het die laaste drie commits in hierdie repository verander?"),
    ("numbers", "Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?"),
    ("chat", "Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog."),
    # A second of each kind (Louis: more samples), so one odd answer doesn't decide a kind.
    ("web-2", "Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne."),
    ("files-2", "Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking."),
    ("git-2", "Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?"),
    ("numbers-2", "Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?"),
    ("chat-2", "Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans."),
    # The cook (F25; Louis: "add the cook delegation to it", two like every kind): a meal must go to puls-cook, not the
    # researcher, in Afrikaans and in English.
    ("cook", "Ek is in Stellenbosch. Wat kan ek vanaand vir vier mense kook? Gee die resep en 'n inkopielys met pryse."),
    ("cook-2", "What should I cook tonight in Pretoria for two? I have about an hour. Give me the recipe and what it'll cost at the shops."),
]

# The role each kind should go to when Puls delegates; None where Puls should answer itself.
# F29: only the cook is on. The right outcome for every other question is no delegation: Puls answers the web itself and
# says a files, git or numbers request isn't available yet. (Before F29: researcher, reader, historian, analyst.)
EXPECTED_ROLE = {
    "web": None, "files": None, "git": None, "numbers": None,
    "chat": None, "cook": "puls-cook",
}

TOKEN_KINDS = ["input", "cache_write", "cache_read", "output"]


EVENTS = None   # a mode's events.jsonl while it runs: the questions and every stream event, so a saved run can be replayed as typed


def progress(event, **fields):
    """A structured progress line for whoever watches the run (the admin portal renders them as a tree; a terminal
    reads them as they are): '@' then one JSON object. The plain lines stay for people. The conversation's own events
    (a question, each stream event) also go to the mode's events.jsonl, with the time, for the portal's replay."""
    line = json.dumps({"event": event, "t": round(time.time(), 3), **fields}, ensure_ascii=False)
    print("@" + line, flush=True)
    if EVENTS is not None and event in ("question", "sse"):
        EVENTS.write(line + "\n")


def events_file(out, mode):
    """Opens the mode's events.jsonl for progress() to write to; the caller closes it when the mode is done."""
    global EVENTS
    if EVENTS is not None:
        EVENTS.close()
    os.makedirs(os.path.join(out, mode), exist_ok=True)
    EVENTS = open(os.path.join(out, mode, "events.jsonl"), "w", encoding="utf-8")
    return EVENTS


def expected_role(task):
    return EXPECTED_ROLE.get(task.split("-")[0])


def hosts(specialist_calls):
    """The hosts the specialist read pages from, in order, without repeats."""
    seen = []
    for c in specialist_calls:
        if c["name"] in ("FetchUrl", "ReadRenderedPage") and (url := c["input"].get("url")):
            host = (urllib.parse.urlparse(url).hostname or "").lower().removeprefix("www.")
            if host and host not in seen:
                seen.append(host)
    return seen


def call(method, path, body=None, stream=False):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(API + path, data=data, method=method, headers={"Content-Type": "application/json"})
    response = urllib.request.urlopen(request, timeout=600)
    return response if stream else json.loads(response.read() or b"null")


def ask(toolsets, run, task, question, mode=""):
    """One question in a new session: the answer's text, the tools it called, its delegations (toolsets and role), the
    specialists that answered, and an error if any. Every stream event is forwarded as progress (the portal shows the
    conversation unfolding), and the specialist's own tool calls are kept for the hosts it read."""
    session = call("POST", "sessions", {"toolsets": toolsets, "personality": "puls", "time_zone": "Africa/Johannesburg",
                                        "task": task, "run": run, "location": "Stellenbosch, Wes-Kaap, Suid-Afrika"})["session_id"]
    text, tools, specialists, delegations, specialist_calls, error = [], [], [], [], [], None
    progress("question", mode=mode, task=task, turn=1, text=question)
    started = time.time()
    try:
        with call("POST", f"sessions/{session}/messages", {"message": question}, stream=True) as reply:
            event = None
            for raw in reply:
                line = raw.decode("utf-8").rstrip("\n")
                if line.startswith("event: "):
                    event = line[7:]
                    continue
                if not line.startswith("data: ") or not event:
                    continue
                data = json.loads(line[6:])
                progress("sse", mode=mode, task=task, turn=1, type=event, data=data)
                delta = data.get("delta") or {}
                if event == "content_block_start" and data.get("content_block", {}).get("type") == "tool_use":
                    block = data["content_block"]
                    tools.append(block["name"])
                    if block["name"] == "StartSubSession":
                        # The API sends a call's whole input here. The toolsets and the role Puls chose (none: the peer
                        # derives it from roles.json).
                        arguments = block.get("input") or {}
                        delegations.append({"toolsets": arguments.get("toolsets"), "personality": arguments.get("personality")})
                elif event == "content_block_delta" and delta.get("type") == "text_delta":
                    text.append(delta["text"])
                elif event == "sub_session":
                    inner = data.get("data") or {}
                    if data.get("event") == "message_start":
                        specialists.append(data.get("agent"))
                    elif data.get("event") == "content_block_start" and inner.get("content_block", {}).get("type") == "tool_use":
                        block = inner["content_block"]
                        specialist_calls.append({"name": block["name"], "input": block.get("input") or {}})
                elif event == "error":
                    error = json.dumps(data)
    finally:
        call("DELETE", f"sessions/{session}")
    seconds = round(time.time() - started, 1)
    role = delegations[0]["personality"] if delegations else None
    expected = expected_role(task)
    progress("turn", mode=mode, task=task, turn=1, question=question, role=role, continued=None, puls_calls=len(tools),
             specialist_calls=len(specialist_calls), hosts=hosts(specialist_calls), seconds=seconds, error=error)
    progress("conversation", mode=mode, task=task, role=role, routed_right=(role == expected) if mode == "delegated" else (role is None),
             hosts=hosts(specialist_calls), shop_hosts=[], sources_local=False, follow_up=None, rand_amounts=0, unconfirmed_marks=0,
             errors=1 if error else 0, seconds=seconds)
    return "".join(text).strip(), tools, specialists, delegations, error


def ledger(run):
    with open(LEDGER, encoding="utf-8") as file:
        return [record for record in map(json.loads, file) if record.get("run") == run]


def total(records):
    """Requests, tokens by kind (a cache read costs a tenth of input, a cache write more than input), all, and the cost."""
    kinds = {k: sum(r["tokens"].get(k) or 0 for r in records) for k in TOKEN_KINDS}
    cost = sum((r.get("cost") or {}).get("amount") or 0 for r in records)
    return {"requests": len(records), **kinds, "tokens": sum(kinds.values()), "cost": round(cost, 6)}


def summarise(records):
    """A run's totals: all, Puls's own, its specialists' (records with a parent), and per question.

    A sub_session row (F5) is Puls's roll-up of a specialist's reply: the same requests as the specialist's own rows,
    which are in this ledger too (Puls and its specialist share logs/puls). Totalled, every delegation would count
    twice, so it is skipped here as usage-probe --ledger skips it.
    """
    records = [r for r in records if r.get("purpose") != "sub_session"]
    own = [r for r in records if not r.get("parent")]
    sub = [r for r in records if r.get("parent")]
    # A specialist's records name their parent ("{session}#{turn}"); the parent session is the question's.
    task_of_session = {r["session"]: r.get("task") for r in own}
    task_of = lambda r: r.get("task") if not r.get("parent") else task_of_session.get(r["parent"].split("#")[0])
    return {"all": total(records), "own": total(own), "specialists": total(sub),
            "per_task": {key: total([r for r in records if task_of(r) == key]) for key, _ in QUESTIONS}}


def main(out, repeat=1):
    if repeat == 1:
        run_once(out)
        return
    runs = []
    for n in range(1, repeat + 1):
        print(f"--- run {n} of {repeat}", flush=True)
        runs.append(run_once(os.path.join(out, f"run-{n}")))
    summarise_runs(out, runs)


def summarise_runs(out, runs):
    """summary.json and summary.md: each mode's cost and tokens over the runs (mean, lowest, highest), and per question."""
    def spread(values):
        return {"mean": round(statistics.mean(values), 6), "min": round(min(values), 6), "max": round(max(values), 6)}

    summary = {}
    for mode in runs[0]:
        summary[mode] = {
            "cost": spread([r[mode]["all"]["cost"] for r in runs]),
            "tokens": spread([r[mode]["all"]["tokens"] for r in runs]),
            "per_task": {key: spread([r[mode]["per_task"][key]["cost"] for r in runs]) for key, _ in QUESTIONS},
        }
    with open(os.path.join(out, "summary.json"), "w", encoding="utf-8") as file:
        json.dump(summary, file, indent=2)
    modes = list(summary)
    lines = [f"# {len(runs)} runs per mode", "", "| | " + " | ".join(modes) + " |", "|---|" + "---:|" * len(modes),
             "| Cost, mean (lowest–highest) | " + " | ".join(
                 f"${s['cost']['mean']:.4f} ({s['cost']['min']:.4f}–{s['cost']['max']:.4f})" for s in summary.values()) + " |",
             "| Tokens, mean | " + " | ".join(f"{s['tokens']['mean']:,.0f}" for s in summary.values()) + " |", "",
             "| Question | " + " | ".join(f"{m}: mean (lowest–highest)" for m in modes) + " |", "|---|" + "---:|" * len(modes)]
    for key, _ in QUESTIONS:
        lines.append(f"| {key} | " + " | ".join(
            f"${s['per_task'][key]['mean']:.4f} ({s['per_task'][key]['min']:.4f}–{s['per_task'][key]['max']:.4f})"
            for s in summary.values()) + " |")
    with open(os.path.join(out, "summary.md"), "w", encoding="utf-8") as file:
        file.write("\n".join(lines) + "\n")
    print("\n".join(lines), flush=True)


def run_once(out):
    """One run of both modes into out: answers, transcripts, usage records, results.json and answers.md."""
    available = [t["name"] for t in call("GET", "toolsets")["toolsets"] if t["available"]]
    modes = {
        # The baseline: one agent with every toolset this instance has (not agents), no delegation.
        "baseline": [t for t in available if t != "agents"],
        # Puls as built (F29): agents and web; the web itself, cooking to the cook, the rest "not yet".
        "delegated": ["agents", "web"],
    }
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    results = {}
    for mode, toolsets in modes.items():
        run = f"puls-check-{mode}-{stamp}"
        os.makedirs(os.path.join(out, mode), exist_ok=True)
        answers = []
        events_file(out, mode)
        progress("mode", mode=mode, toolsets=toolsets, conversations=len(QUESTIONS))
        for key, question in QUESTIONS:
            started = time.time()
            text, tools, specialists, delegations, error = ask(toolsets, run, key, question, mode)
            answers.append({"task": key, "question": question, "answer": text, "tools": tools, "specialists": specialists,
                            "delegations": delegations, "error": error, "seconds": round(time.time() - started, 1)})
            print(f"{mode:9} {key:8} {answers[-1]['seconds']:6.1f}s  tools={tools}  delegations={delegations}", flush=True)
        time.sleep(2)   # the ledger's last lines
        records = ledger(run)
        results[mode] = {"run": run, "toolsets": toolsets, **summarise(records)}
        totals = results[mode]["all"]
        progress("mode_done", mode=mode, cost=totals["cost"], tokens=totals["tokens"], requests=totals["requests"],
                 per_task={k: v["cost"] for k, v in results[mode]["per_task"].items()})
        EVENTS.close()
        with open(os.path.join(out, mode, "answers.json"), "w", encoding="utf-8") as file:
            json.dump(answers, file, ensure_ascii=False, indent=2)
        with open(os.path.join(out, mode, "usage.jsonl"), "w", encoding="utf-8") as file:
            for r in records:
                file.write(json.dumps(r, ensure_ascii=False) + "\n")
        with open(os.path.join(out, mode, "transcript.md"), "w", encoding="utf-8") as file:
            for a in answers:
                file.write(f"## {a['task']}: {a['question']}\n\n*Tools:* {', '.join(a['tools']) or '—'} · "
                           f"*Delegations:* {json.dumps(a['delegations'], ensure_ascii=False) if a['delegations'] else '—'} · "
                           f"*{a['seconds']} s*\n\n{a['answer'] or a['error']}\n\n")
    with open(os.path.join(out, "results.json"), "w", encoding="utf-8") as file:
        json.dump(results, file, indent=2)
    side_by_side(out)
    print_results(results)
    return results


def side_by_side(out):
    """answers.md: each question's two answers, baseline and delegated, side by side, with room for corrections."""
    answers = {}
    for mode in ("baseline", "delegated"):
        path = os.path.join(out, mode, "answers.json")
        if os.path.exists(path):
            with open(path, encoding="utf-8") as file:
                answers[mode] = {a["task"]: a for a in json.load(file)}

    def cell(answer):
        text = (answer or {}).get("answer") or (answer or {}).get("error") or ""
        return text.replace("|", "\\|").replace("\n", "<br>")

    lines = ["# Antwoorde langs mekaar", "",
             "Elke vraag se antwoord: eers deur een agent met alle gereedskap (basislyn), dan deur Puls met sy spesialiste "
             "(gedelegeer). Skryf korreksies onder *Korreksies*: woorde wat te styf is, anglisismes, spelling.", ""]
    for key, question in QUESTIONS:
        lines += [f"## {key}: {question}", "", "| Basislyn | Gedelegeer |", "|---|---|",
                  f"| {cell(answers.get('baseline', {}).get(key))} | {cell(answers.get('delegated', {}).get(key))} |", "",
                  "**Korreksies:**", "", "- ", ""]
    with open(os.path.join(out, "answers.md"), "w", encoding="utf-8") as file:
        file.write("\n".join(lines))


def report(out):
    """Totals again from a saved run's usage records, without model calls."""
    with open(os.path.join(out, "results.json"), encoding="utf-8") as file:
        results = json.load(file)
    for mode in results:
        with open(os.path.join(out, mode, "usage.jsonl"), encoding="utf-8") as file:
            results[mode].update(summarise([json.loads(line) for line in file]))
    with open(os.path.join(out, "results.json"), "w", encoding="utf-8") as file:
        json.dump(results, file, indent=2)
    side_by_side(out)
    print_results(results)


def print_results(results):
    for mode, r in results.items():
        print(mode, r["all"], flush=True)
        print("   own", r["own"])
        print("   specialists", r["specialists"])
        for key, t in r["per_task"].items():
            print("   ", key, t)


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--report":
        report(sys.argv[2])
    else:
        arguments = sys.argv[1:]
        repeat = 1
        if "--repeat" in arguments:
            i = arguments.index("--repeat")
            repeat = int(arguments[i + 1])
            del arguments[i:i + 2]
        main(arguments[0] if arguments else "samples/sample-output-puls-check", repeat)
