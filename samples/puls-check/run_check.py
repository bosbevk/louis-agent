"""The Puls delegation check (F5, F21, F22): the same questions asked twice through Puls's API, first by one agent with
every toolset (the baseline), then by Puls delegating to its specialists, each question in a session of its own. The
usage ledger (logs/puls) is read back for each run's tokens by kind and cost, Puls's own and its specialists', per
question; each delegation's toolsets and role are recorded from the tool call.

Spends money (Claude). Run from the repo root with the Puls containers up:
    docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
    python samples/puls-check/run_check.py samples/sample-output-puls-check
Totals again from a saved run, without model calls:
    python samples/puls-check/run_check.py --report samples/sample-output-puls-check
"""
import json
import os
import sys
import time
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
]

TOKEN_KINDS = ["input", "cache_write", "cache_read", "output"]


def call(method, path, body=None, stream=False):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(API + path, data=data, method=method, headers={"Content-Type": "application/json"})
    response = urllib.request.urlopen(request, timeout=600)
    return response if stream else json.loads(response.read() or b"null")


def ask(toolsets, run, task, question):
    """One question in a new session: the answer's text, the tools it called, its delegations (toolsets and role), the
    specialists that answered, and an error if any."""
    session = call("POST", "sessions", {"toolsets": toolsets, "personality": "puls", "time_zone": "Africa/Johannesburg",
                                        "task": task, "run": run})["session_id"]
    text, tools, specialists, delegations, error = [], [], [], [], None
    calls = {}   # content block index -> [tool name, its input JSON so far]
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
                delta = data.get("delta") or {}
                if event == "content_block_start" and data.get("content_block", {}).get("type") == "tool_use":
                    tools.append(data["content_block"]["name"])
                    calls[data["index"]] = [data["content_block"]["name"], ""]
                elif event == "content_block_delta" and delta.get("type") == "text_delta":
                    text.append(delta["text"])
                elif event == "content_block_delta" and delta.get("type") == "input_json_delta" and data.get("index") in calls:
                    calls[data["index"]][1] += delta.get("partial_json", "")
                elif event == "content_block_stop" and calls.get(data.get("index"), [None])[0] == "StartSubSession":
                    try:
                        arguments = json.loads(calls[data["index"]][1] or "{}")
                    except json.JSONDecodeError:
                        arguments = {}
                    # The toolsets and the role Puls chose for the task (F21: is the role passed?).
                    delegations.append({"toolsets": arguments.get("toolsets"), "personality": arguments.get("personality")})
                elif event == "sub_session" and data.get("event") == "message_start":
                    specialists.append(data.get("agent"))
                elif event == "error":
                    error = json.dumps(data)
    finally:
        call("DELETE", f"sessions/{session}")
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
    """A run's totals: all, Puls's own, its specialists' (records with a parent), and per question."""
    own = [r for r in records if not r.get("parent")]
    sub = [r for r in records if r.get("parent")]
    # A specialist's records name their parent ("{session}#{turn}"); the parent session is the question's.
    task_of_session = {r["session"]: r.get("task") for r in own}
    task_of = lambda r: r.get("task") if not r.get("parent") else task_of_session.get(r["parent"].split("#")[0])
    return {"all": total(records), "own": total(own), "specialists": total(sub),
            "per_task": {key: total([r for r in records if task_of(r) == key]) for key, _ in QUESTIONS}}


def main(out):
    available = [t["name"] for t in call("GET", "toolsets")["toolsets"] if t["available"]]
    modes = {
        # The baseline: one agent with every toolset this instance has (not agents), no delegation.
        "baseline": [t for t in available if t != "agents"],
        # Puls: agents only; every task goes to a specialist with its role.
        "delegated": ["agents"],
    }
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    results = {}
    for mode, toolsets in modes.items():
        run = f"puls-check-{mode}-{stamp}"
        os.makedirs(os.path.join(out, mode), exist_ok=True)
        answers = []
        for key, question in QUESTIONS:
            started = time.time()
            text, tools, specialists, delegations, error = ask(toolsets, run, key, question)
            answers.append({"task": key, "question": question, "answer": text, "tools": tools, "specialists": specialists,
                            "delegations": delegations, "error": error, "seconds": round(time.time() - started, 1)})
            print(f"{mode:9} {key:8} {answers[-1]['seconds']:6.1f}s  tools={tools}  delegations={delegations}")
        time.sleep(2)   # the ledger's last lines
        records = ledger(run)
        results[mode] = {"run": run, "toolsets": toolsets, **summarise(records)}
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
    print_results(results)


def report(out):
    """Totals again from a saved run's usage records, without model calls."""
    with open(os.path.join(out, "results.json"), encoding="utf-8") as file:
        results = json.load(file)
    for mode in results:
        with open(os.path.join(out, mode, "usage.jsonl"), encoding="utf-8") as file:
            results[mode].update(summarise([json.loads(line) for line in file]))
    with open(os.path.join(out, "results.json"), "w", encoding="utf-8") as file:
        json.dump(results, file, indent=2)
    print_results(results)


def print_results(results):
    for mode, r in results.items():
        print(mode, r["all"])
        print("   own", r["own"])
        print("   specialists", r["specialists"])
        for key, t in r["per_task"].items():
            print("   ", key, t)


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--report":
        report(sys.argv[2])
    else:
        main(sys.argv[1] if len(sys.argv) > 1 else "samples/sample-output-puls-check")
