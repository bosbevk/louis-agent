# 17 · Observability: every host's logs and traces on one wire

> **Skill:** #17 in the [goal](../GOAL.md)'s map, with #10 (a sub-session's trace across two processes), #14 (a
> crash logged once) and #18 (the same data to a local or a cloud backend by a setting) · **Stage:** Phase 3,
> F52 *Observability through OpenTelemetry*, nine steps in nine pull requests (#121 to #130, 2026-11-03 to
> 2026-11-04), [ADR 0064](../adr/0064-observability-through-opentelemetry.md) · **Written by** Claude, for Louis
> to read (2026-11-04) · **Next:** speed on every ledger record and OpenTelemetry metrics (F44.1, Phase 9); uptime
> and alerting on the traces (F51.2)

What this answers: what observability is for an agent system, how our logs and traces went from "in the process
only" to "on a dashboard, every host side by side", what each of the eleven picks was and why, what it cost when
measured, and what the build taught.

## The idea in a paragraph

A log line says *what happened*; a **span** says *how long something took and what it was part of* (a trace is a
tree of spans with one id). An agent's turn is a natural tree: the request, the turn, the model calls, the tool
calls, and under a tool call the specialist's own turn. **OpenTelemetry** is the standard both .NET and Azure
speak: the code names its spans and log lines, an **exporter** sends them over one wire (OTLP) to whichever
backend the `OTEL_*` settings name, and the backend draws the tree. So "which tool call made that turn slow?" is a
click, and the same code talks to the Aspire dashboard on Louis's PC and to Application Insights on Azure, with
nothing of ours naming either.

## Before and after

| | Before F52 | After F52 |
|---|---|---|
| Logs | Serilog: stderr, a run file per process, JSON-lines records (CL3) | The same, plus every line to the collector `OTEL_EXPORTER_OTLP_ENDPOINT` names, with its `service.name` |
| Spans | F18 had put Microsoft.Extensions.AI's `chat` and `execute_tool` spans in the tool loop, but nothing listened: they died in the process | Exported by the OpenTelemetry SDK; a turn is `invoke_agent {agent}` above them, tagged with session, host, turn, run and task, never a word of the conversation |
| A sub-session | A separate story on the peer, no link to the tool call that opened it | Nested under that tool call across the HTTP hop: `HttpClient` writes the W3C `traceparent` header, ASP.NET Core reads it |
| A crash | Nothing of ours logged | One Critical line with the exception object, then the last log lines and spans still leave |
| Where to look | The run file, grep | The Aspire dashboard on the PC (its own compose stack), Application Insights on Azure (`deploy.ps1 telemetry`) |
| What a level means | Each author's feel: 64 Information, 61 Warning, 6 Error, no Critical | A table in the guide and the ADR, fixed as a file is touched |

## The eleven picks, in plain words

Six were in the plan, five came up during the build; each went to Louis on a card with three options and a
pattern. The names in brackets are the cards'.

- **One exporter per signal (A1).** Logs through Serilog's OTLP sink, beside the console and the file, so the
  templates, the hashing and the tags stay in one pipeline; traces through the OpenTelemetry SDK, the reference
  every instrumentation plugs into. Weighed against it: Serilog for both (a bridge between two standards) and the
  SDK's logger beside Serilog (two logging stacks in one process).
- **.NET's own sources beside ours (B2).** `Microsoft.AspNetCore` and `System.Net.Http` are already in the
  runtime: a span per request, a span per outgoing call, and the `traceparent` header on it. That one header is
  what makes a sub-session nest under its parent's tool call with nothing of ours (W3C Trace Context). The
  instrumentation packages give richer attributes, but the ASP.NET one can't live in core.
- **The turn's span opens in `TurnRunner` (C1).** The one place a turn runs, whole or streamed, so every host and
  every sub-session gets it (Template Method's hook). Not in `UsageScope` (one class, two concerns) and not per host
  (seven copies, and one forgets).
- **Switched on in the shared bootstrap (D1).** `AgentHost` after the `.env` files load, so five hosts get it with
  one line; the orchestrator and the eval runner call it themselves (Composition Root).
- **The dashboard is its own stack (E2).** Started once, both compose stacks send to it, so Puls's trace and the
  code-review API's sit side by side (a backing service, twelve-factor). A copy per stack would give two dashboards
  on clashing ports.
- **Levels fixed as touched (F1).** The table is written where a reviewer checks it; no 30-file sweep (the Boy
  Scout Rule, as ADR 0057 does it).
- **The runner gets the address, not the key (B).** The API hands the eval runner an allowlist of settings (FX25);
  the endpoint and the protocol pass, `OTEL_EXPORTER_OTLP_HEADERS` doesn't, because a collector's key may be in it.
- **The factory names the engine (G1).** `gen_ai.agent.name` is the profile's role, else the default mode, so a
  trace tells the cook's turn from the chat's.
- **Our own traffic is no span (H1).** The log sink sends its batches on a plain `HttpClient`, which the tracer
  listens to, so every batch became a `POST` span to the collector. A small processor before the exporter drops
  them (the SDK already hides the trace exporter's own).
- **Critical for a crash, Error for a lost task (I1).** The level says whether the process lived on: an exception
  nothing caught ends it, so Critical and a flush; a faulted task nobody awaited hasn't ended a process since
  .NET 4.5, so Error, marked observed, no flush.
- **A host is named by its project (J1).** `api`, `acp-server`, `cli`, `evals.runner`; only `puls`, which is the
  API's project in another coat, sets `OTEL_SERVICE_NAME`.
- **Application Insights on the environment's workspace (K1).** The script creates `puls-insights` on the Log
  Analytics workspace the container environment already logs to, so traces sit beside the container logs, and turns
  on the environment's managed OpenTelemetry agent, which injects the endpoint into the app.

**Left out on purpose** (ADR 0055): metrics (the ledger is the cost, F44.1 takes speed), alert rules (by hand in
App Insights first), the browser apps (Blazor sends nothing), and the errors inbox Louis first asked for
(Exceptionless: a second pipe, its own server or a one-project free plan; GlitchTip only if the inbox is missed
after a month).

## What it costs, measured

Six free replays of the cook's baseline run `dc272b95eaeb` on the local Puls container, three with the exporters
off and three on (a replay plays back the recorded model answers and web pages, so nothing is paid and the model's
time is out of the picture):

| | Exporters off | Exporters on |
|---|---|---|
| The run, median wall time | 22.7 s | 22.9 s |
| Its 20 turns, together | 2.2 s | 2.9 s |
| The runs' spread | 22.0 to 40.7 s | 22.2 to 28.9 s |

About 1% of a run, and the runs' own spread is wider than the difference; a replayed turn takes about 0.1 s and
the judge on a local model is most of a run. Louis's own stopwatch, *which tool call made the slowest turn slow?*
by grep in the run files against a click in the dashboard, is the two blanks in ADR 0064's *Consequences*.

## What was learned

- **Look before you build: the spans were already there.** The plan's first check found F18 had wired
  Microsoft.Extensions.AI's spans into the tool loop months ago, with a test proving their shape. Nobody listened.
  F52 was mostly an exporter and a parent span, not an instrumentation job.
- **Nothing of ours names a backend.** The exporters read `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME`
  themselves and they win over our defaults; with no endpoint nothing listens. The same build goes to the PC's
  dashboard, to Azure's agent, or nowhere, by a setting. That is what "vendor-neutral" buys.
- **A fake handler hides the real behaviour.** The plan had the parent's `traceparent` test on a recording
  handler, which would have seen no header: .NET writes it in the handler a real client sends through, which a
  fake bypasses. The test runs a real `HttpClient` to a loopback listener. A test proves the path it actually takes.
- **Listening to everything includes yourself.** The log sink's own sends to the collector became spans the moment
  the tracer listened to `HttpClient`. The fix is a filter at the one seam before the exporter, not a special case
  in the sink.
- **A wire-name test catches a wire name anywhere.** A test set `OTEL_SERVICE_NAME` to `"puls"` and core's
  `RepeatedNameTests` refused it (`puls` is an enum's wire name, ADR 0043). Rider had run the API's tests only, so
  every prompt since asks for the whole solution before it stops.
- **A command's fields come from its source, not its docs.** The script's first `show` query named fields the
  `az` command never returns; the extension's own code on GitHub had the real shape
  (`properties.openTelemetryConfiguration`). The command is preview, so the docs lag.
- **Flush is a second Configure.** Serilog sends a batching sink's last lines when the logger is disposed, so
  flushing at exit is replacing the logger once more with the same arguments. Not pretty, but one line and honest.
- **A cancelled turn is not an error.** Its span ends with no status: a user's stop is not a failure, and a
  dashboard that counted it as one would lie about the error rate.
- **The one text that leaves unhashed.** An exception's message travels as it is on a crash line and a span's
  exception event; `LogContent`'s hashing doesn't reach it. Worth knowing before a tool puts a user's words in an
  exception.
- **Measure with the model out of the picture.** A replay made the cost of the exporters visible at all: live, the
  model's own seconds and their spread would have buried a 0.7 s difference in the turns.

## The library's counter (F35.7, 2026-11-06)

Louis asked two things F52 couldn't answer: "the recipes is not saving?" and "so how would we know if our source is
working?" Traces show one request at a time. This is the other half of the skill: **what a system does in real use, as
opposed to what an eval proves once.** An eval says it can work; a counter says it does.

| | Before | After |
|---|---|---|
| Is the library being used? | Run an eval, or read log lines one by one | The *Reports* page, a row a day |
| Where it's kept | Docker's logs, gone with the container | `library_usage`, one small record per UTC day |
| Who can read it | Whoever has the logs | Behind the admin key |

**The picks.** One record per day, added to with `$inc` (MongoDB's Computed Pattern): a little work on each write so reading
is a short list, instead of one row per event that grows forever. A new plug-in behind the admin key, as uploads are, because
a feed is public and Puls's endpoint is sold. A general table, so the API and the page know no pack.

**Through the real stack,** with one chat asking for melktert, the report's numbers moved: *Offered* 10, *Served* 1, stored in
Mongo. With no key and with a wrong key the route answers 401, and the page in a browser shows the box for a key. The numbers on the page itself wait for a person with the key to look. That chat kept nothing,
since the recipe was already ours, so *Kept* has only been seen moving in tests, on a real Mongo for the store's half.
A counter that's down costing a chat nothing is proved by a test, not on the running stack.

**What was learned**

- **Try removing what you were sure of.** I expected two adds starting a new day to clash, and wrote a retry. With the retry
  switched off, 50 adds at once still all counted, six runs out of six, on MongoDB 8. So it went, since it was code nothing
  needed. A test that passes with and without your code isn't proving that code.
- **Four of nine tests passed on a stub** in the counting step: the ones saying something is *not* counted. That's fine as
  long as you notice; they only mean something once the counting exists.
- **Decide what a number means before counting it.** *Offered* counts what a search showed, not what it held. *Served* leaves
  out `ScaleRecipe`, or one answer would serve a recipe twice. A failed keep isn't *Failed*, since the cook loses nothing.
- **Measured runs need their own database.** With `RECIPE_LIBRARY_IN_EVALS=true` on the real database, a run adds to the
  real totals.
- **The size tests shaped the design twice:** `Library/Usage` went over the folder limit, so the Mongo record sits beside its
  store, and `Pages/Admin` did too, so the page got a `Data` folder.

**What it can't tell you:** whether an answer used a recipe, since the pack never sees answers, or how often someone reads the
open feed directly. Those stay with the eval's saved events and aren't counted.

## A recorded session is the stream, played again (F120, 2026-11-09)

**What it means.** The coding agent's page on the site shows the agent fixing a real bug: thinking, calling its
tools, reading the test output, writing the fix, running the tests again, with the bill at the end. None of it runs
while a visitor watches. It is one session, recorded once, played back.

**Why it works so simply.** The API already streams every turn as events (server-sent events: one small message per
change, `content_block_delta` for a few words, `content_block_start` for a tool call). The chat draws a turn by
folding those events into it, one at a time. So a recording is nothing more than that stream saved to a file, and
playing it is the same fold with a wait between events. A visitor sees exactly what the live chat drew, because it
is the same code. This is the payoff of a single event contract (ADR 0047): the stream is the observable truth of a
turn, so it can be logged, tested, replayed in the eval studio and now shown on a website, all from one format.

**What had to be added.** A stream carries no times, so the replay waits by kind: a few words of the answer at
reading pace, thinking faster, a beat for each tool call and its result (`RecordingPace`). The loop that applies the
events and asks for a redraw every few was already in the eval studio; it moved into the framework (`TurnReplay`),
and both use it.

**What to watch.** A recording is honest only if it is clean. The first try at recording mounted the demo's config
folder, and the host loads every `.env` file in it, so the container picked up the private key and the git identity.
It was caught before any session opened, and the script now mounts only the two price files. The session itself ran
on a sample project in a fresh git copy, never on this repo, and its file was read for anything private before it
was committed. A recording also ages: when the agent changes, the page shows the old session until it is recorded
again.

**Where it sits in the skill map:** observability (skill 17), a turn as an event stream you can replay; and
human-in-the-loop (skill 16), since the live agent with a shell stays behind the owner's key (ADR 0108).

## Sources

OpenTelemetry, *.NET getting started* and the *GenAI semantic conventions* (`invoke_agent`) · W3C, *Trace Context*
· Serilog.Sinks.OpenTelemetry README · Microsoft, *.NET observability with OpenTelemetry*, *Logging guidelines*
(the levels) and *Collect and read OpenTelemetry data in Azure Container Apps* · aspire.dev, *Run the dashboard
standalone* · Mark Seemann, *Composition Root* · The Twelve-Factor App, *Backing services* · Robert C. Martin, *the
Boy Scout Rule* · [ADR 0064](../adr/0064-observability-through-opentelemetry.md) ·
[ADR 0055](../adr/0055-keep-it-simple.md) · [ADR 0057](../adr/0057-regex-is-the-last-option.md) · the research
(`research/reports/logging-and-error-tracking.md` in the Claude project)

---
[Docs index](../README.md) · Related: [Learning](README.md) · [Debug and logs](../guides/debug-and-logs.md) · [louis-agent.core](../projects/louis-agent.core.md)
