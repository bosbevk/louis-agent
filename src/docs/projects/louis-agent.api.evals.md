# louis-agent.api.evals — design

> **Kind:** host (a library, `net10.0`) · **Path:** `src/hosts/louis-agent.api.evals/` · **References:** louis-agent.api,
> louis-agent.core, louis-agent.evals.protocol, louis-agent.evals.checks, MongoDB.Driver · **Used by:** a program that
> wants the eval studio: the Puls API, the coding API, the starter API
> **Also shows:** `component`

![The eval studio, and its parts: the runner, the stores and the admin routes](diagrams/louis-agent.api.evals.svg)

## Purpose

This is the eval studio, on the API. It holds everything a site needs to run evals and look at their results: the
runner process, the MongoDB stores for runs, grades, drafts and judge checks, the recorders, the reports, and every
admin route for them.

It's its own project because it's big, and because it brings MongoDB, the checks and the protocol along with it. A
program that wants the studio opts in with `app.MapLouisAgentEvals(host)`, after `MapLouisAgentApi`. A program that
doesn't ships none of it.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## How it works

Everything starts from one call, `EvalsStartup.MapLouisAgentEvals(app, host)`. It does three things.

### 1. Getting ready

It reads `EvalOptions`: where the runner is, where the store is, and the address the runner calls back on. It lists
the runner's evals, but off the start-up path, so the API doesn't wait for it.

When `EVALS_MONGO_URL` is set, it opens the Mongo stores. Without it there are no stores, so no run can start, and
the pages say so. In the background it tidies what an older build stored, and closes any runs a restart cut off.

### 2. The routes

It maps the studio's routes on the gated `/admin` group the API hands out (`AdminGroup.MapAdminGroup`, behind
`ADMIN_API_KEY`), through `EvalAdminEndpoints.MapEvalAdmin`. They live under `/evals`, and under the old name
`/benchmarks` too.

There's a route for nearly everything: the evals and benchmarks lists, a check's manual, the stored runs, reviews,
notes, rescoring, calibrations, drafts, the studio, starting a run, a run's events and requests, and the trends.

Two of them are worth a closer look. `GET runs/{id}/requests` gives the ledger's rows for a run this recorder kept,
each on its turn, so the live table can price a turn the moment it ends. It answers `404` for a run that wasn't
recorded here, and `503` if the ledger is being written mid-read.

`POST runs/{id}/rescore` starts the runner again on a stored run's export. It answers `201` with the new run's
`Location`. It answers `404` for an unknown run, `400` for a run that isn't an eval's, `409` while another run is
running, and `503` when there's no store.

*Decided in:* FX34.

### 3. Remembering what a run ran with

Each run is stored with what it ran with (`EvalSettings`). The web it used is named by core's `WebSetup` seam,
never by the web library itself.

### What stays in the API

The API keeps the admin gate, the key, its own admin routes (prompts, imports, screens) and the contracts. The
studio's replies are built here, in `Replies/`, with the `Of` factories. Its texts are in `Messages/`:
`EvalApiLogMessage`, `EvalApiUserText`, and the draft, listing, note and review texts.

![The eval studio on the API: what a program maps, and what the studio holds](diagrams/louis-agent.api.evals-diagrams.svg)

![Starting a run: the gate lets the browser in, the studio stores a run row and answers 202, and the page reads the runner's events](diagrams/louis-agent.api.evals-diagrams-2.svg)

These are the types involved:

![The types: one call maps the studio, the runner runs as its own process, and runs, drafts, reviews and calibrations each sit behind a store](diagrams/louis-agent.api.evals-interface.svg)

## Structure

```
src/hosts/louis-agent.api.evals/
├── EvalsStartup.cs        # MapLouisAgentEvals
├── Config/EvalOptions.cs  # BENCHMARK_API_URL, EVALS_MONGO_URL, EVALS_MONGO_DATABASE, EVAL_RUNNER
├── Endpoints/             # Runs/ (EvalAdminEndpoints, AdminHandlers, stored runs, rescore), Drafts/, Evals/, Reviews/, Studio/
├── Replies/               # the contracts' Of factories
├── Messages/              # EvalApiLogMessage, EvalApiUserText, DraftMessage, EvalListingMessage, NoteUserText, ReviewUserText
├── Calibrations/ Drafts/ Export/ List/ Reports/ Reviews/ Runs/ Store/ Studio/   # the studio's own parts
src/tests/hosts/louis-agent.api.evals.tests/   # the studio's tests, beside the API's; EvalRoutesSnapshot.json pins the routes
```

## Configuration

The studio runs inside a demo's API program. The eval runner is a child process in the same container.

![Where the eval studio runs: in the API's container, starting the runner as a child process and keeping runs in MongoDB](diagrams/louis-agent.api.evals-deployment.svg)

Its settings are `EVALS_MONGO_URL`, `EVALS_MONGO_DATABASE`, `EVAL_RUNNER` and `BENCHMARK_API_URL`. The key it's
behind is the API's own `ADMIN_API_KEY`.

## Tests

The tests are in `src/tests/hosts/louis-agent.api.evals.tests`. They cover the stores, the recorder, the reports
and trends, the drafts and the studio, and the reviews and calibrations. Each handler runs through the in-memory test
host, `ApiTestHost`, which maps the studio for these tests and leaves it out for the API's own route snapshot.
`EvalRoutesSnapshotTests` pins the studio's routes.

## Related docs

[louis-agent.api](louis-agent.api.md) · [louis-agent.web.evals](louis-agent.web.evals.md) ·
[ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md) · [ADR 0039](../adr/0039-api-routes-under-v1.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
