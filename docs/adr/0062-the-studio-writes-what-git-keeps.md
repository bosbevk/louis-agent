# 0062. The studio writes what git keeps

**Status:** Accepted · **Date:** 2026-10-31 (the F49.5 plan's findings R, S and T; Louis picked R and S on
2026-11-01, and the picks of the unattended steps 1 to 8 on 2026-11-03 wait for his review on the pull request) ·
**Recorded:** 2026-11-03

## Context

After F49.4 the admin portal could make samples and transcripts from stored runs, but a new eval was still a folder
written by hand in Rider. Louis's goal was that "a new eval must only be a json file", made and edited "from the admin
panel, living in identity". F49's parent done-when for its last child: a new eval is made, tried and published from
the admin panel.

Three questions had to be settled. **Who reads and judges an eval file** (finding R): the runner, through new
commands the API starts as a process (taken); the API referencing the harness, so every host carries it; or the API
keeping its own copy of the records, two readers that drift. **How a publish reaches git** (finding S): the folder
committed on a branch in the API's workspace, the push and the pull request left to the person, a zip where there is
no workspace (taken); a GitHub token in the API that pushes and opens the pull request (a secret and a GitHub
dependency in the API); a zip only (Louis's own PC would get no commit either). **promptfoo in and out** (finding T):
a pair of runner commands with YamlDotNet and a mapping table (taken); the same in the API (finding R again); later,
on the TODO (F49's done-when not met).

The lesson behind all three, from the research of 2026-10-28: the hosted eval formats that died took their evals
with them. Git stays the truth; the studio is a convenience over files.

## Decision

- **The runner is the one reader and judge of eval files, reached as a process.** The API never references the
  harness. It starts the runner (`RunnerProcess`: an argument list, never a shell, a timeout, the runner's last stderr
  line as the reason) for:

  | Command | Does |
  |---|---|
  | `--checks` | The catalogue as JSON lines: each check type with its kind and its settings' JSON schema, then each reader with the kind it reads |
  | `--check {folder}` | Seals a manifest left without its hash (`ManifestSeal`), loads the folder as strictly as a run would and holds it to the conformance gate CI runs; one `{"problem": …}` line each, then `{"ok": …}`; exit 0 whenever it could look |
  | `run {eval} --folder {path} --samples a,b` | Runs the eval in a folder outside `EvalFiles/` (`RunTarget`, its name checked against the run's), only the samples named (`SampleSubset`, a Decorator over `IEval`) |
  | `--import-promptfoo {file} {folder}` | A promptfoo file to an eval folder; one `{"left_out": …}` line for each part with no counterpart |
  | `--export-promptfoo {folder}` | An eval folder to a promptfoo file, on stdout |

- **An eval being composed is a draft in the store, never a file yet.** It is the third typed view of `eval_drafts`
  (`kind` `eval`, beside ADR 0061's samples and transcripts): the file's text exactly as typed, its rubrics, who saved
  it and when, and scenario files of its own when an import brought them. Its name and identity are read from the file
  (kebab-case), so the two can't disagree; the rest is the runner's to judge, so a half-made draft can be saved.
- **One bridge from records to files.** `DraftFolder` writes a draft out as the folder the runner reads, with the
  sample and transcript drafts filed under its eval, afresh each time. Check, try, publish and export all read that
  folder, so what is checked is what is tried and what is published.
- **A try is always a replay.** It replays a stored run on the samples named, so it costs nothing; a question the
  recording lacks ends as a replay miss, never a model call. A fresh run stays the Run button's, on Louis's go.
- **A publish commits, and never pushes.** Only a folder the runner found no problem with, beside an identity project
  the workspace has, is committed: to `identities/louis-agent.identity.{identity}/Evals/{PascalName}/` on the branch
  `eval/{identity}-{name}`, through git's plumbing over a temporary index, off the workspace's HEAD, so the person's
  branch and working tree stay as they were. The push and the pull request are the person's. A workspace that isn't a
  git checkout gets the folder as a zip. The draft stays until the person deletes it.
- **promptfoo is an Adapter with a mapping table, and what it can't map is named.** `tests[].vars` become dev
  scenarios, `assert[].type` maps by `PromptfooAssertion` (an enum of promptfoo's names, ADR 0043: `contains`,
  `not-contains`, `starts-with`, `is-json`, `levenshtein` as `near`, `latency` as `within-time`, `cost` as
  `cost-under`), each `llm-rubric` becomes a rubric file a `judge` check reads. Anything else is left out and said, on
  import and in a comment at the top of an export, never dropped unsaid. YamlDotNet (MIT, 18.1.0) is in the runner
  only (Louis: "add yamldotnet if needed").

The routes, behind the admin key:

| Method | Route | Does |
|---|---|---|
| `GET` | `/v1/admin/evals/checks` | The runner's catalogue: types, kinds, settings' schemas, readers |
| `POST`, `GET` | `/v1/admin/evals/drafts` | Make an eval draft; list them |
| `GET`, `PUT`, `DELETE` | `/v1/admin/evals/drafts/{id}` | One eval draft |
| `POST` | `/v1/admin/evals/drafts/{id}/verdict` | The runner's check of its folder (not `…/check`: a resource is never named both singular and plural, and `/evals/checks` is the catalogue) |
| `POST` | `/v1/admin/evals/drafts/{id}/try` | A replay on `{replay, samples}` |
| `POST` | `/v1/admin/evals/drafts/{id}/publish` | A commit on its branch, or a zip |
| `POST` | `/v1/admin/evals/drafts/promptfoo` | A promptfoo file to a new eval draft, with what was left out |
| `GET` | `/v1/admin/evals/drafts/{id}/promptfoo` | The draft as a promptfoo file |

## Consequences

- A new eval is composed, checked, tried and published from the Studio page (`/admin/studio`) without Rider, on
  Louis's PC as a commit on a branch and on Azure as a zip for a pull request; F49 is done.
- Every check, try and export costs a process start, about a second; a check that grades every transcript may take
  up to two minutes.
- On a hosted instance a publish is a download, not a pull request; a GitHub token would change that, and would need
  its own ADR.
- A promptfoo file with JavaScript or Python asserts loses them on import, by name: .NET has no in-process sandbox, so
  a script is never run (F49's parent plan); a new check is a reviewed catalogue entry (ADR 0056).
- The runner's command line grew five commands; each is tested in `louis-agent.evals.runner.tests`, and the API's
  side with a stand-in runner function.

## Links

[The evals' architecture](../architecture/evals.md#the-studio) · [0004](0004-arguments-as-data.md) ·
[0043](0043-repeated-names-are-enums.md) · [0056](0056-a-check-is-a-catalogue-entry.md) ·
[0058](0058-an-eval-is-a-folder-of-files.md) · [0061](0061-evals-grow-from-use.md) ·
[write-up 12](../learning/12-evaluation.md#compose-try-publish-f495) ·
[write-up 18](../learning/18-testing-and-shipping.md#git-stays-the-truth-f495) ·
[Add an eval](../guides/add-an-eval.md#9-or-compose-it-in-the-studio) · promptfoo's assertion reference · YamlDotNet

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
