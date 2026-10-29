# CL24 Done docs folded: what stays, what changes, what goes

> **Plan (approved 2026-10-29).** Bring every doc up to date now that F18 is merged (PR #95), and take the docs of
> finished work out, as Louis decided on 2026-10-28 ("do we still need implemented feature plans? and feature docs?",
> then "yes add it"): a done feature's or fix's plan is deleted, its doc's lasting parts move to the project's design
> doc and its learning write-up, then the doc goes, and the READMEs keep one status line per id. This is also the
> sub-features docs step and CL24's pass. Branch `docs/after-f18` off `main` (cc6afe8). README.md is left alone apart
> from one link (below).

Surveyed 2026-10-29 on `main`: 32 feature docs, 42 plans, 24 fix docs, and every file that links them (about 70,
mostly ADRs, the CHANGELOG, the specs and the project docs; one code comment in `ArchitectureTests.cs` and one in
core's `CLAUDE.md`).

## Remove (git keeps them)

**Plans of finished work, 36 files, deleted whole** (a plan is the *how* of a build that is over; nothing in it is
lasting that isn't already in an ADR, a write-up or the code):

- Cleanups: CL1, CL2, CL3, CL4, CL5, CL6, CL7, CL8, CL9, CL10, CL13, CL14, CL15, CL19, CL20
- Features: F01, F02, F03, F04, F05, F05-usage-rollup, F14, F18-evals, F18-unattended-decisions, F20, F21, F22, F24,
  F25, F26, F27, F29, F30, F31, F32, F33, F34
- Fixes: FX17
- The one open item that lives only in `F18-unattended-decisions.md` (finding 3, the session bodies into the
  contract) is already a TODO line; the line stops linking the file and says it in full.

**Feature docs of finished work, 20 files, folded then deleted.** Where the lasting parts go:

| Feature docs | Lasting parts move to |
|---|---|
| F1 ledger, F2 prices, F3 usage display, F14 display currency | `projects/louis-agent.core.md` (the ledger, prices, currency), `projects/louis-agent.usage-probe.md`; the numbers are already in write-up 13 |
| F4 prompt caching, F5 toolset profiles | `projects/louis-agent.core.md` (the cache markers, toolsets per session); ADR 0006 and write-up 13 already hold the why and the numbers |
| F20 Puls look, F24 landing motion | `projects/louis-agent.puls.md` |
| F21 roles, F22 role tools, F25 local cook, F26 follow-up, F29 validated specialists | `projects/louis-agent.identity.puls.md` and `projects/louis-agent.core.md` (sub-sessions, the role gate); write-up 10 |
| F27 admin portal | nothing: F31 replaced it two days later |
| F30 local web peer (built, not in use) | `projects/louis-agent.core.md` (minions); its open item, *F30's minions tested*, is already a TODO line |
| F31 benchmarks portal | `projects/louis-agent.api.md` and the evals project docs (F18 rebuilt it) |
| F32 self-hosted web, F33 shop catalogues, F34 shop deals | `projects/louis-agent.core.md` (search and the browser), `projects/louis-agent.pack.shops.md`; write-up 07 |
| F18 evals (1,909 lines) | `projects/louis-agent.evals*.md` (the four of them) and write-up 12; its follow-ups become TODO lines |

## Keep, unchanged or nearly

- **Feature docs of work still to come:** F7, F8, F9, F10, F15, F28 (step 1 built, the rest open), and the ones merged
  into those (F6, F11 into F7; F12 into F44; F13 into F15; F16 into F9; F17 into F39), since they hold the design the
  parent will be planned from. Each one's status line checked.
- **Plans still to build:** F28 (its open steps), F48 (the cook and teacher), `platform.md` (see card 3).
- **Fix docs:** FX4 (open). The 23 done ones depend on card 1.
- **ADRs** (all 49): records, never rewritten; only their links to deleted files change (card 2).
- **Specs** (`docs/specs/`), **write-ups** (`docs/learning/`), **guides**, **architecture**, **project docs**: kept,
  and updated where the code moved on.

## Update

| Doc | What changes |
|---|---|
| `docs/CLAUDE.md` | The sub-features rule (a plan holds about ten steps; above that a parent with `F49.1`, `F49.2`, each its own plan, branch, PR, done-when and write-up; commit scope `f49.1`), and the step list's *Finishing* points at the new finishing rule |
| `features/README.md`, `fixes/README.md`, `plans/README.md` | One status line per id (done ones link nothing, or the tag); *Finishing a feature* and *Finishing a fix* gain "fold the doc, delete the plan"; the plans README gets the sub-features convention and loses the frozen-roadmap rule if card 3 goes that way; milestones table checked |
| `TODO.md` | Out: CL18 (built in F18 step 1e), *Personal data out of the logs* (built in step 4f), the ticked `Endpoints/` line. In: Phase 3b to 3e as parents with their children (F49 evals as content .1 to .5, F35 .1 to .2, F48 .1 to .2, F46 .1 to .2, F50 Puls's specialists .1 to .7), the later parents' children on their lines (F40, F7, F39, F42, F36, F43, F44, F51 hosting, CL17, F47), CL24 marked done, and F18's follow-ups: FX26 (a sub-session's tool result isn't replay-stable, and no *replay miss* kind), the judge check on Louis's own labels, the reference transcripts' size in git. The *Now* heading moves from "Phases 0 to 4" to what is now |
| `docs/GOAL.md` | *Planned* loses what is built (CL15 Phase 2, FX20 Phase 1, personal data out of the logs, the one stream reader); skill 12's *Planned* gains F49; skill 18's *Have* gains the CI that grades recorded transcripts; phase names follow the plan (3b to 3e, 10) |
| `CHANGELOG.md` | One entry for this pass (what was removed, and where to find it now); old entries keep their text, their links follow card 2 |
| `docs/README.md` (index) | *Features*, *Plans*, *Fixes* rows say what they hold now; the project-docs row gains the evals projects |
| `docs/specs/*` | The *Implementation* maps stop linking deleted feature docs (F1 to F5, F14 done; the rest still planned) |
| `docs/projects/*` | Each one read against the code and its folded feature docs; the four evals docs checked against what merged |
| `docs/architecture/05`, `10`, `AGENT_COMMUNICATION.md`, `MODELS.md`, `deploy/azure/README.md`, core's `CLAUDE.md`, `ArchitectureTests.cs` (one comment) | Links to deleted docs |
| `docs/plans/platform-summary.md` | The investor summary's *what's built* lines brought to today (Phases 0 to 3 done) |
| The living platform plan (project files, and its page) | Phase 3's done steps out, one done-line left, as the plan's rule says |
| `README.md` | One link only: line 124 points at the F18 feature doc, which goes; it will point at the evals project doc |

## Commits, in order (each shown before it is committed, nothing pushed without asking)

1. This plan as `docs/plans/CL24-done-docs-folded.md`, the sub-features rule and the finishing rule in
   `docs/CLAUDE.md` and the three READMEs.
2. TODO: the sub-features as lines, the built ones out, F18's follow-ups in.
3. GOAL.md columns.
4. The platform plan: Phase 3 done (the repo's copy per card 3, and the living one).
5. Fold usage and caching (F1 to F5, F14) into the core and usage-probe docs, delete those docs and plans.
6. Fold Puls (F20 to F22, F24 to F27, F29, F30).
7. Fold the web and shops (F31 to F34).
8. Fold F18 into the evals docs.
9. The cleanups' plans (CL1 to CL20) and FX17's plan deleted.
10. The fix docs, per card 1.
11. Every link to a deleted file, per card 2, and the indexes; a link check over the whole repo (no link to a missing
    file).
12. The project docs read against the code.

## Decided (Louis, 2026-10-29, one card each)

| # | Question | Pick |
|---|---|---|
| 1 | Do the finished fix docs go too? | **Fold them**: each fix's row keeps its exact symptom and its cause, so a search for the error still finds it |
| 2 | What do links to deleted docs point at? | **A tag, `pre-cl24`**, on `main` before the deletes (cc6afe8), as CL10 did with `pre-cl10` |
| 3 | The repo's `docs/plans/platform.md`? | **Replaced by the living plan**: current and future only, built phases as done-lines; the "never edited" rule goes |
| 4 | Parent plans for every sub-feature parent now? | **TODO lines now**, each listing its children and their done-whens; a parent's plan file when it starts |
| 5 | All now, or a few at a time? | **All now**, in about twelve small commits by area |

By its own rule, the PR's last commit deletes this plan too, once the work is reviewed.
