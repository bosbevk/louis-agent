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
- **Step 6b (2026-10-17): the read endpoints**, one per level the explorer opens. `GET /admin/benchmarks` adds
  `store: {configured, error}` and the last 50 `runs`; `GET runs/{id}` the run with its settings, modes, log,
  conversations, turns (without their events) and requests; `GET runs/{id}/turns/{mode}/{repeat}/{task}/{turn}/events`
  one turn's events for its replay (a route of its parts, not one key string); `DELETE runs/{id}` 204, 404 when there
  is none, 409 while it runs. Without a store, or when it doesn't answer, a read is 503 saying why. By hand on the
  rebuilt Puls with a run planted in `puls_benchmarks`: each endpoint answered as above, the scores and the events came
  back as the JSON they were stored as, and with `puls-mongo` stopped the list said `The benchmark store didn't
  answer: A timeout occurred after 5000ms…` and a run read was 503, both back to normal once it started again.
- **Step 6c (2026-10-17): the stub check** (`benchmarks/stub/run_check.py`, standard library only; in the catalogue
  as `stub`, *Proefkontrole*). A canned run of two modes and two conversations each (bobotie with a follow-up), every
  turn's stream in the API's own event shapes, the cook's as nested `sub_session` events, so the live view and the
  replay draw it as a real run; no session, no model, no cost. `StubCheckTests` run it through the real runner and
  recorder: every `@` line a known event, the store holding 8 conversations for two repeats. **The first run end to
  end, by hand on the rebuilt Puls:** `POST /admin/benchmarks/stub/runs` → 202; its SSE ended with `done`
  `stored: true`; the run read back from `puls-mongo` with its settings, both modes, 4 conversations in order with
  their scores, 6 turns and the cook's events (18, `sub_session` among them). Its total is 0 requests at an unknown
  cost: no request was made, and `UsageReport.Total` calls a cost with no priced request unknown; the page shows it
  as such. That run (`c1608ea06dc0`) stays in the store for steps 9 and 10.
- **Step 7a (2026-10-17): the shared harness** (`benchmarks/puls_check/`, standard library only): `client.py` (a
  session opened with its tags, its messages' streams read as events, the key from `AGENT_API_KEY`), `protocol.py`
  (the `@` writer, the run tag `{run}:{mode}:{repeat}`), `stream.py` (a turn's stream into the answer, Puls's calls,
  the role handed to or the sub-session continued, the specialist's calls through nested `sub_session` events, the
  thinking, the error) and `scoring.py` (rand amounts, the cook's tally or its priced rows, unconfirmed marks).
  Louis chose (2026-10-17) to split step 7 (7a the harness, 7b the cook check) and to record the fixture from **one
  paid turn**: a meal question on Puls, 8 requests on Claude Haiku 5.5, **$0.0175** from the ledger
  (`benchmarks/fixtures/session-stream.sse`, 401 KB, 1,503 events). Its expected counts
  (`session-stream.expected.json`: Puls's one `StartSubSession` to `puls-cook`, the cook's 32 calls, the hosts, the
  answer's start and length) were taken once by a throwaway reader, and both `stream.py`'s unittest and a C# test of
  `TurnStream` (read as the web app reads it) are held to them; with one count changed, both failed. No minion in
  it (no prompt hands work to one) and no error turn: those two are unit cases in `test_stream.py`. CI runs the
  Python tests (18). **Seen in the recording:** the cook called `VerifyQuote` six times though its restored role
  never mentions it: it learnt the tool from the web guide's row, which every web session loads (PR #51's review,
  should-fix).
- **Step 7b (2026-10-17): the cook check** (`benchmarks/cook/run_check.py`, on the harness; in the catalogue as
  `cook`, *Kokkontrole: Puls alleen teen Puls met die kok*). Modes `puls-only` (`web`) and `puls-cook` (`specialists`,
  `web`); the four conversations of the pre-CL10 check (cook-af and cook-en with a follow-up, cook-shops, the
  control); each session tagged `{BENCHMARK_RUN}:{mode}:{repeat}`; every stream event forwarded, the turn's reading and
  the conversation's scores (role, routed right, hosts, shops, the cook's rating, rand, unknown marks, the follow-up)
  sent as events. No ledger code and no files: the API keeps the run and prices it. Its scoring has 11 unittests. **A
  free run against a stand-in API that streams the recorded turn back** found a bug: by hand on Windows stdout is
  cp1252, and the turn's curly quotes and emoji raised mid-stream, so each conversation stopped after ~300 events and
  no turn was reported (in the container the runner sets `PYTHONIOENCODING`, so the stub's test never saw it). The
  writer now sends events as ASCII JSON (every other character escaped, read back as it was) and replaces what a
  console can't show in a plain line; two tests first. Replayed again: 12 questions, 12 turns, 18,036 events, scores
  as designed.
- **Step 8 (2026-10-17): the routing check** (`benchmarks/routing/run_check.py`; in the catalogue as `routing`,
  *Roeteringkontrole*). The pre-CL10 delegation check on the harness: twelve questions, two of each kind, each in its
  own session; `baseline` is every toolset the instance has (`GET /toolsets`, the client's new `toolsets()`) but the
  delegating ones (`specialists`, and `minions`, which a chat can't hold), `delegated` is Puls as built. Only the cook
  is on, so a meal is right with `puls-cook` and anything else when Puls answers itself; the baseline, which can't
  delegate, is right unless it failed. 10 unittests (the expected role, both modes, the baseline's toolsets, errors).
  A free replay against the stand-in API (every answer the recorded delegation): 24 questions and turns, 36,072
  events, `delegated` 2 of 12 right (the two meals), as designed. On Louis's Puls the baseline would be `files, git,
  dotnet, python, powershell, bash, web, paymo, devops, skills`: Paymo and DevOps are there because their keys are
  set, as in the old check.
- **Step 9 (2026-10-17): the list page** (`Pages/Admin/Benchmarks.razor`, rebuilt on F27's page and its CSS). The
  store's state as a pill beside *Herlaai* and a banner when there is no store or it doesn't answer (then *Loop* is
  off); a card per check with its last stored run, the repeats and *Loop* (its title says which spend money); the live
  run as a table, a row per turn as the script reports it (mode, conversation, turn, role, Puls's and the
  specialist's calls, seconds, and each turn's cost from the store once the run ends), the conversation's score chips
  under its turns, the open turn typing out in the chat's own `TurnView` and any finished one opened from its row (from
  this page's events, or from the store's when the page didn't see them); the script's plain lines folded; the stored
  runs, newest first, each a link to its explorer (step 10). A reloaded page takes the finished turns from the store,
  then follows the run's events. By hand in Chrome on the rebuilt Puls with the stub (free): 2 repeats, 12 rows, *klaar
  · in die stoor*, the cook's hand-off card in the open turn, a finished turn opened from its row, no console errors.
  Two fixes on the way: the score chip took the role from the scores (the protocol sends it beside them), and the table
  needed the panel's width. Not seen by hand: a reload mid-run (the stub ends in a second).
- **Step 10 (2026-10-17): the explorer** (`/admin/maatstawwe/{run}`: `Pages/Admin/BenchmarkRun.razor` and
  `Components/RunExplorer.razor`, F27's explorer ported from the old run folders onto the store's JSON). One repeat at a
  time (a chooser when there are more); the modes as columns at every level; the run (tiles counting up: cost,
  requests, tokens, seconds, and the score as routed right out of the conversations; cost per conversation as bars
  growing in a cascade; each mode's token mix as a donut whose arcs sweep in one after another, Jitter's idiom, new
  CSS under the explorer's section); the conversations (cost split Puls/specialist, their score chips); a
  conversation's turns on a timeline of model requests in Puls's lane and the specialist's; a turn's timeline and its
  transcript, which before a replay is its question and answer, and *Speel af soos getik* replays every mode at once
  from the stored events through `TurnStream`; one request's record. The page around it shows what the run ran with
  and its log. By hand in Chrome on the stub's 2-repeat run, its turns given made-up requests in the store for the
  check (removed afterwards): every level opened, the bars, donuts and timelines drew, the replay typed both modes out
  with the cook's hand-off card, no console errors.
- **Step 10b (2026-10-17): a manual per check** (Louis). `benchmarks/{check}/HANDLEIDING.md` beside each script, in
  Afrikaans: what it measures, its modes (Puls's tools and the specialist's apart: Louis caught the table listing
  `specialists` as if the cook had it; it is Puls's ability to hand on, and the cook gets only `web`), its
  conversations and what's right, every score and when it's ✓, what it costs, how to read it, how to run it by hand.
  `BenchmarkManuals` reads it from the catalogue entry, and only inside `benchmarks/`; `GET
  /admin/benchmarks/{check}/manual`, the list saying which checks have one. The card's *Handleiding* button opens it
  in a modal on the search dialog's frame (Esc, ×, a click outside). Tests first: every check has its manual and none
  is read from outside `benchmarks/` (C#, 6); every key a check's `score()` returns is named in its manual (Python).
  By hand in Chrome: all three manuals open, each way of closing works, the card doesn't fold, no console errors.
- **Step 11a (2026-10-17): the trend** (`BenchmarkTrends.ComputeAsync`, `GET /admin/benchmarks/checks/{check}/trend`,
  `Components/TrendChart.razor`). Per mode, a point per finished run, oldest first: what one repeat cost (the mode's
  requests over the run's repeats; null when none had a price) and the share of conversations routed right; a run
  still going isn't a point. In C# over the store, 6 tests on the in-memory one. A line per mode, small on the check
  card (cost only) and full in the explorer (cost and score, the open run marked); a point opens its run. The lines
  are revealed from the left by a growing clip, and the points are round-capped zero-length lines: a dash-drawn line
  broke into pieces and circles became ovals once the chart stretches to its width (seen by hand). **One fold for
  all the check cards** (Louis: "do not roll up 3 times, have one that rolls up them all"): a *Kontroles* toggle above
  them; folded, each keeps its head (title, *Handleiding*, chip). By hand in Chrome on the four stub runs given
  made-up costs (removed afterwards).
  **A check card selects its check** (Louis, after first asking for the runs grouped by check with a fold each:
  "each of these must be selectable and then only display the runs for the type; on initial load display nothing
  until selection"): the selected card glows, the list below holds only its runs with their count and cost
  together, and before any choice it asks for one; the card's own buttons (*Handleiding*, the repeats, *Loop*) don't
  also select it, and starting a run selects its check. The groups went: one check's runs need no grouping. A trend panel with no
  known value (the stub's costs) says so instead of drawing an empty chart; on the card it draws nothing.
  **Who each mode is** (Louis: "be more specific, say Puls with puls-cook"): the explorer's heads, tiles and legends
  say *Puls met puls-cook* or *Puls alleen*, from the roles in the run's own turns, with the mode's name beside them,
  and name the specialist by its role instead of *spesialis*; the run page's trend uses the same names.
  **The chats side by side** (Louis: "a button where I can see the replay chat side by side of Puls vs Puls with
  puls-cook, open in a modal"): *Speel gesprekke af* at the right of the explorer's crumbs opens a wide modal with one
  conversation of every mode, every turn replayed from the stored events through `TurnView`, the modes at once; a
  chooser switches conversation (it starts on the one open in the explorer), *Speel weer* replays it, Esc, × or a
  click outside closes it. By hand in Chrome on the stub's 2-repeat run.
  Louis saw the chats modal cut off at the bottom of the explorer's card, with the trend card painted over it: `.card-p`
  rises in with a filled `transform` animation, and an element so animated becomes the frame of its fixed children, so
  the modal was positioned and clipped inside the card. The card that holds the explorer doesn't rise in
  (`.card-p.holds-modal`); seen right by hand.
  **The ways on are buttons** (Louis, over four rounds: the way into a turn was a bare chevron, a conversation's only
  way in was its bar, which at R0 is nothing to click; then "remove this hover effect, the button is enough, why not
  just open the modal?"): every conversation and turn row ends in *Speel af*, which opens the chats side by side on
  its conversation, and a small *Koste ›* (a conversation: its turns) or *Versoeke ›* (a turn: its requests) for what
  the modal doesn't show; the rows themselves neither light up nor click. The breadcrumb's earlier levels are in
  Puls's orange and underline on hover. Each amount in *Koste per gesprek* has its mode's dot, so whose is whose
  reads even when its bar is empty.
  **Cost per mode beside tokens per mode** (Louis; "it must also be a graph"): at the run level, *Koste per modus* is a
  ring per mode, split between Puls's own requests and the specialist's, beside the token rings; every ring has its
  total in its middle, so a run without costs still shows a graph. **The cost pops** (Louis: "that cost value looks
  ugly … it must pop, that is the cost"): in every conversation and turn row the cost is a bold figure of its own,
  counting up, each mode's with its dot, instead of a grey label at a bar's end; and the second way on is a button,
  *Beurte ›* or *Versoeke ›*, named for where it goes. And
  a conversation's cost no longer runs into its buttons: its bar sits in a group, as at the run level.
  The live table's *Gesprek* column carries each turn's question under the conversation's name (Louis: "add a
  description next to it"), so a follow-up's row says what it asked.
- **Step 11b (2026-10-17): the first paid run** (Louis's go), the cook check, 1 repeat, run `474faef30ff0`, started
  from the API as the page's *Loop* does. Done in 5 min 47 s, exit 0, `stored: true`. **$0.04456** (R0.73), 49
  requests on Claude Haiku 5.5, 743,193 tokens: `puls-only` 18 requests $0.0126, `puls-cook` 31 requests $0.0320. The
  ledger, read straight from `logs/puls/usage-2026-10.jsonl`, says the same: 49 requests (5 roll-ups skipped),
  $0.04456. **Step 4's open point is answered for this run:** every request joined a turn (none left over), and the
  last ledger row was written 17 ms before the run ended. **But it measured no web search:** Louis's Tavily plan was
  out of credits, so every `WebSearch` was refused (*This request exceeds your plan's set usage limit*); Puls and
  the cook answered from page fetches and their own knowledge. All 8 conversations were routed right, both follow-ups
  went back to the same cook (*continued*), and the cook priced 0 of its 20 ingredients (`puls-only`'s cook-af priced
  11 of 11 from memory, which the check counts as priced: a scoring gap to look at). This is the first run that will
  carry its reason on record (step 11c).
  **Accuracy per mode** (Louis: "one that shows accuracy"): a third ring chart beside cost and tokens, the
  conversations routed right (green) and wrong (red) per mode with the share in its middle, and, where the check counts
  ingredients, the share priced under it.
  **A guess is not an answer** (Louis, on 11b's *geprys 11 / 11*: "this is not true, it was guessed; it should be
  orange, not count as answered. The idea is to get an actual answer"): a price counts as priced only with the link it
  came from; one without is `unsourced`, an orange *geskat* chip, never priced; the cook's own *Geprys: n van m* line is
  its claim, so only the shopping list's ingredient rows count (a table headed *Bestanddeel*, without its bold total
  rows: the branches and *Onseker* are tables too). Tests first (Python, 6). 11b's run, whose answers are in the store,
  was rescored with the rule: Puls alone's cook-af went from 11 / 11 priced to 0 / 10 with 10 guessed; the cook's
  three lists 0 / 4, 0 / 11, 0 / 5. **Accuracy is the answer, not the routing** (Louis on the 100 % ring: "this is not
  true"): for a check that lists ingredients, the accuracy ring is ingredients sourced (green), guessed (orange) and
  unknown (red), the share sourced in its middle; routing and the meals that came without a list are under it.
  **Performance per mode**, the fourth chart (Louis): model time split Puls / specialist, the seconds per turn in its
  middle; the four charts two by two.
  Then (Louis: "we should keep this one and call it routing"): *Roetering per modus* is the routed-right ring, and
  *Akkuraatheid per modus* the ingredients ring, shown only for a check that lists ingredients; five charts, three
  across on a wide screen.
  **Eight charts, four to a row** (Louis: a sixth from three options, then "push 4 sections into a row and fill
  them all"): besides cost, tokens, performance, routing and accuracy, *Kas per modus* (input read from the prompt
  cache against sent fresh, the hit share in its middle), *Gereedskap per modus* (tool calls that worked against
  those refused or failed, the failures in its middle, the commonest reason under it) and *Waarde per modus* (the cost
  of conversations with a real answer, routed right and, for a meal, at least 80 % priced with a source, against the
  rest; one real answer's cost in its middle). Every ring's figure the same width, so the rings line up across the
  charts. The tools ring is step 11c's first half, done now: `BenchmarkTools.Of` reads a turn's tool calls and
  failures, the specialist's and a minion's too, from its stored events (3 tests first), and the run endpoint gives
  each turn its summary, so every run has it with no backfill. 11b's run shows it: 12 of Puls alone's 23 calls and
  28 of the cook's 46 refused, *WebSearch × 9: Error: Tavily's limit was reached*; 85 % and 82 % from the cache; one
  conversation of four with a real answer in each mode (the prime rate).
  **Aligned and folded** (Louis: "this must always align"; "only 3 items in a row, then the ability to roll up
  sections"): the conversations level is one grid, a row per conversation with every mode's cell in it, so the same
  conversation sits level across the modes, with one cost scale for both and the costs in a fixed column; the charts
  three to a row; and the explorer's sections (*Oorsig*, *Koste per gesprek*, *Per modus*) and the run page's
  *Tendens* each fold from their head.
  A ninth chart to fill the rows (Louis): *Bronne per modus*, the sites each mode read pages from, South African shops,
  other South African sites and the rest, how many different ones in its middle.
  **Every chart explains itself** (Louis: "on hover can we explain what is good and bad and why?"): a chart's title
  carries Puls's info icon, and hovering or focusing it opens a note of what the chart shows, what good and bad look
  like, why it matters, and which way is better. One text per measure (`ChartInfo`), shared by the rings and the trend.
  **The trend is the nine rings over time** (Louis: "3 rows, with 3 charts each"): `BenchmarkTrends` adds, per mode
  and run, the tokens per repeat, seconds per turn, the cache's share of the input, the share of ingredients priced
  with a source, the share of tool calls that failed (from the turns' stored events, `BenchmarkTools`), what one real
  answer cost and how many sites were read; a value nobody measured stays null.
  **The admin portal is in English** (Louis: "afrikaans is just the language puls can be configured for we use
  english"): the portal is the builders' screen; Puls's chat keeps its configured language, and recorded questions,
  answers and errors stay in the language they were recorded in. The addresses are `/admin/benchmarks…`, with the
  first `/admin/maatstawwe…` kept so shared links open. The checks' manuals (`HANDLEIDING.md`) are still in Afrikaans.
