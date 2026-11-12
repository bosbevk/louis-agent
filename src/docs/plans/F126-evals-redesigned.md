# F126 Evals, redesigned: one file of conversations, a judge you correct, one page per run

> **Plan, approved by Louis on 2026-11-09** (thread "Simplify our evals"). A parent with four children, each its own
> plan, branch and PR when it starts (the development guide, rule 2). Nothing here is built. Every choice below was put
> to Louis with three options and picked by him on 2026-11-09; the research is the doc
> [Evals, redesigned from scratch](https://claude.ai/artifact/6Kng5tR4AXcbb1VvLqrefm), and the screens are drawn in
> [F126-mockups/](F126-mockups/) (open the `.html` files in a browser; also on the canvas
> [Eval pages](https://claude.ai/artifact/TYsS27hXkGjEVuT77oK6dk)). It changes only under *Approved during the build*.
> It replaces the CL60 plan, merged the same day and dropped here.

## Context

**What Louis asked.** "Can we research how we can simplify our evals?", then: "we can redesign and throw everything
away, I do not like the evals how we do it", "it is not simple, the idea is too make it simple and user friendly and
have an elegant back end design that still works", "we cannot have 23000 lines of code for something simple", and
"forget the current evals, we will redo it all" (all 2026-11-09).

**What exists.** About 33,000 lines of code and 22,000 of tests across `src/evals/` (harness, check contract and
catalogue, protocol, runner), `louis-agent.api.evals` (store, recording, the runner as a child process, reviews,
calibration, drafts, the studio, promptfoo in and out), `louis-agent.web.evals` (five admin pages, a run page with five
tabs and a five-level explorer) and Puls's readers, judge and pack checks; about 300 eval files on disk. An eval is a
folder (`eval.json`, a manual, a dataset, labels, transcripts; ADR 0058), its checks typed entries in a catalogue
(ADR 0056).

**What the others do.** Anthropic's *Demystifying evals for AI agents*: read the transcripts, and a model judge
"closely calibrated with human experts". OpenAI's HealthBench: a rubric of plain criteria per conversation, graded by a
model. Hamel Husain and Shreya Shankar's *AI Evals FAQ*: binary pass or fail, error analysis first. promptfoo's
`llm-rubric`, Inspect's eval logs and Microsoft.Extensions.AI.Evaluation's reporting: one result file a page reads.

**The bar** (Louis, 2026-11-09): someone who has never seen the code writes an eval in five minutes; a run's result is
understood at a glance; the back end is a few small parts with one job each.

## Design

### The shape, in plain words

An eval is **one file**, `{name}.eval.json`, beside the identity it proves. It names the agent (role and toolsets) and
lists **conversations**. A conversation has an id, where the person is (`place` for Puls, `page` for the guide on
louis-agent.dev) and a list of **turns**: the question, then each follow-up. Every turn has its own **facts** (what
code can tell for certain and for free) and **criteria** (plain sentences the judge marks yes or no with a reason).

```json
{
  "$schema": "../../../src/evals/eval.schema.json",
  "agent": { "role": "puls/voice-af", "toolsets": ["specialists", "web"] },
  "conversations": [
    { "id": "cook-follow-up", "place": "Somerset-Wes, Wes-Kaap",
      "turns": [
        { "ask": "Wat kan ek vanaand vir vier mense kook met hoender? Ek koop by PnP in Somerset-Wes.",
          "facts": ["no-error", "routed-to puls-cook"],
          "criteria": ["Feeds four people", "Names a PnP branch in Somerset-Wes", "Shows a photo from the recipe page"] },
        { "ask": "Maak dit eerder vegetaries, vir ses mense. Wat kos dit nou?",
          "facts": ["no-error", "continued puls-cook"],
          "criteria": ["The new recipe has no meat", "Feeds six people", "Gives the new total"] } ] }
  ]
}
```

That is the whole eval: no manual, no dataset folder, no version, no check types, no readers, no modes. The guide's
example with seven conversations is [F126-mockups/guide.eval.json](F126-mockups/guide.eval.json).

- **Six facts**, words in the file parsed once into an enum (ADR 0024) and told by one switch: `no-error`,
  `routed-to {role}`, `continued {role}` (the follow-up asked the same address again with `AskAgent`, F26, F127.3.1), `used {tool}`, `not-used {tool}`, `under {seconds}s`. Never C# per eval.
- **Criteria** go to the judge, by default one call per criterion with the transcript; `"judge": "per-conversation"`
  in the file asks once for all of a conversation's criteria. Each answer is `{criterion, met, reason}`; never a score.
- A conversation **passes** when every fact and every criterion of every turn is met. The run's number is how many
  conversations passed, and per criterion how often it was met.
- **A run** plays each conversation through the same session API a person's chat uses, live or as a free **replay**
  of recorded model and web answers. It keeps a compact transcript (asks, answers, each tool call with its input and
  a short result), the seconds and the cost from the usage ledger (ADR 0007). No raw stream lines.
- **Runs live in their own API**, the eval host: it serves the eval pages, runs a run as a background job and calls the
  agent API over HTTP like any app (`HttpSessionClient`). The agent API ships no eval code; `MapLouisAgentEvals`
  leaves the demos' `Program.cs`.
- **The page a visitor reads** (the guide): only the address is in the file. On a live run the host fetches the page
  from the site and sends its address, title and text with the first message, as the widget does (ADR 0104); a replay
  plays the recorded page back.
- **Fixing the judge is a flip.** Click a mark you disagree with and flip it; the flip is kept with the run and counts
  as your answer from then on. "Judge agrees with you" per criterion is worked out from the flips: no label files.
- **Re-grade** replaces re-score: facts are checked again for free, the judge sees only new or reworded criteria, and
  every other mark and flip is reused. **Replay free** costs nothing; **Run live** asks first, with the estimate and
  1 or 3 repeats.
- **Copy as conversation** on any conversation of a run copies it as a ready JSON block for the file.

### The six screens

Drawn in [F126-mockups/](F126-mockups/) with the guide's evals as sample data, in the framework's Console look:

| Screen | Mockup | Replaces today |
|---|---|---|
| How the screens connect | [0-flow.html](F126-mockups/0-flow.html) | — |
| 1. Evals: a card per eval, last result, better or worse, Open last run, Replay free, Run live | [1-evals.html](F126-mockups/1-evals.html) | `/admin` (list, details, run controls, scorecard, timeline, trend, checks, drafts, runs table) |
| 2. A live run: the ask with cost and repeats, then the list filling in | [2-live-run.html](F126-mockups/2-live-run.html) | The live panel |
| 3. A run: passed against last time, a row per conversation, the criteria missed most | [3-run.html](F126-mockups/3-run.html) | `/admin/runs/{id}`'s five tabs |
| 4. A conversation: the turns with their marks and reasons, flip, Copy as conversation | [4-conversation.html](F126-mockups/4-conversation.html) | The five-level explorer and `/admin/review` |
| 5. A re-graded run: what changed, and which button when | [5-regraded.html](F126-mockups/5-regraded.html) | Re-score |
| 6. The eval file, with turns | [6-eval-file.html](F126-mockups/6-eval-file.html) | `/admin/studio` and its drafts |

**Indicators:** red `#d9675f` (text `#e8a39c`) failed, green `#6cbf94` passed, blue `#7fa8e8` a mark you flipped,
amber `#d4a72c` costs money or is out of date. Red is the framework's today; green, blue and amber are added as tokens
in `src/web/louis-agent.web/Styles/web/settings/_colours.scss`.

### Where each piece lives

Louis, 2026-11-09: what can be shared everywhere goes to the framework, the rest to a pack's web (ADR 0074, 0084, 0086).

- **The framework, shared by every site:** the five back-end parts and the schema under `src/evals/`; the eval host
  under `src/hosts/`; the six screens and the generic tool-call fold in `src/web/louis-agent.web.evals`, naming no
  pack, identity or site.
- **A pack's web, only where a pack needs its own look:** a view of that pack's tool calls in the conversation's fold
  (a recipe card for `FindRecipes`, a price list for the shop prices) in `packs/web/`; without one the fold shows the
  plain input and result.
- **A demo, its own content:** its `.eval.json` files beside the identity (`demos/puls/evals/`,
  `demos/louis-agent/evals/`) and the program that starts the eval host with its settings.

### The back end: five parts, one job each

| Part | Its one job | Pattern | Built from |
|---|---|---|---|
| `EvalFile` | Read and check one `.eval.json` strictly, once | Data Transfer Object (records, ADR 0040) | New |
| `EvalRun` | Play each conversation (live or replay) and keep a compact transcript | Pipeline: a conversation in, a transcript out | Today's `ISessionClient`, `HttpSessionClient` and replay store, kept |
| `Facts` | Tell the six facts from a turn's transcript | An enum and one switch (ADR 0024) | New |
| `Judge` | Mark each criterion yes or no with a reason | Strategy: per criterion or per conversation, each an `IChatClient` call with structured output | Today's `ConfiguredJudge` and `NoTemperatureChatClient`, kept |
| `RunStore` | Keep one run as one document, flips included, and list runs per eval | Repository over one Mongo collection of records (ADR 0048) | New |

The pages read `RunStore` and nothing else. A run is the same document whether it ran live, as a replay or in CI.
Old runs are deleted, not moved (ADR 0049).

### Interface design (proposed; F126.1 and F126.2 settle the signatures)

![A run reads its file, asks, gets facts for free and a judge's marks, and is kept](diagrams/F126-evals-redesigned-interface-design-proposed-f126-1-and-f126-2-settle-the-signatures.svg)

### Architecture

![An eval file goes into the eval host, a run talks to the agent API or the site, a judge grades it, and the run store keeps it](diagrams/F126-evals-redesigned-architecture.svg)

### Communication: one live run of one conversation with a follow-up

![One live run with a follow-up: two turns in one session, facts checked in code, the judge grading the criteria, and the run and its flips stored](diagrams/F126-evals-redesigned-communication-one-live-run-of-one-conversation-with-a-follow-up.svg)

### Objects in memory: one run

![One run in memory: conversations hold turns, turns hold their facts and marks, and the flips sit beside them](diagrams/F126-evals-redesigned-objects-in-memory-one-run.svg)

### Folder trees

Today, the parts that go: `src/evals/` (harness, checks contract, catalogue, protocol, runner, template),
`src/hosts/louis-agent.api.evals/` (store, recording, runner process, reviews, calibration, drafts, studio, promptfoo),
`src/web/louis-agent.web.evals/` (five pages, the run tabs and explorer), Puls's readers, judge and pack checks, and
the eval folders under `demos/*/evals/` and `packs/evals/`. After F126.4:

```
src/evals/louis-agent.evals/        EvalFile, Facts, Judge, EvalRun, RunStore, eval.schema.json
src/hosts/louis-agent.eval-host/    the eval host: the pages, the run job, the endpoints
src/web/louis-agent.web.evals/      the six screens and the generic tool-call fold
packs/web/…                         a pack's own tool-call view, only where it needs one
demos/puls/evals/*.eval.json        Puls's evals, one file each
demos/louis-agent/evals/*.eval.json the guide's and the triage team's evals
```

### Namespace map (proposed; each child's plan settles it)

Today's `louis-agent.evals` project holds the old harness; F126.1 builds the new parts beside it in a folder of
their own, and F126.4 deletes the old ones, so the name is kept.

| Type | Folder | Namespace | Child |
|---|---|---|---|
| `EvalFile`, `Conversation`, `Turn`, `Fact`, `FactKind`, `JudgeMode` | `src/evals/louis-agent.evals/Files/` | `LouisAgent.Evals.Files` | F126.1 |
| `Facts` | `src/evals/louis-agent.evals/Grading/` | `LouisAgent.Evals.Grading` | F126.1 |
| `IJudge`, `PerCriterionJudge`, `PerConversationJudge`, `Mark` | `src/evals/louis-agent.evals/Grading/` | `LouisAgent.Evals.Grading` | F126.1 |
| `EvalRun`, `RunResult`, `ConversationResult`, `TurnResult` | `src/evals/louis-agent.evals/Runs/` | `LouisAgent.Evals.Runs` | F126.2 |
| `RunStore`, `Flip` | `src/evals/louis-agent.evals/Runs/` | `LouisAgent.Evals.Runs` | F126.2, F126.3 |
| The eval host's program and endpoints | `src/hosts/louis-agent.eval-host/` | `LouisAgent.EvalHost` | F126.2 |
| Tests, mirroring each | `src/tests/evals/…` | `LouisAgent.Evals.Tests.…` | each child |

## The children, in build order

1. **F126.1 The file and the grading.** `EvalFile` and its schema, `Facts`, the two judges; tested on transcripts
   written by hand. No API, no page, no money.
2. **F126.2 Runs and the run page.** `EvalRun` over the session API and replays, the guide's page fetched at run,
   `RunStore`, the eval host, screens 1 to 4. Proved on a free replay.
3. **F126.3 Flips, re-grade and better-or-worse.** Flipping a mark, "judge agrees with you", re-grade reusing marks,
   the comparison with the last run, Copy as conversation, screen 5.
4. **F126.4 New evals and delete the old.** New evals for Puls, the guide and the triage team written fresh as
   `.eval.json`; one paid run per eval on Louis's go to set the baseline; then today's harness, runner, store, studio,
   pages, checks, readers and eval folders deleted, with an ADR that supersedes ADR 0029, 0056, 0058 to 0062 and
   0073 where they no longer hold.

## Verification

- Each child's tests, written first; the full suite once F126.4 is built.
- Free: replays of recorded runs, re-grades of stored runs with only facts or reused marks.
- Paid, each on Louis's go first: F126.4's baseline runs, and any live run while building.
- The bar: Louis writes a new eval in five minutes without reading code; the counts show the cut (estimate: code
  about 33,000 to 4,500 lines, tests about 22,000 to 4,000; the "after" numbers are estimates).

## Concepts, and where they sit in GOAL.md

Skill 12, Evaluation: rubric grading with binary criteria, a model judge calibrated against a person's corrections,
error analysis from transcripts, replay as a free regression test, and deterministic checks for what code can tell.
Skill 4, Tool use: a follow-up proved to reach the same specialist (`continued`).

## Decisions taken (Louis, 2026-11-09)

- **C1 the file:** JSON with a schema (not YAML, not Markdown).
- **C2 where a run runs:** its own API, the eval host (not inside the agent API, not a runner process, not `dotnet test`).
- **C3 code or judge:** facts in code, the rest as criteria; and "forget the current evals, we will redo it all".
- **C4 a new conversation:** a Copy as conversation button (not by hand only, not evals in the database).
- **C5 the judge:** one call per criterion by default, per conversation as an option in the file.
- **C6 follow-ups:** turns, each with its own checks (not asks as a list, not a simulated person).
- **C7 the guide's page:** the address only, fetched at run (not the page in the file, not no page).
- **The split:** what every site shares goes to the framework, the rest to a pack's web.
- **CL60 is dropped:** it tidied what F126.4 deletes.

## Approved during the build

Nothing yet.

---
[Plans](README.md) · Related: [ADR 0029](../adr/0029-simulations-in-csharp.md) · [ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md) · [ADR 0104](../adr/0104-a-message-may-carry-the-page.md)
