# F27 · Admin portal: plan

> Approved 2026-10-15 (Louis: in-house template, "use what you think will fit") · Feature: [F27 Admin portal](../features/F27-admin-portal.md) ·
> Branch: `claude/project-thread-381vmw`, with F25 and F26 (it runs the cook check, which lives here)

## Context

Louis (2026-10-15): "can you add an admin page where we can benchmark tools? … have an admin menu … navigate to an
admin portal where we can run benchmarks, but this will grow so look up admin management screens and choose one which
will fit and implement." The app is Blazor WebAssembly with Bootstrap 5.3 from a CDN and Puls's own `app.css` (F20:
dark, Puls's colours, Montserrat).

## Design

### The template: our own, with the admin kits as reference

Louis (2026-10-15): "we are building our own template for this, we use this only for reference, to get examples; we
will build this in-house, use what you think will fit." Three kits were compared (AdminLTE 4, Tabler 1.4, MudBlazor);
none is loaded. The layout follows the pattern every one of them uses, because it is what people expect of a
management screen, built with Puls's own CSS variables (F20) on the Bootstrap 5.3 already loaded:

- **A fixed left sidebar** (the brand, then sections as a vertical list with an icon and a label, the active one
  lit) and a **content column** with a page header (title, one line of help, the actions on the right) over cards.
  Below the `lg` breakpoint the sidebar is an offcanvas, as the chat's is (the pattern AdminLTE and Tabler share).
- **Cards for every block** (a list, a table, a form), the way the chat's Werk panel already draws its items, so the
  portal is Puls, not a second product. Tables are Bootstrap's `table` with Puls's colours; status as the chips the
  Werk panel uses (`toolset-chip`, `work-status`).
- **Reference, not copy:** AdminLTE's "sidebar + content header + cards" page anatomy and Tabler's page-header with
  actions are the two examples followed; their class names and files are not used.

### The pieces

- **Web app (Puls):** `Layout/AdminLayout.razor` (the sidebar and content column above, in `app.css` under an
  `/* Admin */` section), `Pages/Admin/Benchmarks.razor` at `/admin` and `/admin/benchmarks`, an *Admin* item in the
  profile menu. No new stylesheet or script: the chat pages pay nothing.
- **API:** `Endpoints/AdminEndpoints.cs`: `GET /admin/benchmarks` (the checks and the saved runs, from `samples/`),
  `POST /admin/benchmarks/{check}/runs` (`{ "repeat": n }`; 409 while one runs), `GET /admin/benchmarks/runs/{id}/events`
  (SSE: the script's lines, then `done` with the folder), `GET /admin/benchmarks/runs/{folder}` (its `results.json`,
  `summary.json`, `README.md`). All behind `ADMIN_API_KEY` (falls back to `AGENT_API_KEY`); 403 with neither.
- **Runner:** `Services/BenchmarkRunner.cs`: one process at a time, `python3` with an argument list (never a command
  string), `PULS_API=http://127.0.0.1:8080/`, cwd `/workspace`, output `samples/sample-output-{check}-{stamp}/`.
  Python 3 is already in the image (`docker/Dockerfile.puls` installs it for the `python` toolset).
- **Texts** in the API's `ApiUserText` and Puls's text class; Afrikaans on the page as the rest of Puls.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | API: the admin gate and `GET /admin/benchmarks` listing the saved runs | API tests (403 without a key; the list from a temp `samples/`) |
| 2 | Web: the *Admin* menu item, `AdminLayout` (our sidebar and content column), the Benchmarks page listing runs and opening one | by hand (no web test project) |
| 3 | API: `BenchmarkRunner` and the run endpoints with SSE progress | API tests with a fake script (a one-line Python file); 409 while running |
| 4 | Web: *Run* with repeats, live progress, the result table; a real run by hand | **paid: Louis's go** |

## Verification

`dotnet test` for API per step; the portal by hand against the Puls containers (rebuilt: the API and the image change).
A real run from the page spends money and is Louis's go.

## Concepts

- **Evaluation as a product surface** (GOAL #12): a benchmark that anyone on the team can run and read, the step
  before regression gates in CI.
- **Arguments as data** (GOAL #15, ADR 0004): a script launched with an argument list and environment, never a shell
  string built from a request.
- **Progressive disclosure in a UI** (GOAL #4's cousin): a portal whose first page is one thing, with a place for the next.

## Approved during the build

- **2026-10-15, the run's progress as a table** (Louis, watching the first run from the page: "this can be made
  bigger and be more detailed"): step 5. The cook check prints structured progress (`@` + JSON: a mode's start, each
  turn with its role or continued handle, call counts, hosts and seconds, a summary per conversation, the mode's cost)
  beside its plain lines; the page renders those as a table and folds the plain lines below.
- **2026-10-15, the run's conversations live, then as a tree** (Louis: "use all the space and show me as if I'm
  typing it"; then "merge this view, a nice modern treeview-like structure"): steps 6 and 7. The script forwards every
  stream event of each turn; the page builds a tree, mode → conversation → turn, with the figures as chips on each
  node and the chat's own TurnView inside a turn, so the conversation types out as it happens.
- **2026-10-15, charts you drill into, with motion, and the replay** (Louis: "this does not work, we need to
  visualise this; drill down to the lowest level of all data", "we do not even have graphs", "fancy animations, take
  inspiration from the landing page, do research before starting", "the whole chat transcript as it was typed"): steps
  8 to 10. Researched first: the dataviz method (the form by the data's job; the reference palette's dark column in its
  validated order for the token kinds, since the validator's Node isn't on this machine; thin marks, a surface gap, a
  legend for two or more series, tooltips that never gate), the landing page's motion, and drill-down conventions (the
  child level unfolds from the clicked point, a breadcrumb back). Built in-house as HTML and CSS, no chart package
  (Chart.js and ECharts were the alternatives; a package decision for a later chart that needs an axis system). The
  checks save each mode's events with their time; the API serves them on demand; the turn replays through the chat's
  own components.
- **2026-10-15, the smaller asks along the way** (Louis, from screenshots): costs in the browser's currency; the saved
  runs as a list in a side column; repeats as a segmented control instead of a native select; the check cards roll
  up and down; repeats per check; a saved run opens its transcripts; the finished run's report rendered as markdown.
