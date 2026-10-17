# F31 · Benchmarks in the admin portal: the checks run from the portal, their results kept in a store

> **Status:** planned (2026-10-17), on `docs/F31-benchmarks-portal-plan` (the plan); the code after F30 (merged 2026-10-17) ·
> **Milestone:** — · **Depends on:** F27 (the portal, its runner and SSE), F1–F3 (the ledger the costs come from),
> F25 and F29 (the cook and the role gate the checks measure), F30's build steps (the minions the first measured run
> compares)
>
> **In the TODO:** **F31 Benchmarks in the admin portal** under *Sessions and memory*, beside F21 ([TODO](../../TODO.md)) ·
> **Plan:** [plans/F31](../plans/F31-benchmarks-portal.md)

CL10 removed the checks that measured Puls, their recorded runs and `BENCHMARKS.md`; they measured a design that CL8
and CL9 replaced. The admin portal (F27) kept its runner and its live stream but has nothing to run and nowhere to put a
result. Louis wants the checks back as scripts in `benchmarks/{check}/`, run only from the portal, with every run's
results kept in a database so runs can be compared over time, and the Benchmarks screen laid out properly before it's
built. F30's hand check and measured run (minions on, minions off) are the first measurements through it.

## User stories

**F31-S1 — Run a check from the portal, nowhere else.** As *Louis* (the operator), I want to pick a check and how
many repeats and start it from the Benchmarks page, so that a change is measured the same way every time.
- *Given* the page *then* each check in `benchmarks/` is a card with a repeat control and *Loop*; a run starts on the
  server, one at a time (409 while one runs), and the page shows its progress as the script prints it: a table of mode,
  conversation, turn, role, calls, seconds and cost, the open turn typing out, the plain lines folded below.
- *Given* no store is configured *then* the page says so and a run is refused (503): a paid run whose results vanish
  is worse than no run.

**F31-S2 — Every run is kept.** As *Louis*, I want each run stored with its configuration, its conversations, turns,
transcripts and every model request's cost, so that nothing a run cost is lost and any level can be read later.
- *Given* a run *then* its documents are in MongoDB (database `puls_benchmarks`, a container in the Puls stack, its data
  on a host folder): the run with its settings (model, provider, roles on, git commit, the check's arguments), each
  conversation with its scores, each turn with its events for replay, and one document per model request from the
  ledger, joined to its turn.
- *Given* a script that dies or a store that can't be reached mid-run *then* what was recorded stays, the run is
  marked failed with the reason, and the ledger's costs are still joined (the money was spent).

**F31-S3 — Read a run at every level.** As *Louis*, I want a run's explorer, modes side by side at every level, so
that baseline and delegated (or minions off and on) are read next to each other.
- *Given* a saved run *then* it opens to tiles (cost, requests, tokens, seconds, score), cost per mode, the token mix,
  the conversations with their score chips, a turn with its transcript replayed from the stored events, and a single
  request; the modes are the columns at every level (F27's rule).

**F31-S4 — Trends.** As *Louis*, I want a check's runs over time, so that a change's effect is read against the runs
before it.
- *Given* a check with runs *then* its card shows cost and score per run as a line, one series per mode, and a point
  opens that run.

## Design notes

- **The harness speaks a line protocol; the API records it.** A check prints plain lines and `@{json}` lines on
  stdout (F27's protocol with `repeat` and `session` added, and a version line first). The runner streams them to the
  page as before; a recorder parses the same lines into documents as they arrive, so a half-done run is already
  readable. Costs are no longer computed in Python: at the end the recorder reads the usage ledger filtered by the run's
  tag (`{run id}:{mode}:{repeat}`), skips the `sub_session` roll-ups (or delegations count twice) and joins each request
  to its turn by session (own rows) or parent (specialist rows). The script never holds a secret or a database driver
  (the image's Python has no pip), and page and store agree because they consume one stream.
- **The store is MongoDB** (Louis, 2026-10-17): a `puls-mongo` service beside `puls` and `puls-agent`, bound to
  127.0.0.1, its data dir mounted from `data/mongo` (git-ignored) so results outlive rebuilds. The API talks to it with
  `MongoDB.Driver` (Apache-2.0; the EF Core Mongo provider and MongoDB.Entities were the other options). Collections:
  `runs` (settings, modes, totals, the log), `conversations` (scores), `turns` (with the turn's events), `requests`.
  Behind an `IBenchmarkStore` with an in-memory implementation for the tests; the Mongo implementation is held to the
  same contract test, run when `BENCHMARKS_MONGO_URL` is set (CI gets a Mongo service container).
- **Settings:** `BENCHMARKS_MONGO_URL`, `MONGO_ROOT_USER` and `MONGO_ROOT_PASSWORD` in `config/.env.secrets`; read with
  `ADMIN_API_KEY` and `BENCHMARK_API_URL` into one `AdminOptions`.
- **The page keeps Puls's look:** the admin layout, cards, chips and the explorer's existing CSS (bars that grow in a
  cascade, tiles, the legend, reduced motion respected); charts in-house in SVG and CSS on Puls's variables, with the
  motion idioms of animated chart templates (bars growing, donut segments sweeping, numbers counting up, a line drawing
  itself). No chart package. The layout is drawn in the plan before a step builds it.
- **The checks:** a shared `benchmarks/puls_check/` module (sessions and SSE on the standard library, the protocol
  writer, scoring) and two checks rewritten for the current design: the cook check (`puls-only` with `web` against
  `puls-cook` with `specialists` and `web`) and the routing check (`baseline` with every toolset but `specialists`
  against `delegated`). F30's prices check is written in F30's measured-run step on the same harness, with F30's `tool` and
  `llm` session fields.
- **F27 finishes here.** Its run and read stories are met by this feature's; its status becomes *done, superseded by
  F31* in the last step.

## Done when

- **F31-S1:** a check runs from the page with live progress; a second run and a run without a store are refused.
  *Met by:* recorder and runner tests; the endpoints by hand; a free run of the stub check (`benchmarks/stub/`, plan step 6c).
- **F31-S2:** the run's documents, with the ledger's requests joined and roll-ups skipped; a failed run keeps what it
  recorded. *Met by:* the store contract (in-memory always, Mongo when configured), recorder and ledger-join tests.
- **F31-S3 and S4:** the explorer and the trend from the store. *Met by:* the trend test; the pages by hand; one paid
  run of the cook check from the page (Louis's go), its numbers in this doc.
- Suites pass; F27 closed; *Finishing a feature* (the ADR for the store and the protocol, CHANGELOG, GOAL skill 12,
  SETUP's settings, the API's and Puls's project docs).

---
[Features](README.md) · [Plan](../plans/F31-benchmarks-portal.md) · Previous: [F27 Admin portal](F27-admin-portal.md)

## What the build changed

- **Step 1 (2026-10-17): the protocol, version 1** (`Services/Benchmarks/BenchmarkProtocol.cs`). Beyond F27's events
  and the plan's three changes, what the store needs is in the lines: `mode` carries `personality`, `llm` and its
  number of `conversations`; `turn` carries the `answer`; `conversation` carries the check's own `scores` document
  instead of each check's fields at the top level, so one parser reads every check. A line is a log line unless it is
  a whole, known event: plain text, bad JSON, an unknown event, a missing or mistyped `mode`, `repeat`, `task`,
  `turn` or `session`. The keys are records (`ConversationKey`, `TurnKey`), the store's keys in step 2. The new
  folder `Services/Benchmarks/` holds F31's types; F27's `Benchmark*` files stay in `Services/` until a move is
  worth its diff.
- **Step 2 (2026-10-17): the store's seam** (`IBenchmarkStore`, `BenchmarkDocuments`, `InMemoryBenchmarkStore`). One
  record per collection (`Run`, `Conversation`, `Turn`, `Request`), the same in memory and in MongoDB. A turn's stream
  events are saved with it but read apart (`TurnEventsAsync`), so a list of turns never carries thousands of events.
  A conversation has an `Order` (when it started in its run), since neither insertion order nor its task's name keeps
  the check's order in MongoDB. A request's tokens and cost stay null when the ledger doesn't know them (FX9, an
  unpriced model), never 0. The contract (`BenchmarkStoreContract`, 13 tests) gives every test its own run ids and
  check name, so step 5c runs it on a shared database unchanged.
- **Step 3 (2026-10-17): the recorder** (`BenchmarkRecorder.RecordAsync(run, settings)`). It listens before its first
  write, so no line printed meanwhile is lost, and a run that already ended is recorded whole. It writes the run when it
  starts and at each mode, a conversation at its first question (its `Order`, no scores yet) and again when it ends, a
  turn with its events when it ends; the plain lines become the run's `log`, written when the run ends. A turn the run
  cut off is kept with its question, its events and the error `BenchmarkTurnCutOff`. An unknown protocol version is
  said in the log and the run is still read. A store that fails stops every later write but the run's last one, which
  carries `store_error`; the run itself never waits on it. `Total` stays empty until step 4.
- **Step 4 (2026-10-17): the cost, from the ledger** (`BenchmarkLedger`, called by the recorder when the run ends).
  A run's rows are those tagged with its id or `{id}:…` (not an id that merely starts the same); the `sub_session`
  roll-ups are skipped, since they repeat the child's own rows in the parent's turn. A row joins its turn by its
  session (Puls's, which the script named) or by its `parent`, followed up through a specialist, so a minion's cost
  lands on the Puls turn that sent its specialist. A row nobody can place is kept with no turn and still counted. The
  run's `total` is `UsageReport.Total` over those rows, as every host totals usage, so the portal and the ledger
  agree. The ledger's files are read for every month the run spans; an unreadable one leaves the cost unknown and
  says so in the log. Open: whether the last turn's rows are always written before the script ends (the old scripts
  waited two seconds); step 11b's paid run checks the count.
- **Step 5a (2026-10-17): `AdminOptions`** (`src/louis-agent.api/Config/`, a new folder for the API's settings), read
  once at start-up as `AgentOptions` is: the portal's key, the URL a script calls back, and the store
  (`BENCHMARKS_MONGO_URL`), plus `BENCHMARKS_MONGO_DATABASE` (`puls_benchmarks` by default) so the contract tests can
  use `puls_benchmarks_test` on the same server. `Program.cs`'s three reads go; nothing else changes: by hand on the
  rebuilt Puls, `GET /admin/benchmarks` is 401 without the key and 200 with it.
- **Step 5b (2026-10-17): `puls-mongo`** in `docker/docker-compose.puls.yml` (`mongo:8`, `127.0.0.1:27017`, its data
  on the host in `data/mongo`, git-ignored, a `mongosh` ping as its health check; `puls` starts after it). Its root
  user is `MONGO_INITDB_ROOT_USERNAME` and `_PASSWORD` in `.env.secrets`, the image's own names, not the plan's
  `MONGO_ROOT_USER`: compose fills `${…}` only from `config/.env`, so the container reads `.env.secrets` itself.
  **The restart test passed on Louis's Windows host** (Docker Desktop, the host folder): a document written as root
  read back after `restart puls-mongo` and after the stack's `down` and `up`, no errors in Mongo's log; an
  unauthenticated read is refused. So the host folder stays and no named volume is needed.
- **Step 5c (2026-10-17): `MongoBenchmarkStore`** on `MongoDB.Driver` 3.12.0. The documents are mapped to BSON by
  hand, field by field in snake case, so what `mongosh` shows is what the portal reads; a check's scores and a turn's
  stream events are stored as the JSON they came as. Conversations and turns have their key as a compound `_id`, so a
  save is a replace by key. The indexes are made on first use. A server that doesn't answer fails a call within five
  seconds, so the recorder gives up quickly. The same 13 contract tests pass against `puls-mongo` on Louis's machine
  (`BENCHMARKS_MONGO_URL` set in the shell, with `127.0.0.1`) and are skipped, with the reason, without it; the test
  database is dropped afterwards. CI gets a throwaway `mongo:8` service, so the contract runs there on every push.
  Noticed meanwhile, not changed: `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.12 has two published
  high-severity advisories (NU1903 on restore).
- **Step 6a (2026-10-17): the wiring.** `Program.cs` builds the Mongo store and the recorder (with this instance's
  ledger directory) when `BENCHMARKS_MONGO_URL` is set; `POST …/runs` answers 503 (`NoBenchmarkStore`) without one,
  else attaches the recorder to the run it starts (`Attach`, in the background, kept by run for `RecordingOf`). The
  runner gives the script `BENCHMARK_RUN`, its run id. `done` waits up to 30 seconds for the recording and carries
  `stored` and `store_error`. A run keeps its settings (`BenchmarkSettings`: provider, model, the personalities
  allowed, the check's script and arguments); not yet the commit, which needs git run in the container (open).
  `CloseOrphans` runs at start-up in the background, so a store that's down never delays the API: by hand, a run
  planted as `running` in `puls_benchmarks` was `failed` with its end time after the rebuild, and the run file said
  `1 benchmark run(s) a restart cut off are marked failed`.
