# LouisAgent.Api — design

> **Kind:** host (a library, ASP.NET Core minimal API) · **Path:** `src/hosts/LouisAgent.Api/` · **References:** LouisAgent.Core, and no web
> app: a demo's program references the one it serves ·
> **Docker services:** `api` in the coding demo (http://127.0.0.1:5082, `API_PORT`); `puls` in the Puls demo (5070)
> **Also shows:** `component`

![The agent over HTTP, and its parts: sessions, the routes, the site's pages, the workspace and the admin key](diagrams/LouisAgent.Api.svg)

## Purpose

This is the agent over HTTP. You open a session, send it a message, and the answer streams back as Server-Sent
Events, shaped like Claude's. Beside that, it lets you browse the workspace read-only, has a git interface, serves the
packs' screens and uploads, and holds the admin key. It also serves the web app.

Other programs and agents, such as the orchestrator, use it to hand louis-agent a whole task. The eval studio is a
library on top of it that a program opts into: [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md).

*Decided in:* CL57.

## How it works

![LouisAgent.Api: How it works](diagrams/LouisAgent.Api-how-it-works.svg)

### 1. Starting up

The API is a library, and a program puts it together in three calls, the way ASP.NET itself is put together. Five
programs do: the coding demo's `LouisAgent.CodingApi`, Puls's `LouisAgent.PulsApi`, the starter's
`LouisAgent.Starter.Api`, and the two sites' `LouisAgent.SiteApi` and `LouisAgent.SiteFrameworkApi`.

First the program builds its agent with `AgentHost.Build(...)`, from its own catalogue and services. It hands the
result to `builder.AddLouisAgentApi(host)`. That adds the services the routes need, through `ApiMap.AddApi`: the open
door, the engine and its engines, `AgentSessions`, and a `WorkspaceBoundary` for checking paths.

Then `app.UseLouisAgentKey()` decides on the key, with core's `HostAuth`. Without `AGENT_API_KEY`, the start-up
stops before it listens, unless `AGENT_AUTH=off` says the API is open on purpose, which is logged as a warning. With
a key, it adds `AgentKeyGate`, which asks the open door first.

Between the key and the routes, the program serves its own web app: its static files and, usually, its site's pages
(part 3). Last, `app.MapLouisAgentApi(host)` maps every route with `ApiMap.MapApi`, and the API's admin routes on the
group that returns. A program that has routes of its own asks for the same `/v1` group with
`MapLouisAgentHostGroup()`, as the site's host does for its résumé.

A program could forget the key check, so the routes fail closed. `MapLouisAgentApi` and `MapLouisAgentHostGroup`
refuse to map, and the start-up stops, if `UseLouisAgentKey` never ran. That's all start-up does; the routes and the
turn's stream live in `Endpoints/` and `Sessions/`.

*Decided in:* FX18, CL57, [ADR 0074](../adr/0074-the-folders-say-what-ships.md).

### 2. The routes

Every route is under `/v1`, except `/health`, and they're all mapped in one place, `Endpoints/Routing/ApiMap`.
Resources are nouns. A create answers `201` with a `Location`. A delete is `DELETE`. An action is
`POST /v1/{resource}/{verb}`.

A route needs the key unless the code marks it otherwise: `RouteAccess.Open` for the prices, the rates, the feeds and
a site's search, and `AdminKey` for `/v1/admin`. A `/v1` path that no route matches gets a JSON `404`. `ApiRoutesSnapshot.json` pins the
whole table, and `ApiRouteRuleTests` checks the rule on it.

*Decided in:* CL20, [ADR 0039](../adr/0039-api-routes-under-v1.md).

### 3. Serving a site's pages as HTML

A program calls `MapLouisAgentSite` with its web app's assembly and the services its pages inject, instead of falling
back to a plain `index.html`. Then a page's address, or a section's under it, answers the site's `index.html` filled in
with the page's own title, description, link card, canonical link and body. The canonical link always names the page's
own route, even when a section's address was asked for, so a search engine sees one original per page and not a copy
at every section.

`HtmlRenderer` draws each page the first time it's asked for, and the result is kept until the process restarts. A
page that declares no `PageHead`, or fails to draw (logged once), answers the plain shell. An address that names no
page answers the shell with a 404.

A site names where it lives in `SITE_ADDRESS`, a whole https address such as `https://louis-agent.dev`, or its
program passes one. Every canonical link is on that address, and a page asked for on any other host, such as www,
answers a permanent redirect (301) to the same path on it. Only pages move this way: a file and an API route answer on
any host, because Azure's health probes and a chat on another site call them there. With no address named, which is
how a developer runs it on localhost, the canonical link uses the address the request came to. `SiteAddressOptions`
reads the setting.

The page's JSON-LD scripts go into the served head just as they are. And every page under the landing gets a
`BreadcrumbList` with the site's name (`og:site_name`) and the page's title at its route (`PageBreadcrumb`).

**The pages a site's areas bring are drawn too.** A site may turn on a package of pages, such as the framework's
pages or the account pages, among its areas. The host never names such a package. `SiteRoutes` runs the services the
program hands it, finds the `SitePageAssemblies` the framework's web registered there for the areas, and reads the
pages of those assemblies beside the site's own, the first page of a route winning. `SitePageAssemblies` is a record in
`LouisAgent.Api.Contracts`, which both the web and the API reference, so neither has to reference the other.

**A page behind a sign-in is never drawn.** A route with a parameter is no page of its own, and neither is one under
`/admin`, behind the admin key. A page behind a sign-in says so with Blazor's `[Authorize]`, as the profile page does,
and is left out too, so it is never rendered for a search engine or offered to the guide as a place. The public
sign-in, register and reset pages have no `[Authorize]`, and are drawn like any other.

**The drawn pages are also an index of places.** `MapLouisAgentSite` reads every section, card and piece of those
pages into one index, `SitePlaces`, rendered once, the first time it's asked. It hands that index to two readers. The
engines get it, so an agent's `FindPlace` tool can find a place, when the host runs agents. And a visitor's search
gets it, through `GET /v1/site/places?q=` (`Site/Places/SitePlaceSearch`). That route answers a `PlacesReply` with
the places that hold the words, the best first, at most eight, each a `PlaceFound` with its own address, its heading
and its first words. No words, or none long enough, answer an empty list. It uses no model, so a search costs
nothing. It is open, since it answers only what any visitor can read on the pages. A place's words leave out what a
style or a script holds, so a diagram's rules and a page's code never turn up as words.

*Decided in:* F121, [ADR 0109](../adr/0109-a-page-is-served-as-html.md), F123, F164.1,
[ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md), FX216,
[ADR 0128](../adr/0128-a-site-names-one-address-per-page.md).

### 4. Prices and rates, open to anyone

`GET /v1/prices` is public. It gives the price table `AgentHost.Build` loaded for this API's usage ledger, so other
hosts can point `PRICES_URL` at it and price with the same table. The demo's orchestrator does. Without a table, it's a
`404`.

`GET /v1/rates` is public too. It gives the exchange rates `AgentHost.Build` loaded, in our `rates.json` format, for
other hosts' `RATES_URL` (with `RATES_FORMAT=rates-json`). `GET /v1/rates/display` gives the web app the server's
display currency and a symbol per currency. Usage objects carry `display_cost`, `display_currency`, `display_symbol`
and `rate_as_of`, beside the charged `cost`.

*Decided in:* F14.

### 5. Feeds

`GET /v1/feeds`, `GET /v1/feeds/{name}` and `GET /v1/feeds/{name}/{id}` are public. They're the data the packs publish
(`AgentCatalog.Feeds`), listed with each one's path, and read as the JSON its pack wrote. The API knows no pack and no
feed's shape. A name no pack publishes, or an item a feed doesn't have, is a `404`. The first feed is `shop-prices`,
and `recipes` gives each item an address.

*Decided in:* [ADR 0070](../adr/0070-packs-publish-data-feeds.md),
[ADR 0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md).

### 6. Screens

`GET /v1/admin/screens` and `GET /v1/admin/screens/{name}/rows?search=&page=` sit behind the admin key. They give
what each screen the packs list says about itself (`AgentCatalog.Screens`), and one page of its rows with the total.
A page before the first is the first, and a screen no pack offers is a `404`.

`GET` and `PUT .../rows/{id}` read a row as a form and save it. `POST .../actions/{action}` does what the
description names. Anything else is a `404`, and each save and action is logged with its row.

*Decided in:* [ADR 0086](../adr/0086-a-packs-screens-are-described.md).

### 7. Uploads

`GET /v1/admin/imports`, `POST /v1/admin/imports/{name}/preview` and `/save` sit behind the admin key. They're the
data uploads the packs declare (`AgentCatalog.Imports`). The files come as base64 in the body, 20 MB at most, or it's
a `413`. The pack's report comes back just as the pack wrote it. An unknown upload is a `404`, and no files is a `400`.
The first is `shop-prices`, which takes Stats SA's two spreadsheets.

*Decided in:* [ADR 0071](../adr/0071-packs-take-data-uploads.md).

### 8. Sessions

Sessions (`AgentSessions`) live in memory. A session runs one turn at a time, so a second message while one is running
gets a `409`. A turn is cancelled by `POST /v1/sessions/{id}/cancel`, or by the client disconnecting. Idle sessions
are dropped after 4 hours.

### 9. Toolsets per session

`POST /v1/sessions` takes `toolsets`; with none, the session gets `skills` only. It gets that profile's engine from
core's `AgentEngineFactory`, and keeps it for the session's whole life. An unknown or unavailable name, a `depth`
other than 0 or 1, or `specialists` at depth 1, is a `400`. `parent` tags a sub-session's usage records with the turn
that opened it.

`GET /v1/toolsets` lists the names, descriptions and whether each is available, but never the tool definitions. The
code-review web app opens chats with every available toolset. Puls opens them with `agents` and `web`, and asks its
cook on its peer by address.

Engines are shared by every session with the same toolsets, role and level. The least recently used of 16 is dropped,
though a session keeps its own.

*Decided in:* F5.

### 10. Roles

`POST /v1/sessions` takes a `role`: a built-in one at the session's level. That's a mode such as `puls/voice-af` at
depth 0, or a specialist's role such as `puls-cook` at depth 1. With none, you get the default mode,
`louis-agent/voice`. An invalid or unknown name is a `400`, before any engine is built. `GET /v1/roles` lists them by
level, with the default.

A sub-session (depth 1) gets no routing, and takes the role its parent names. With none, it gets the default mode's
voice. A role is never worked out from the toolsets.

*Decided in:* F21, CL14, CL10.

### 11. The user's place

A session can carry `location` (only when the user shared it) and `time_zone` (IANA; an unknown one is a `400`). They're
kept with the session, so `GetToday` and "near me" mean the user's day and place.

The location is said once, in front of the session's first message, as `ApiModelText.UserPlace`:
`[The user is in {Location}; their time zone is {TimeZone}.]`. It's not in the system prompt, because engines share
that. Without a `time_zone`, the note and `GetToday` use the host's zone, `HOST_TIME_ZONE`, and without that the
machine's own.

Files attached to a message are remembered by name. That way `AskAgent` can forward them to the agent it asks, without
the model having to copy them.

*Decided in:* F22.

### 12. Recording and replaying the web for evals

`POST /v1/sessions` takes `replay` (`{ web, of, model }`), but only on a session with a run tag. **Sending it, even as
an empty `{}`, is what marks a session as an eval.** A run tag alone marks nothing, since the orchestrator sends one
on every fix session. So with no `replay`, the session is left alone, whatever `WEB_REPLAY` says.

`ReplayResolution` decides before any engine is built, and each refusal is a 400. It refuses a `replay` with no run
tag, an unknown `web` (listing the supported names), `model: true` with any web but `replay` (the model's answers
come from a recording, with its web), `record` or `replay` on an instance with no replay folder, and `replay`
without `of`.

The instance's `WEB_REPLAY` is parsed once at start-up, so a bad value stops the API. It only fills in a `web` the
eval didn't send. A recording without `of` is written to `{run tag}/{task}`. A sub-session is sent its parent's `of`
and keeps it, so it never works out a key of its own. A child of a parent that said `off` is sent nothing, and stays
live. The session keeps the result, and `TurnStream` sets it for each turn, the same way it sets the user's place.

*Decided in:* F18 step 4a, F18 step 4e.

### 13. Streaming the answer

The writer is `ITurnStreamFormat`, in `Sessions/Streaming/`. The turn and the session view only ever call that. Its one
format is `Claude/ClaudeStreamFormat`. `"stream"` on `POST /v1/sessions` picks the format (`StreamFormat`, Claude by
default), and the session keeps it.

The engine's updates become `message_start`, then `content_block_start`, `delta` and `stop` (for thinking, text,
tool_use and tool_result), then `message_delta` (the stop reason and the turn's `usage`), and `message_stop`.

A failure becomes an `error` event in Claude's shape, with the turn's `usage` if a request had come back. The error's
`type` says what kind of failure it was (`StreamErrorKind`, in `LouisAgent.SessionStream`). It's `api_error` for any
failure, and `replay_miss_error` when the engine's replay had no recorded answer (`ModelReplayMissException`, mapped
by `Sessions/Streaming/StreamErrorKinds`). So a client can tell a replay miss from the agent's own failure.

`/tools`, `/approve`, `/reject` and `/usage` are answered without the model.

When the host turns the engine's `STREAM_PACE` on, the engine hands out the turn's text at a steady pace in whole
words, and the API says so: it answers the turn with the header `Stream-Paced: on` (`ApiHeaders.StreamPaced`), before
the first event. A page that reads it leaves its own pacer off, so the words are paced once. The events themselves are
the same either way, still Claude's shape; only when their text arrives changes. With the setting off there is no
header, and a client paces as it likes.

*Decided in:* F18 step 1f, F3, FX6, FX26, F156.

### 14. Usage

Each session keeps its finished turns' records (`UsageSession`), for `/usage` and for the `usage` of
`GET /v1/sessions/{id}`. The turn is added in the stream's `finally`, so a turn that disconnected or failed still
counts.

*Decided in:* F3.

### 15. Workspace and git

Every path goes through core's `WorkspaceBoundary` and `SensitiveFiles` (`Guardrails/`). So the API keeps to the same
boundary and secret-file rules as the agent itself. Git runs without a shell, with a 30-second timeout.

For history and merging (`GitCommitsEndpoints`, `GitBranchesEndpoints`), you get branches with their ahead and behind
counts, the log across branches, and commit and branch diffs, with secret files' content removed. Merges are
`--no-ff` and abort cleanly on a conflict, and merged branches can be deleted. A branch name is only accepted if the
branch exists, and it keeps its slashes in the route (`/v1/git/branches/feature/x`).

The full reference for every endpoint and event is in
[How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-http-api-louis-agentapi).

### 16. Identities, and the open door

A host runs the identities its catalogue holds. A session's request may carry `identity`, and the session opens in
that identity's default voice, unless it names a role. The replies say who answered, and `GET /v1/identities` lists
them, the default first.

A host may name some identities as open, with a cap: `OPEN_IDENTITIES` (by commas) and `OPEN_SESSIONS_PER_DAY` (a
whole number). With no identity named, or a cap that is 0 or not a number, the door stays shut. The key gate asks
`OpenDoor` first. A visitor with no key and no sign-in may then open a session with one of
those identities, a few times a day from one address, counted across them all. The request is narrowed in code to the
identity the door let in and its own toolsets, and it's tagged `open-door` on the ledger. The calls on that session
pass, and past the cap the answer is a 429.

When an admitted session's turn asks for another session (its `parent` tag), that one counts as the same visitor's and
is let in for them, so the visitor's window can carry on in it. Each admitted session remembers which visitor it counts
for, for 24 hours.

The sessions and the door's list of the ones it let in live in memory, so a restart forgets both. A visitor whose
page still holds a conversation from before it would otherwise be sent to the key's check and told to enter a key.
So the door asks the host whether it has the session, and lets a call on one it doesn't have go on to its handler.
The handler answers that the session is gone (a 404), and the page ends the chat with its own words for a restart.
A session the host does have, and the door didn't let in, still needs the key.

A session knows who it runs as (`AgentSession.Who`), and each turn sets that in the background (`TurnIdentity`, with
the user's place and the rest, in `Sessions/Streaming/TurnAmbience`). That way a pack can tell a demonstration's turn
from a real one.

*Decided in:* F109.3, F127.2, F127.4.2, FX157, [ADR 0117](../adr/0117-one-address-for-every-agent-call.md),
[ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md),
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

Here's the main route: opening a session, and asking it something.

![A session and a message to it: the key gate, the session with its shared engine, and the turn streamed back as Server-Sent Events](diagrams/LouisAgent.Api-diagrams.svg)

These are the types involved:

![The types: a program adds the API, the handlers open a session per conversation, the turn streams in the format it picks, and the open door counts the public's](diagrams/LouisAgent.Api-interface.svg)

## Structure

| File | What's in it |
|---|---|
| `ApiStartup.cs` | The calls a program makes (part 1): `AddLouisAgentApi` (the services), `UseLouisAgentKey` (the key check, FX18), `MapLouisAgentApi` (every route and the admin routes) and `MapLouisAgentHostGroup` (the `/v1` group for a host's own routes), the last two refusing to map without the key check |
| `Endpoints/Info/InfoEndpoints.cs` | `/health`, `/v1/prices`, `/v1/rates`, `/v1/rates/display`, `/v1/feeds`, `/v1/feeds/{name}`, `/v1/feeds/{name}/{id}`, `/v1/toolsets`, `/v1/model`, `/v1/roles`, `/v1/llm-profiles`: what this instance is and has |
| `Endpoints/Sessions/SessionEndpoints.cs` | `POST /v1/sessions` (`201` with `Location`; with `"stream"`, the format, refused as a 400 when unknown), `GET` and `DELETE /v1/sessions/{id}`, `GET /v1/sessions/{id}/work` (its steps from its history, without its thinking; `Sessions/Work/SessionWorkReplyFactory`, F127.4.2), `POST /v1/sessions/{id}/cancel` and `/messages` (the turn as SSE, through `TurnStream`) |
| `Endpoints/ApiErrors.cs` | The one error body every endpoint returns |
| `Site/` | A site's pages as HTML (F121): `SitePages.MapLouisAgentSite`, the one call; `SiteRoutes`, the page an address names, among the site's pages and those its areas bring through `SitePageAssemblies` (F164.1) (an address under `/admin` names none: a site's own admin page is never rendered for a search engine or listed for the guide, F142; nor does a page with `[Authorize]`, F164.1); `SitePageHandler`, the endpoint with the rendered pages it keeps; `PageShell`, the page put into `index.html`; `Rendering/`, the page rendered with `HtmlRenderer` and a `HeadOutlet` (`PagePrerenderer`, `PrerenderRoot`, `RenderedPage`, and the stand-ins for navigation and scripts); `Places/` (F127.7, ADR 0121), every section, card and piece of the site's pages, read from those renders (`SitePlaces`, rendered once when first asked; `PlaceReader` and `HtmlTag`, plain string reading; `PlaceSearch`, word matching), which `MapLouisAgentSite` hands the engines' context for the site pack's `FindPlace`, and `SitePlaceSearch`, the open route a visitor's search asks (F164.2) |
| `Endpoints/Workspace/WorkspaceEndpoints.cs` | `GET /v1/workspace/entries`, `GET /v1/workspace/file` (text ≤ 1 MB; secret files listed but not readable) |
| `Endpoints/Git/Changes/GitChangesEndpoints.cs` | `GET /v1/git/status`, `GET /v1/git/diff`, `POST /v1/git/stage` · `unstage` · `discard`: the working tree and the index |
| `Endpoints/Git/Commits/GitCommitsEndpoints.cs` | `POST /v1/git/commits` (`201` with `Location`), `GET /v1/git/commits?count=` (the log across branches), `GET /v1/git/commits/{sha}` |
| `Endpoints/Git/Branches/GitBranchesEndpoints.cs` | `GET /v1/git/branches`, `GET` and `DELETE /v1/git/branches/{name}`, `POST /v1/git/merge` |
| `Endpoints/Git/GitProcess.cs`, `Output/` | The git runner (no shell, 30 seconds at most); git's output parsed (`GitOutputParser` and its records) |
| `Endpoints/Routing/` | `ApiMap` (the services and every route, the `/v1` group, the JSON 404, and `MapHostGroup`, the same group handed to a host for routes of its own through `MapLouisAgentHostGroup`, F142), `RouteAccess` and `WithAccess` (who may call a route), `AgentKeyGate` (the key check) |
| `Endpoints/Routing/OpenDoor.cs`, `Config/OpenDoorOptions.cs` | The open door (part 16): who it lets in without a key, counted per address per day, and its two settings |
| `Endpoints/Answers/CreatedJson.cs` | A `201` with its `Location` and its JSON body |
| `Endpoints/*/…Handlers.cs` | Each route's named handler beside its `…Endpoints.cs` (sessions, info, admin) |
| `Messages/` | The API's own texts as enums (ADR 0009): errors, log lines, what the model reads (`ApiModelText`), what the user reads, and their holes |
| `Sessions/SessionOpener.cs` | Who opened a session when a signed-in person did: their id and tenant, read from the cookie's claims; a session a key opened has none (ADR 0088) |
| `Sessions/Prompts/` | `TurnPrompt`, the message a turn sends the model, and `PageBlock`, the page the visitor is reading, cut at 6,000 characters |
| `Sessions/AgentSession.cs`, `AgentSessions.cs` | One conversation (id, engine, toolsets, depth, role, place, attachments, history, turn lock, cancellation) and the in-memory session store, which picks each session's engine |
| `Sessions/Streaming/` | The stream formats (F18 step 1f): `ITurnStreamFormat`, the writer's seam; `StreamFormat`, the enum `"stream"` names; `StreamFormats`, which makes a turn's format; `TurnUsage`, a turn's usage computed once from the ledger; `Claude/ClaudeStreamFormat.cs`, the Claude-style events |
| `Sessions/TurnStream.cs` | A turn as the API streams it: the prompt from the message, its attachments and the page the visitor is reading (F115, ADR 0104, one block cut at 6,000 characters), the engine's updates as Claude-style events, a sub-session's events live among them, the usage at the end, and (F127.8) the turn's line in the turn log |
| `Sessions/Log/` | The turn log (F127.8): `TurnLogRecord`, one finished turn (when, the session and turn, who answered, the page and the part of the site, the tools it called, whether the admin's own browser had it, never what was said), and `TurnLog`, which appends it to `turns-{yyyy-MM}.jsonl` beside the ledger and reads a month back; nothing without a log folder or with the ledger off |
| `Endpoints/Admin/Overview/` | `GET /v1/admin/overview?day=` behind the admin key (F127.8): `OverviewReader` joins the month's turn log and ledger by session into `OverviewReply` (today and the month, the talks by part and by who answered, the latest twenty), costs as the ledger charged them, an unpriced one null, a talk marked the admin's when any of its turns was; `OverviewHandlers` reads the two files |
| `Endpoints/Admin/Sessions/` | `POST /v1/admin/sessions/{id}/mine` behind the admin key (FX207): the admin's own browser says a session it opened is theirs, and `AgentSession.FromTheAdmin` carries that into the turn log, so the overview can tell the admin's talks from visitors'; an unknown session is a 404 |
| `Sessions/SessionReplyFactories.cs`, `SessionCreatedReplyFactory.cs`, `Sessions/Requests/` | A session as the API shows it; how it reads the request bodies, which are shared records in `LouisAgent.SessionStream`'s `Wire/` since F127.3.1 (`CreateSessionRequestReading`: `CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `PageRequest`, `ReplayRequest`), and what `replay` resolves to (`ReplayResolution`, F18 step 4a) |
| `Admin/AdminGate.cs`, `AdminAccess.cs` | The gate on `/v1/admin` (F27): the admin key, else the agent's, else closed; what it decided |
| `Endpoints/Admin/Imports/ImportHandlers.cs`, `ImportEndpoints.cs` | The packs' uploads (F55, ADR 0071), behind the admin key: `GET /v1/admin/imports` lists each with its files, `POST …/{name}/preview` and `…/save` hand the files to the pack and return its report, all in the contract's `Admin/Imports/ImportRows.cs` |
| `Endpoints/Admin/Screens/` | The packs' described screens (ADR 0086), behind the admin key, naming no pack: `ScreenHandlers` the list and a page of rows, `ScreenRowHandlers` a row's form, `ScreenActionHandlers` an action |
| `Endpoints/Admin/Prompts/PromptsHandlers.cs` | `GET /v1/admin/prompts` (CL27, ADR 0063), behind the admin key: every built-in prompt from core's `PromptCatalog` as its header says it, a `PromptRow` each (path, name, description, level by its wire name or null, the toolsets it always loads) in the contract's `Admin/Prompts/PromptRows.cs` |
| `Config/SiteAddressOptions.cs` | Where a site lives, `SITE_ADDRESS` (FX216): every canonical link's host, and where a page on another host is sent; only a whole https address counts |
| `Config/AdminOptions.cs` | The admin key, `ADMIN_API_KEY` else `AGENT_API_KEY`; neither closes the admin routes. The studio's settings are `EvalOptions` in LouisAgent.Api.Evals (CL57) |
| `Endpoints/Admin/AdminGroup.cs`, `AdminGateFilter.cs`, `AdminEndpoints.cs` | The gated `/admin` group the API hands out (CL57): one filter that refuses without the key, a group per library that maps on it; the API's own admin routes are the overview, the prompts, the imports and the screens, and the eval routes are `LouisAgent.Api.Evals`'s ([its doc](LouisAgent.Api.Evals.md)) |

## Configuration

The API runs inside the program that adds it, one container per demo, reached on the PC's own address.

![Where the API runs: in the demo's program in its container, reached by a browser on 127.0.0.1, reaching a model, the collector and its mounted folders](diagrams/LouisAgent.Api-deployment.svg)

It uses core's settings, including `WEB_REPLAY` and `REPLAY_DIRECTORY`, which it reads through `ReplayResolution` and
`AgentOptions.KeepsRecordings`. On top of those:

`AGENT_API_KEY` is required on every `/v1` route except the open ones: the prices, the rates and their display, the
feeds and a site's search of its places. Without it, the API
refuses to start, unless `AGENT_AUTH=off`. `API_PORT` is the host port in Docker, bound to `127.0.0.1`.
`GIT_AUTHOR_*` and `GIT_COMMITTER_*`, set in `.env.secrets`, are who commits and merges from the web app are made
as. Outside Docker,
`ASPNETCORE_URLS` and `ASPNETCORE_ENVIRONMENT` matter too: an unpublished `dotnet run` only serves the web app in
`Development`.

The admin routes use `ADMIN_API_KEY`, or `AGENT_API_KEY` when that's not set. The eval studio's settings are in
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md). [SETUP](../SETUP.md) has the full list.

The images carry the eval runner. Both Dockerfiles (`demos/louis-agent/docker/images/Dockerfile.api` and
`demos/puls/docker/images/Dockerfile.puls`) publish it in the build stage and copy it to `/app/evals`, its own folder
beside the API's. Each image sets `EVAL_RUNNER` to its runner there, such as
`/app/evals/LouisAgent.CodingEvalRunner.dll`, and compose sets none of its own.

*Decided in:* FX18, F18 step 4.3b.

## Extending it

The steps are in [Change the API](../guides/change-the-api.md). In short: a new endpoint goes in its concern's
`*Endpoints.cs`, with a `Map…` extension, mapped from `ApiMap.MapApi` under `/v1`. It needs the key unless it's marked
open on purpose, and the route golden is written again. Use `GitProcess.RunAsync` for git, and `ApiErrors.Result` for
errors, in Claude's error shape.

A new session option goes in `CreateSessionRequest`. New stream content goes through `ITurnStreamFormat` (its Claude
format is in `Sessions/Streaming/Claude`), so every client sees the same event shapes; new events are added, never
changed. And anything that changes how the agent behaves belongs in core, not here.

## Tests

The tests are in `src/tests/hosts/LouisAgent.Api.Tests`. They cover the sessions' toolsets, the default, unknown
names, depth and the `specialists` rule, shared engines with separate histories, roles, a role opening only at its own
level, and a sub-session with no role getting none. The model client in these tests is never called. The eval
studio's tests are in `LouisAgent.Api.Evals.Tests`.

For the routes, `ApiRoutesSnapshot.json` pins every route, its method and who may call it, and `ApiRouteRuleTests`
checks ADR 0039's rule on that table. The in-memory host, `ApiTestHost`, runs the key gate (`AgentKeyGateTests`), a
session's `201` (`SessionCreatedTests`), and the git resources in a temporary repository (`GitResourceTests`).

For the site's pages, `PagePrerendererTests` draw a sample page, and `PageShellTests` fill the code-review site's real
`index.html`. `SitePagesTests` ask the test server for a page, a section, a page with no description, one that throws,
and an address with no page. They check that a section's canonical link names its page, that with no site address it
comes from the request's own, and that with `SITE_ADDRESS` a page on another host is sent there while a file and an
API route are not. `SiteAddressOptionsTests` hold which settings count as an address. `SitePlaceSearchTests`
hold the search: a word finds its places, a word too short finds none, and the route is open. `PlaceReaderTests` hold
how a place's words are read.

A streamed turn is still checked by hand, in the browser.

*Decided in:* CL20, F121.

## Limits and plans

Sessions are kept in memory; keeping them is the [session architecture](../specs/SESSION_ARCHITECTURE.md).

A usage API over the ledger (`/v1/usage` endpoints, `/v1/budgets`, `/v1/estimates`) and budget errors are
[F7](../features/F07-budgets.md), which takes in [F11](../features/F11-usage-tab-and-reconciliation.md).
Account-limit errors are [F8](../features/F08-reliability.md), and settings per role and per session are
[F10](../features/F10-route-settings.md).

## Related docs

[How the agents and endpoints talk](../AGENT_COMMUNICATION.md) · [LouisAgent.CodeReview](../../../demos/louis-agent/docs/projects/LouisAgent.CodeReview.md) ·
[Architecture § HTTP API](../architecture/05-building-block-view.md#http-api-and-web-app)

---
[Projects](README.md) · Previous: [LouisAgent.Acp](LouisAgent.Acp.md) · Next: [LouisAgent.CodeReview](../../../demos/louis-agent/docs/projects/LouisAgent.CodeReview.md)
