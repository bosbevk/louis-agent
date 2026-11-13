# louis-agent.api — design

> **Kind:** library, ASP.NET Core minimal API · **Path:** `src/hosts/louis-agent.api/` · **References:** louis-agent.core, and no web
> app: a demo's program references the one it serves ·
> **Docker services:** `api` (http://127.0.0.1:5080); `demo-api` in the demo; `puls` (5070)

![The agent over HTTP, and its parts: sessions, the routes, the site's pages, the workspace and the admin key](diagrams/louis-agent.api.svg)

## Purpose

The agent over HTTP: sessions whose answers stream as Server-Sent Events shaped like Claude's, plus read-only workspace
browsing and a git interface, the packs' screens and uploads, and the admin key. It also serves the web app. The eval
studio is [louis-agent.api.evals](louis-agent.api.evals.md), a library on it a program opts into (CL57). Other programs and agents (the orchestrator) use it to hand
louis-agent a whole task.

## How it works

![louis-agent.api: How it works](diagrams/louis-agent.api-how-it-works.svg)

- **Start-up** (`ApiStartup.cs`, called by a demo's program): `AgentHost.Build()`, the services with `ApiMap.AddApi` (the engine, `AgentSessions` and
  a `WorkspaceBoundary` for path checks); decide the auth with core's `HostAuth` (FX18: no `AGENT_API_KEY` stops
  start-up unless `AGENT_AUTH=off`; a key adds `AgentKeyGate`, whose check is `HostAuth.HasKey`); map static assets,
  then the API with `ApiMap.MapApi`, and the admin portal on the `/v1` group it returns. Nothing else: the routes and
  the turn's stream live in `Endpoints/` and `Sessions/`.
- **Routes** (CL20, [ADR 0039](../adr/0039-api-routes-under-v1.md)): every route is under `/v1` but `/health`, mapped
  in one place (`Endpoints/Routing/ApiMap`). Resources are nouns; a create answers `201` with `Location`, a delete is
  `DELETE`, an action is `POST /v1/{resource}/{verb}`. A route needs the key unless the code marks it
  (`RouteAccess.Open` for the prices, the rates and the feeds, `AdminKey` for `/v1/admin`); a `/v1` path no route matches is a
  JSON `404`. `ApiRoutesSnapshot.json` pins the table, and `ApiRouteRuleTests` checks the rule on it.
- **The site's pages as HTML** (F121, [ADR 0109](../adr/0109-a-page-is-served-as-html.md)): a program calls
  `MapLouisAgentSite` with its web app's assembly and the services its pages inject, in place of an `index.html`
  fallback. A page's address (or a section's under it) answers the site's `index.html` with the page's own title,
  description, link card, canonical link and body, rendered by `HtmlRenderer` the first time and kept until the
  process restarts; a page that declares no `PageHead`, or fails to render (logged once), answers the plain
  shell; an address that names no page, the shell with a 404. The public address is the request's own. The page's
  JSON-LD scripts go into the served head as they are, and every page under the landing gets a `BreadcrumbList` of
  the site's name (`og:site_name`) and the page's title at its route (`PageBreadcrumb`, F123,
  [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md)).
- **Prices** (`GET /v1/prices`, public): the price table `AgentHost.Build` loaded for this API's usage ledger, so other
  hosts can set `PRICES_URL` to it and price with the same table (the demo's orchestrator does); `404` without one.
- **Rates** (`GET /v1/rates`, public, F14): the exchange rates `AgentHost.Build` loaded, in our `rates.json` format, for
  other hosts' `RATES_URL` (`RATES_FORMAT=rates-json`); `GET /v1/rates/display` gives the web app the server's display
  currency and a symbol per currency. Usage objects add `display_cost`, `display_currency`, `display_symbol`,
  `rate_as_of` beside the charged `cost`.
- **Feeds** (`GET /v1/feeds`, `GET /v1/feeds/{name}`, `GET /v1/feeds/{name}/{id}`, public,
  [ADR 0070](../adr/0070-packs-publish-data-feeds.md)): the data the packs publish (`AgentCatalog.Feeds`), listed with
  each one's path and read as the JSON its pack wrote; the API knows no pack and no feed's shape. A name no pack
  publishes, or an item a feed doesn't have, is a `404`. The first is `shop-prices`; `recipes` gives each item an address
  ([ADR 0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md)).
- **Screens** (`GET /v1/admin/screens`, `GET /v1/admin/screens/{name}/rows?search=&page=`, behind the admin key,
  [ADR 0086](../adr/0086-a-packs-screens-are-described.md)): what each screen the packs list says about itself
  (`AgentCatalog.Screens`), and one page of its rows with the total. A page before the first is the first, and a screen no pack
  offers is a `404`. `GET` and `PUT .../rows/{id}` read a row as a form and save it, and `POST .../actions/{action}` does what the
  description names; anything else is a `404`, and each save and action is logged with its row.
- **Uploads** (`GET /v1/admin/imports`, `POST /v1/admin/imports/{name}/preview` and `/save`, behind the admin key,
  [ADR 0071](../adr/0071-packs-take-data-uploads.md)): the data uploads the packs declare (`AgentCatalog.Imports`),
  the files as base64 in the body (20 MB at most, else `413`); the pack's report comes back as it wrote it. An
  unknown upload is a `404`, no files a `400`. The first is `shop-prices`, Stats SA's two spreadsheets.
- **Sessions** (`AgentSessions`): in memory, one turn at a time per session (a second message gets `409`), cancelled by
  `POST /v1/sessions/{id}/cancel` or by the client disconnecting; idle sessions are dropped after 4 hours.
- **Toolsets per session** (F5): `POST /v1/sessions` takes `toolsets` (none: `skills` only) and gets that profile's engine
  from core's `AgentEngineFactory`, kept for the session's life; unknown or unavailable names, a `depth` other than 0
  or 1, or `specialists` at depth 1 are a `400`. `parent` tags a sub-session's usage records with the turn that opened it.
  `GET /v1/toolsets` lists names, descriptions and availability, never tool definitions. The code-review web app opens
  chats with every available toolset; Puls opens them with `agents` and `web` and asks its cook on its peer by address.
  Engines are shared by every session with the same toolsets, role and level, and the least recently used of
  16 is dropped (a session keeps its own).
- **Roles** (F21, CL14): `POST /v1/sessions` takes `role`, a built-in one at the session's level (a mode such as
  `puls/voice-af` at depth 0, a specialist's role such as `puls-cook` at depth 1); none gives the default mode,
  `louis-agent/voice`; an invalid or unknown name is a `400` before an engine is built. `GET /v1/roles` lists them by
  level, with the default. A sub-session (depth 1) gets no routing, and the role its parent names; with none it gets
  the default mode's voice (CL10: a role is never derived from the toolsets).
- **The user's place** (F22): `location` (only when the user shared it) and `time_zone` (IANA; an unknown one is a
  `400`) are kept with the session, so `GetToday` and "near me" are the user's. A location is said once, in front of
  the session's first message (`ApiModelText.UserPlace`: `[The user is in {Location}; their time zone is {TimeZone}.]`),
  not in the system prompt, which engines share; without a `time_zone` the note and `GetToday` use
  `Africa/Johannesburg`. Files attached to a message are
  remembered by name, so `AskAgent` can forward them to the agent it asks without the model copying them.
- **Web input recorded and replayed** (F18 step 4a): `POST /v1/sessions` takes `replay` (`{ web, of, model }`) only on a session with
  a run tag, and **sending it, even an empty `{}`, is what marks a session as an eval**: a run tag alone marks nothing (the orchestrator
  sends one on every fix session), so with no `replay` the session is left alone whatever `WEB_REPLAY` says. `ReplayResolution` decides,
  before any engine is built (each refusal a 400): a `replay` with no run tag; an unknown `web` (the supported names are listed);
  `model: true` with any web but `replay` (step 4e: the model's answers come from a recording, with its web); `record` or `replay` on an instance with no replay folder; `replay` without `of`. The
  instance's `WEB_REPLAY` (parsed once at start-up, so a bad value stops the API) only fills in a `web` the eval didn't send. A
  recording without `of` writes `{run tag}/{task}`; a sub-session is sent its parent's `of` and keeps it, so it never works out a key of
  its own, and a child of a parent that said `off` is sent nothing and stays live. The session keeps the result, and `TurnStream` sets it for each turn as it sets
  the user's place.
- **Streaming** (`Sessions/Streaming/`, F18 step 1f): the writer is `ITurnStreamFormat`, and the turn and the session
  view call only it. Its one format is `Claude/ClaudeStreamFormat`; `"stream"` on `POST /v1/sessions` picks the format
  (`StreamFormat`, Claude by default) and the session keeps it. Engine updates become `message_start`,
  `content_block_start/delta/stop` (thinking, text, tool_use, tool_result), `message_delta` (stop reason and the
  turn's `usage`, F3) and `message_stop`;
  failures become an `error` event in Claude's shape, with the turn's `usage` when a request had returned (FX6).
  The error's `type` is the failure's kind (`StreamErrorKind` in `louis-agent.session-stream`, FX26): `api_error`
  for any failure, `replay_miss_error` when the engine's replay had no recorded answer (`ModelReplayMissException`,
  mapped by `Sessions/Streaming/StreamErrorKinds`), so a client can tell a replay miss from the agent's own failure.
  `/tools`, `/approve`, `/reject` and `/usage` are answered without the model.
- **Usage** (F3): each session keeps its finished turns' records (`UsageSession`) for `/usage` and the `usage` of
  `GET /v1/sessions/{id}`; the turn is added in the stream's `finally`, so a disconnected or failed turn still counts.
- **Workspace and git:** every path goes through core's `WorkspaceBoundary` and `SensitiveFiles` (`Guardrails/`), so the API applies
  the same boundary and secret-file rules as the agent; git runs without a shell with a 30-second timeout.
- **History and merging** (`GitCommitsEndpoints`, `GitBranchesEndpoints`): branches with ahead/behind counts, the log
  across branches, commit and branch diffs (secret files' content removed), `--no-ff` merges that abort cleanly on
  conflict, deletion of merged branches. Branch names are only accepted if they exist, and keep their slashes in the
  route (`/v1/git/branches/feature/x`).

The full endpoint and event reference: [How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-http-api-louis-agentapi).

**A session names its identity, and a door lets a visitor in (F109.3).** A host runs the identities its catalogue
holds; a session's request may carry `identity`, and the session opens in that identity's default voice unless it
names a role, the replies say who, and `GET /v1/identities` lists them (the first is the default). A host may name
open identities and a cap (`OPEN_IDENTITIES`, by commas, and `OPEN_SESSIONS_PER_DAY`, since F127.2 a list): the key
gate asks `OpenDoor` first, a visitor with no key and no sign-in may open one of those identities' sessions a few
times a day from one address across them all, the request is narrowed in code to the identity the door admitted and
its own toolsets and tagged `open-door` on the ledger, the calls on that session pass, and past the
cap the answer is a 429. A session an admitted session's turn asks for (its `parent` tag) counts as that visitor's
and is admitted for them, so the visitor's window may carry on in it (F127.4.2, ADR 0117); each admitted session
remembers the visitor it counts for ([ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md),
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md)). A session knows who it runs as
(`AgentSession.Who`), and each turn sets it ambient (`TurnIdentity`, with the user's place and the rest, in
`Sessions/Streaming/TurnAmbience`), so a pack can tell a demonstration's turn from a real one.

## Diagrams

The diagram under *How it works* shows the routes; this is the main one, opening a session and asking it something.

![A session and a message to it: the key gate, the session with its shared engine, and the turn streamed back as Server-Sent Events](diagrams/louis-agent.api-diagrams.svg)

![The types: a program adds the API, the handlers open a session per conversation, the turn streams in the format it picks, and the open door counts the public's](diagrams/louis-agent.api-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `ApiStartup.cs` | Start-up only (CL15, the core layout review): `AgentHost.Build`, the services, the API-key middleware (FX18), the web app's files, then each endpoint group mapped |
| `Endpoints/InfoEndpoints.cs` | `/health`, `/v1/prices`, `/v1/rates`, `/v1/rates/display`, `/v1/feeds`, `/v1/feeds/{name}`, `/v1/feeds/{name}/{id}`, `/v1/toolsets`, `/v1/model`, `/v1/roles`, `/v1/llm-profiles`: what this instance is and has |
| `Endpoints/SessionEndpoints.cs` | `POST /v1/sessions` (`201` with `Location`; with `"stream"`, the format, refused as a 400 when unknown), `GET` and `DELETE /v1/sessions/{id}`, `GET /v1/sessions/{id}/work` (its steps from its history, without its thinking; `Sessions/Work/SessionWorkReplyFactory`, F127.4.2), `POST /v1/sessions/{id}/cancel` and `/messages` (the turn as SSE, through `TurnStream`) |
| `Endpoints/ApiErrors.cs` | The one error body every endpoint returns |
| `Site/` | A site's pages as HTML (F121): `SitePages.MapLouisAgentSite`, the one call; `SiteRoutes`, the page an address names; `SitePageHandler`, the endpoint with the rendered pages it keeps; `PageShell`, the page put into `index.html`; `Rendering/`, the page rendered with `HtmlRenderer` and a `HeadOutlet` (`PagePrerenderer`, `PrerenderRoot`, `RenderedPage`, and the stand-ins for navigation and scripts); `Places/` (F127.7, ADR 0121), every section, card and piece of the site's pages, read from those renders (`SitePlaces`, rendered once when first asked; `PlaceReader` and `HtmlTag`, plain string reading; `PlaceSearch`, word matching), which `MapLouisAgentSite` hands the engines' context for the site pack's `FindPlace` |
| `Endpoints/WorkspaceEndpoints.cs` | `GET /v1/workspace/entries`, `GET /v1/workspace/file` (text ≤ 1 MB; secret files listed but not readable) |
| `Endpoints/Git/GitChangesEndpoints.cs` | `GET /v1/git/status`, `GET /v1/git/diff`, `POST /v1/git/stage` · `unstage` · `discard`: the working tree and the index |
| `Endpoints/Git/GitCommitsEndpoints.cs` | `POST /v1/git/commits` (`201` with `Location`), `GET /v1/git/commits?count=` (the log across branches), `GET /v1/git/commits/{sha}` |
| `Endpoints/Git/GitBranchesEndpoints.cs` | `GET /v1/git/branches`, `GET` and `DELETE /v1/git/branches/{name}`, `POST /v1/git/merge` |
| `Endpoints/Git/GitProcess.cs`, `Output/`, `Requests/` | The git runner (no shell, 30 seconds at most); git's output parsed (`GitOutputParser` and its records); the request bodies |
| `Endpoints/Routing/` | `ApiMap` (the services and every route, the `/v1` group, the JSON 404), `RouteAccess` and `WithAccess` (who may call a route), `AgentKeyGate` (the key check) |
| `Sessions/AgentSession.cs`, `AgentSessions.cs` | One conversation (id, engine, toolsets, depth, role, place, attachments, history, turn lock, cancellation) and the in-memory session store, which picks each session's engine |
| `Sessions/Streaming/` | The stream formats (F18 step 1f): `ITurnStreamFormat`, the writer's seam; `StreamFormat`, the enum `"stream"` names; `StreamFormats`, which makes a turn's format; `TurnUsage`, a turn's usage computed once from the ledger; `Claude/ClaudeStreamFormat.cs`, the Claude-style events |
| `Sessions/TurnStream.cs` | A turn as the API streams it: the prompt from the message, its attachments and the page the visitor is reading (F115, ADR 0104, one block cut at 6,000 characters), the engine's updates as Claude-style events, a sub-session's events live among them, the usage at the end, and (F127.8) the turn's line in the turn log |
| `Sessions/Log/` | The turn log (F127.8): `TurnLogRecord`, one finished turn (when, the session and turn, who answered, the page and the part of the site, the tools it called, never what was said), and `TurnLog`, which appends it to `turns-{yyyy-MM}.jsonl` beside the ledger and reads a month back; nothing without a log folder or with the ledger off |
| `Endpoints/Admin/Overview/` | `GET /v1/admin/overview?day=` behind the admin key (F127.8): `OverviewReader` joins the month's turn log and ledger by session into `OverviewReply` (today and the month, the talks by part and by who answered, the latest twenty), costs as the ledger charged them, an unpriced one null; `OverviewHandlers` reads the two files |
| `Sessions/SessionView.cs`, `Sessions/Requests/` | A session as the API shows it; how it reads the request bodies, which are shared records in `louis-agent.session-stream`'s `Wire/` since F127.3.1 (`CreateSessionRequestReading`: `CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `PageRequest`, `ReplayRequest`), and what `replay` resolves to (`ReplayResolution`, F18 step 4a) |
| `Admin/AdminGate.cs`, `AdminAccess.cs` | The gate on `/v1/admin` (F27): the admin key, else the agent's, else closed; what it decided |
| `Admin/AdminRows.cs` | The JSON rows the admin endpoints send (a run, a conversation, a turn, a request, a live run), with the standard records: the run's eval, version, commit and graders, a conversation's grades and outcome; null from before F18 step 2a (step 4.5b) |
| `Endpoints/AdminEndpoints.cs` | The admin portal's API under `/v1/admin` (F27, F31), behind `AdminGate`: the checks and runs, a run started (503 without a store), its live events over SSE, its requests so far (`GET runs/{id}/requests`, FX34: the ledger's rows of a run the recorder keeps, each on its turn by the run's end's own join, so the live table prices a turn as it ends; 404 for a run not recorded here, 503 for a ledger read mid-write), a check's manual and trend, a stored run with its levels, a turn's events, a run deleted. Every eval route answers under `/v1/admin/evals/…`
(the trend at `/evals/{check}/trend`) and, until a TODO line removes the alias, under its old
`/v1/admin/benchmarks/…` (F18 step 7b (iii); the routes below keep their old spelling); `RescoreEndpoints.cs` (F18 step 5b.2): `POST /v1/admin/benchmarks/runs/{id}/rescore` starts the eval runner on the stored run's export, `201` with the new run's `Location`, `404` for an unknown run, `400` for a run of a Python check, `409` while a run runs, `503` without a store |
| `Endpoints/Admin/Imports/ImportHandlers.cs`, `ImportEndpoints.cs` | The packs' uploads (F55, ADR 0071), behind the admin key: `GET /v1/admin/imports` lists each with its files, `POST …/{name}/preview` and `…/save` hand the files to the pack and return its report, all in the contract's `Admin/Imports/ImportRows.cs` |
| `Endpoints/Admin/Screens/` | The packs' described screens (ADR 0086), behind the admin key, naming no pack: `ScreenHandlers` the list and a page of rows, `ScreenRowHandlers` a row's form, `ScreenActionHandlers` an action |
| `Endpoints/Admin/Prompts/PromptsHandlers.cs` | `GET /v1/admin/prompts` (CL27, ADR 0063), behind the admin key: every built-in prompt from core's `PromptCatalog` as its header says it, a `PromptRow` each (path, name, description, level by its wire name or null, the toolsets it always loads) in the contract's `Admin/Prompts/PromptRows.cs` |
| `Config/AdminOptions.cs` | The admin key, `ADMIN_API_KEY` else `AGENT_API_KEY`; neither closes the admin routes. The studio's settings are `EvalOptions` in louis-agent.api.evals (CL57) |
| `Endpoints/Admin/AdminGroup.cs`, `AdminGateFilter.cs`, `AdminEndpoints.cs` | The gated `/admin` group the API hands out (CL57): one filter that refuses without the key, a group per library that maps on it; the API's own admin routes are the prompts, the imports and the screens |

## Configuration

Core settings (`WEB_REPLAY` and `REPLAY_DIRECTORY` among them: F18 step 4a, read through `ReplayResolution` and `AgentOptions.KeepsRecordings`), plus: `AGENT_API_KEY` (required on every `/v1` route but the prices, the rates and the feeds; without it the API
refuses to start unless `AGENT_AUTH=off`, FX18), `API_PORT` (host port in
Docker, bound to `127.0.0.1`), `GIT_AUTHOR_*` / `GIT_COMMITTER_*` (identity for commits and merges from the web app),
`ASPNETCORE_URLS` / `ASPNETCORE_ENVIRONMENT` outside Docker (an unpublished `dotnet run` only serves the web app in
`Development`). The admin portal: `ADMIN_API_KEY` (falls back to `AGENT_API_KEY`), `BENCHMARKS_MONGO_URL` and
`BENCHMARKS_MONGO_DATABASE` (the store; without it no run starts), and `EVAL_RUNNER` (the eval runner's dll: the API lists and starts the C# evals with it; default `/app/evals/louis-agent.evals.runner.dll`), [SETUP](../SETUP.md).

The images carry the eval runner: both Dockerfiles (`demos/louis-agent/docker/images/Dockerfile.api`, `Dockerfile.puls`) publish it in the
build stage and copy it to `/app/evals`, its own folder beside the API's (F18 step 4.3b). Compose sets no `EVAL_RUNNER`.

## Extending it

- New endpoints in their concern's `*Endpoints.cs` with a `Map…` extension, mapped from `ApiMap.MapApi`; reuse
  `GitProcess.RunAsync` for git and `ApiErrors.Result` for errors (Claude's error shape).
- New stream content goes through `ITurnStreamFormat` (its Claude format, `Sessions/Streaming/Claude`), so every client sees the same event shapes.
- Anything that changes agent behaviour belongs in core, not here.

The steps are in [Change the API](../guides/change-the-api.md). In short: an endpoint in its concern's `*Endpoints.cs`,
mapped from `ApiMap.MapApi` under `/v1`, needing the key unless it is marked open on purpose, and the route golden
regenerated; a session option in `CreateSessionRequest`; new stream events added, never changed.

## Tests

`demos/puls/tests/louis-agent.api.tests`: the sessions' toolsets, the default, unknown names, depth and the `specialists` rule, shared
engines and separate histories, roles, a role opening only at its own level, and a sub-session with no role getting none (a model client that is
never called). The benchmarks (F31): the protocol, the recorder, the ledger join, the runner, the catalogue, the
manuals, the trends, the tool failures, and one store contract run against the in-memory store and, when
`BENCHMARKS_MONGO_URL` is set, MongoDB (CI sets it); since CL57 those are the studio's tests, in `louis-agent.api.evals.tests`. The routes (CL20): `ApiRoutesSnapshot.json` pins every route, its
method and who may call it, `ApiRouteRuleTests` checks ADR 0039's rule on that table, and the in-memory host
(`ApiTestHost`) runs the key gate (`AgentKeyGateTests`), a session's `201` (`SessionCreatedTests`) and the git
resources in a temporary repository (`GitResourceTests`). The site's pages (F121): `PagePrerendererTests` render a
sample page, `PageShellTests` fill the code-review site's real `index.html`, and `SitePagesTests` ask the test server
for a page, a section, a page with no description, one that throws and an address with no page, and the canonical
link from the request's own address. A streamed turn is still
checked by hand, in the browser.

## Limits and plans

- Sessions are in memory → [session architecture](../specs/SESSION_ARCHITECTURE.md).
- A usage API over the ledger (`/v1/usage` endpoints, `/v1/budgets`, `/v1/estimates`) and budget errors →
  [F7](../features/F07-budgets.md) (taking in [F11](../features/F11-usage-tab-and-reconciliation.md)); account-limit
  errors → [F8](../features/F08-reliability.md); settings per role and per session →
  [F10](../features/F10-route-settings.md).
- The eval routes still answer under their old `/v1/admin/benchmarks/…` names too, until a deploy has passed (TODO,
  *Small cleanups*).

## Related docs

[How the agents and endpoints talk](../AGENT_COMMUNICATION.md) · [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) ·
[Architecture § HTTP API](../architecture/05-building-block-view.md#http-api-and-web-app)

---
[Projects](README.md) · Previous: [louis-agent.acp](louis-agent.acp.md) · Next: [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)
