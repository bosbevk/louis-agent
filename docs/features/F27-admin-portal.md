# F27 · Admin portal: a place to run and read the benchmarks, built to grow

> **Status:** built (2026-10-15), on `claude/project-thread-381vmw`; open: a real run from the page (Louis's go) · **Milestone:** — · **Depends on:** F20 (Puls's app and look), F25 (the cook check), F1–F3 (the ledger the checks read)
>
> **In the TODO:** **F27 Admin portal** under *Web app* ([TODO](../../TODO.md)) · **Plan:** [plans/F27](../plans/F27-admin-portal.md)

The checks that measure Puls (the delegation check, the cook check) are Python scripts run from a terminal on the
host, with the results read from `samples/` folders. Louis wants them a click away: an *Admin* entry in the profile
menu that opens an admin portal, whose first page runs a benchmark and shows its results, and which will take more
pages over time (the ledger, prices, skills, the roles). So the portal needs a layout meant for management screens,
not the chat's three panels.

## User stories

**F27-S1 — An admin portal.** As *Louis* (the operator), I want an *Admin* item in the profile menu that opens a
portal with its own navigation, so that management pages have a home that doesn't crowd the chat.
- *Given* the profile menu *then* it has *Admin*, which opens `/admin` in the same app, with a sidebar of sections
  (Benchmarks first) and the Puls look (dark, Puls's colours).
- *Given* no admin key is configured on the API *then* the portal says so and shows nothing: it is never open by
  accident.

**F27-S2 — Run a benchmark.** As *Louis*, I want to pick a check (the cook check, the delegation check), choose how
many repeats, and run it from the page, so that a change can be measured without a terminal.
- *Given* *Run* *then* the run starts on the server, the page shows its progress as the check prints it (each
  conversation's role, hosts and seconds), and the results table when it ends, with the cost.
- *Given* a run in progress *then* a second *Run* is refused until it ends (one at a time: the containers are shared).

**F27-S3 — Read past runs.** As *Louis*, I want every saved run listed with its date, cost and summary, so that runs
can be compared over time.
- *Given* the Benchmarks page *then* every `samples/sample-output-*` folder with a `results.json` or `summary.json`
  is listed, newest first, and opens to its tables and transcripts.

## Design notes

- **The template is our own.** Three admin kits were compared as reference (AdminLTE 4, Tabler, MudBlazor; the plan
  has them); none is loaded. The portal follows the page anatomy they share, a fixed sidebar of sections and a content
  column of a page header over cards, built with Puls's own CSS variables (F20) on the Bootstrap 5.3 already loaded,
  so it looks like Puls and the chat's pages pay nothing.
- **Access.** Admin endpoints on the API (`/admin/…`) need `ADMIN_API_KEY` (falls back to `AGENT_API_KEY`), sent as
  `x-api-key` from the key saved in *Instellings*. With neither key set the endpoints answer 403 and the portal shows
  why. Accounts and roles are the backlog's; this is the gate until then.
- **Running a check.** The API runs the check's own script (`samples/puls-cook-check/run_check.py` and the delegation
  check's) as a child process, arguments as a list, with `PULS_API` pointing at itself and the output folder under
  `samples/` with a timestamp, so a run never overwrites a saved sample. Progress is the script's stdout, streamed as
  SSE; the result is the folder's `results.json`. Python is already in the image for the `python` toolset, and the
  scripts stay the one source of truth: nothing is ported.
- **Grows by adding a page:** a section in the sidebar and a page under `/admin/{section}`; the API gets its
  endpoints under `/admin/`.

## Done when

- **F27-S1:** the menu item, the portal shell at `/admin` with the Benchmarks section, 403 and the message without a
  key. *Met by:* API tests for the gate; the page by hand.
- **F27-S2:** a run started from the page shows progress and ends with the results and cost; a second run is refused
  while one runs. *Met by:* API tests with a fake script; a real run by hand (paid, Louis's go).
- **F27-S3:** the saved runs listed and readable. *Met by:* an API test on a sample folder; the page by hand.
- Suites pass; *Finishing a feature*.

## What the build changed

- **The template is ours** (Louis, during the build): the layout reuses the chat's sidebar, links, offcanvas and chips,
  with one `/* Admin portal */` section of styles; no admin kit is loaded.
- **The key is entered on the page itself** when the API answers 401 or 403, saved where *Instellings* saves the API
  key, so the portal can be opened without going back to the chat.
- **A reloaded page resumes a run:** `GET /admin/benchmarks/runs/current` and a late listener gets the lines so far.
- **Runs write to `samples/sample-output-{check}-{stamp}/`,** so the committed samples are never overwritten; the
  listing shows them side by side with the saved ones, matched to their check by folder prefix.
- **Python is the API container's,** already there for the `python` toolset; `BENCHMARK_API_URL` says where the script
  reaches the API back (the container's own port by default).
- **Not built:** the delegation check's `--repeat` output layout differs from the cook check's (it has no
  `summary.json` for a single run); the listing reads either file.

---
[Features](README.md) · [Plan](../plans/F27-admin-portal.md)
