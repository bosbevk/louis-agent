# 0039. The agent API's routes follow one rule, under /v1

**Status:** Accepted · **Date:** 2026-10-25 (Louis: "we should conform", and "also pin this rule") ·
**Recorded:** 2026-10-25

## Context

`louis-agent.api` grew a route at a time: sessions (F5), rates (F14), the admin portal (F27, F31), the LLM profiles
(F30). A survey of `main` on 2026-10-25 counted 37 routes and found these gaps:

- none was versioned;
- a verb sat in a path (`POST /git/branch/delete`);
- one resource went by several names (`/git/branch?name=` beside `/git/branches`, `/git/commit` for both create and
  read, and `/git/log` for the list);
- the key check listed the routes that need the key, so a route left off the list was open (`GET /llm-profiles`
  was);
- the web app's pages and the API shared paths (`/admin/benchmarks`);
- an unknown path got the web app's page;
- nothing tested the table.

The eval harness's HTTP client (F18 step 5), the engine package's `Hosts.Api` and the Puls gateway (Phase 9) will be
built against this API, so its shape should settle before them.

The options for the version were a header (`Accept: application/vnd…+json; v=1`) or the path. A header is invisible
in logs, links and the browser's network tab, and awkward for an `EventSource`. A path prefix shows in all of them, and
it separates the API from the pages.

## Decision

- **We put every route of `louis-agent.api` under `/v1`, except `/health`.** `/health` is the probe Docker and Azure
  call, for any version. A breaking change to a route's shape gets a new version beside the old one, never a change
  in place.
- **Resources are nouns, and a collection and its items share one plural name** (`GET /v1/git/branches`,
  `GET /v1/git/branches/{name}`, `DELETE /v1/git/branches/{name}`).
- **A create is a POST to the collection, and it answers `201 Created` with a `Location` header.** A create whose work
  goes on after the answer answers `202 Accepted` with the same header. A POST that streams its answer answers 200 with
  the stream.
- **A delete is `DELETE` on the resource.** An action that no HTTP method names is `POST /{resource}/{verb}`
  (`POST /v1/sessions/{id}/cancel`, `POST /v1/git/stage`). A `GET` never changes anything.
- **Every `/v1` route needs the API key, unless the code marks it open.** Today that is the prices, the rates and their
  display. The admin routes keep their own key. An unknown `/v1` path is closed, and with the key it answers a JSON 404.
- **A client names the whole route, version included** (`v1/sessions`). The URL it is configured with names only the
  host.
- **A test pins the table:** every route, its method and who may call it, in a golden file, with these rules checked on
  the real table.

## Consequences

- Every client changes at once (the two web apps, the orchestrator, core's peer sessions, the Python harness), and the
  hosts deploy together. An old client meets 404s, because no unversioned alias is kept. CL14's `GET /personalities`
  goes too, since no client calls it under `/v1`.
- A setting that holds a full URL to this API (`PRICES_URL`, `RATES_URL`) gains `/v1`.
- A page reload on the web app never reaches the API, and a mistyped API path is an error, not a page.
- A wrong method on a known `/v1` route answers 404 rather than 405, because the `/v1` fallback accepts every method.
- Not decided here:
  - an OpenAPI document, for the engine package's `Hosts.Api`;
  - rate limits (F42, and the Puls gateway);
  - the MCP server's endpoint, which follows MCP's spec.
- `Asp.Versioning.Http` was considered and not taken: a route group is the whole need while one version runs. Its
  URL-segment style is this same `/v1`, so it can be added later without changing a URL.

## Links

- [CL20 API standards](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL20-api-standards.md): the survey, the routes before and after, the steps.
- [FX18 Auth fails closed](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md): the key's start-up rule, which this keeps.
- [0009](0009-message-text-as-enum-templates.md): the error texts stay message enums.
