# 11 · Protocols: an API is a contract

> **Skill:** #11 in the [goal](../GOAL.md)'s map, with #15 (deny by default) and #18 (a pinned surface) · **Stages:**
> CL20 API standards, steps 0 to 6 (2026-10-25) · **Written by** Claude, for Louis to read (2026-10-25) · **Next:** an
> OpenAPI document for the engine package's `Hosts.Api`; someone else's MCP server as a pack (F36, Phase 8)

What this answers: why the agent API's routes changed shape before the evals' client and the engine package are built
on them, what the rule is, and how a change to a public surface is proved.

## API versioning and the route rule

### Before and after

| | Before (`main`, 2026-10-25) | After (CL20) |
|---|---|---|
| The version | None: a route's path was its only name | Every route under `/v1` but `/health` |
| The names | A verb in a path (`POST /git/branch/delete`); one resource under four names (`/git/branch?name=`, `/git/branches`, `/git/commit` to create and to read, `/git/log`) | Nouns, one plural per collection: `/v1/git/commits`, `/v1/git/commits/{sha}`, `/v1/git/branches/{name}` |
| A create | `200` and a body | `201 Created` with a `Location` header (a session, a commit) |
| A delete | `POST …/delete` | `DELETE /v1/git/branches/{name}` |
| The key | A list of the prefixes that need it; `GET /llm-profiles` (F30) wasn't on it, so it was open | Every `/v1` route needs it unless the code marks it open (the prices and the rates) |
| An unknown path | The web app's page, `200` | A JSON `404` with the key, a `401` without |
| The proof | Nothing tested the table | A golden file of all 37 routes, and the rule run as tests on it |

### Why a version, and why in the path

- **An API is a contract with code you don't deploy.** The evals' HTTP client (F18 step 5), the engine package's
  `Hosts.Api` and the Puls gateway (Phase 9) will be built against this API. A shape changed after them breaks code
  that someone else owns. A version lets a breaking change sit beside the old shape (`/v2`) instead of replacing it.
- **The path, not a header.** A header (`Accept: application/vnd…+json; v=1`) is invisible in logs, in links and in
  the browser's network tab, and a browser's `EventSource` can't set one. The path shows everywhere. It also separates
  the API from the web app's pages, which share the host: `/admin/benchmarks` was both a page and an API route.
- **No library yet.** `Asp.Versioning.Http` was considered and not taken. One route group (`app.MapGroup("/v1")`) is
  the whole need while one version runs, and the library's URL-segment style is this same `/v1`, so adding it later
  changes no URL.

### Why `v1/` is in the code, not in a setting

Louis asked whether the version should come from settings. It doesn't, on purpose:

- **The version is part of the contract, not of the deployment.** A client written for `/v1`'s shapes can't talk to
  `/v2` by changing a setting: its code has to change too. A setting would let the two drift apart without a test
  noticing.
- **The base address is shared.** The web apps' `HttpClient` also loads static files (`css/themes/themes.json`), and
  `/health` has no version, so a base address ending in `/v1` would break both.
- **One place on each side.** The server writes it once (`ApiMap.Version`). Each client writes the whole route
  (`"v1/sessions"`), so `grep "v1/sessions"` finds the server and every caller.
- **What is a setting is where the API is:** the host (`AGENT_PEERS`, `BENCHMARK_API_URL`, the web app's address). A
  setting that holds a full URL to one route (`PRICES_URL`, `RATES_URL`) gains `/v1`, because it names a route.

### Nouns and status codes

- **The method is the verb.** `GET`, `POST` and `DELETE` already say read, create and remove. A path with `delete` in it
  says it twice, and it makes a `GET …/delete` possible.
- **One plural name for a collection and its items.** `/git/branches` lists the branches, `/git/branches/{name}` is one
  of them. A branch name keeps its slashes (`feature/x`), so the route catches the rest of the path (`{**name}`) and the
  client escapes each segment on its own.
- **A create answers `201` and says where the new thing lives**, in `Location`, so a client never builds that URL
  itself. A create whose work goes on after the answer answers `202` (an admin run, when F18 renames those routes).
- **An action that no method names** (`cancel`, `stage`, `merge`) is a `POST` with the verb last:
  `POST /v1/sessions/{id}/cancel`.
- **404 versus 405.** A wrong method on a known route should be `405 Method Not Allowed`. Here it is a `404`, because
  the `/v1` fallback that turns an unknown path into a JSON error matches every method. ADR 0039 records the trade: a
  JSON error for every mistyped path, at the cost of a less precise code for a wrong method.

### Deny by default

The old key check listed the prefixes that need the key, so a route under a new prefix was open until someone
remembered the list. One was forgotten: F30's `GET /llm-profiles` was open. Now each route carries a mark
(`RouteAccess`: `Key`, `Open` or `AdminKey`), and the gate reads the mark of the route the request matched. `Key` is
the enum's first member, so a route nobody marked is closed. An unknown `/v1` path is closed too: `401`
without the key, and the `404` only with it, so nobody can map the table without the key. It is Phase 1's rule (FX18,
write-up [15](15-safety-and-security.md)) applied to routes: fail closed, and open on purpose.

### A pinned surface

- **The table as a golden file.** `ApiRoutesSnapshot.json` lists every route as `METHOD /pattern (Access)`, sorted.
  The tests build the table with the same `ApiMap.MapApi` that `Program.cs` calls, and one test fails if `Program.cs`
  maps a route of its own, so the golden is the table that runs.
- **The rule as tests on the real table.** `ApiRouteRuleTests` checks ADR 0039 on it: every route under `/v1` but
  `/health`, the open and the admin routes, no verb as a segment, an action word only last in a `POST`, and no
  resource under both its singular and its plural.
- **The gate tried on every route.** `AgentKeyGateTests` walks the same table and calls every `Key` route without the
  key, so a new route is checked without anyone writing a test for it.

### What was measured, and what proved it

| Step | What changed | What proved it |
|---|---|---|
| 1 | One map, `ApiMap` | The golden held exactly the survey's 37 routes |
| 2 | `/v1`, and the key by default | The golden's diff: every route under `/v1`, `GET /v1/llm-profiles` now `Key`, the fallback added, `/personalities` gone (still 37) |
| 3 | Every client calls `v1/` | A grep found 44 request sites on `main` and none left; the commit was byte for byte the dry run |
| 4 | The git endpoints split by resource | The golden byte-equal: a refactor that changed no route |
| 5 | Nouns, `201` and `Location` | The golden's diff: five routes out and five in, still 37 |
| 6 | The docs and the prompts | The goldens of what the model reads changed only by `/v1` |

## Trade-offs accepted

- **Every client changed at once, and the hosts deploy together.** No unversioned alias was kept. An old client's
  `GET` outside `/v1` isn't a 404: the web app's fallback (`MapFallbackToFile`, which keeps its links working on a
  reload) answers it with its page and a `200`, as the by-hand check of `GET /sessions/nope` showed. A `PRICES_URL`
  without `/v1` gets that page, and the host stops at start-up on a price table that isn't one, which is loud. An
  alias is a second table to keep: CL14's aliases are still not all gone, a deploy later.
- **A wrong method answers 404, not 405** (above).
- **The docs of the past keep their routes.** Plans, ADRs, fix and feature docs and the CHANGELOG record what was true
  when they were written; only the living docs moved to `/v1`.

## What didn't work (and what it taught)

- **A list a person has to remember is a hole in waiting.** The prefix list was right when it was written, and wrong
  by F30. Marks on the routes, closed when missing, can't be forgotten that way.
- **A page and an API route under one path.** `/admin/benchmarks` was both, so which one a request reached depended on
  how it was made. The version prefix separated them. The trap that stays: a page's `href="admin/benchmarks"` is a page,
  not a call, and step 3 had to leave it alone.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL20-api-standards.md) ·
[ADR 0039](../adr/0039-api-routes-under-v1.md)
