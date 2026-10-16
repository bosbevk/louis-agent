# CL9 · Modes per product: a voice and its router, chosen per session

> **Plan.** How a level-0 session becomes a *mode*, a voice and the router that goes with it, so Puls and the coding
> agent each get only their own routing, Puls speaks Afrikaans or English as its user chose, and `AGENT_FUNCTION`
> goes. **For:** TODO *Modes per product* (cleanup). **Approved:** 2026-10-16 (Louis; the coding router named `louis-router`).
> **Branch:** `refactor/CL9-modes-per-product`, stacked on `refactor/CL8-skills-by-level` and rebased onto `main`
> once CL8 is merged; before F30 step 4.

## Context

CL8 left `louis-agent.core/Prompts/agent/` holding three files that do three different jobs: `personality.md` (Louis's
Minion, the coding agent's voice, for every level-0 session that names none), `puls.md` (Puls's voice, Afrikaans) and
`default.md` (the router, shared by both products). Louis (2026-10-16), reading that folder: "why do we have [these],
could we not just have one, for instance modes puls-default-af (for afrikaans), puls-default-en (for english) … and
then puls-orchestrator for the orchestration part"; then "code-review could also be a mode?", "there is no real
reason for agent function", and "there is a lang selector … if that changes update the lang file; we only cater for
af and en".

What the code shows:
- **The shared router costs both products.** `default.md` carries Puls's cook row and its Afrikaans briefing labels
  (`**Vir:**`, `**Wil hê:**`, `**Klink:**`) and the coding rows (files, git, dotnet, Paymo, DevOps) Puls never uses; every
  request of either product pays for the other's rows in its fixed prefix.
- **`AGENT_FUNCTION` selects a placeholder.** CL8 step 6 removed time-logging; what's left is `code-review.md`, whose
  three skills only `echo` (the file says so), and `louis`, which loads every function file. Its default is
  `code-review`. The orchestrator sets it to `orchestrator` and never reads it.
- **The code-review app is a mode, not a function.** `louis-agent.web` opens sessions with every toolset and no
  personality; nothing about it uses the code-review skills.
- **Puls's language is fixed twice.** `ChatPage.razor` sends `personality: "puls"`; the *Taal* menu lists English,
  Nederlands, Deutsch and isiZulu, each "coming soon". Nothing ties a language choice to the voice.

Goal skill: 11, context engineering (each mode's prompt holds only its own routing), and 13, cost (a smaller fixed
prefix per request, measured).

## Design

### Modes

A **mode** is a level-0 voice and the router that goes with it. The voices are what a session names; a router is never
named, it comes with its voice. Specialists (level 1) and minions (level 2) are unchanged: they have no router.

```
Prompts/agent/
  puls-default-af.md      Puls's voice, Afrikaans (today's puls.md, git mv)
  puls-default-en.md      Puls's voice, English (new text: Louis writes or approves it)
  puls-orchestrator.md    Puls's router: the world (web), the cook, delegating, briefing and relaying
  louis-default-en.md     the coding agent's voice (today's personality.md, git mv): CLI, Rider
  louis-code-review.md    the code-review app's voice: how to review a change (new text: Louis approves it)
  louis-router.md   the coding router: files, git, web, scripts, dotnet, paymo, devops, skills
  voices.json             each voice and its router; the default voice
  functions/              (empty of built-ins: the agent's own function skills are on disk, in Skills/)
```

`voices.json` is data, as `specialists/roles.json` is:

```json
{
  "default": "louis-default-en",
  "voices": [
    { "voice": "puls-default-af",   "router": "puls-orchestrator" },
    { "voice": "puls-default-en",   "router": "puls-orchestrator" },
    { "voice": "louis-default-en",  "router": "louis-router" },
    { "voice": "louis-code-review", "router": "louis-router" }
  ]
}
```

- **A voice picks its router**, from `voices.json`. Considered: deriving it from the name's prefix (`puls-*` →
  `puls-orchestrator`), which hides a rule in a naming habit and breaks on the first voice that doesn't fit it.
- **The voices offered at level 0 are the ones in `voices.json`.** A router isn't a voice, so naming one is refused as
  an unknown personality; `Personalities.IsReserved` and the constants `DefaultFile` and `Router` go.
- **No name means the default voice** (`louis-default-en`), so the CLI, Rider and the MCP server keep the Minion and
  the coding router with no change on their side.
- **The engine key doesn't change:** the voice is already in it, and the router follows from the voice.
- **`GET /personalities`** keeps its shape; `default` becomes `"louis-default-en"` instead of `null`.

### Puls's language

- The *Taal* menu offers **Afrikaans** and **English** only. The choice is kept in this browser, as the currency and the
  location are, and defaults to Afrikaans, so Puls behaves as today.
- **New chats** open with `personality: "puls-default-{af|en}"`; a running chat keeps its voice, as a session keeps its
  personality (its prefix stays cacheable).
- **Linked by data:** the menu lists the languages for which `GET /personalities` offers a `puls-default-{code}` voice
  (with a name per code in Puls: `af` Afrikaans, `en` English). A voice without a name in Puls, or a name without a
  voice, isn't offered, so the two can't drift; Nederlands, Deutsch and isiZulu come back when their voices exist.
- **The UI's own words stay Afrikaans** (Louis: only the voice follows the language).
- The checks (`samples/puls-check`, `samples/puls-cook-check`) send `puls-default-af` instead of `puls`.

### `AGENT_FUNCTION` goes

- `AgentOptions.AgentFunction`, its setting and the `louis` / function-name branches in `AgentHost.LoadSkills` go. With
  the `skills` toolset, a session loads every function skill the agent wrote (`Skills/agent/functions/*.md`, on disk);
  there are no built-in ones left once `code-review.md` goes.
- The orchestrator's `agentOptions.AgentFunction = "orchestrator"` goes. An `.env` that still sets `AGENT_FUNCTION` is
  harmless (an unknown variable); `.env.example` drops it.

### The routers' split

- **`puls-orchestrator.md`**: the world (`web`, answered yourself), cooking (`web`, `puls-cook`), delegating, the
  briefing shape, relaying a report, following up; files, git and scripts as *not yet* only if Puls should still say
  so in one line (to decide while splitting, with the delegation check's cases).
- **`louis-router.md`**: files, git, web, python / powershell / bash, dotnet, paymo, devops, skills, agents (a
  coding agent may delegate when it has peers); no cook, no Afrikaans labels.
- Step 1 copies `default.md` into both unchanged, so the rename is proven by the golden file before any text changes;
  step 2 splits the text.

### Architecture: where a session's prompt comes from

```mermaid
flowchart LR
    subgraph browser["Puls in the browser"]
        taal["Taal menu · step 4<br/>af · en (from GET /personalities)<br/>kept in this browser"]
        chat["ChatPage<br/>new chat: personality puls-default-{lang}"]
    end
    review["Code-review app (louis-agent.web)<br/>personality louis-code-review · step 3"]
    cli["CLI · Rider (ACP) · MCP server<br/>no personality"]

    subgraph api["louis-agent.api / puls-agent"]
        sessions["AgentSessions<br/>level = depth"]
        factory["AgentEngineFactory<br/>key: toolsets | voice | level"]
        host["AgentHost.LoadSkills<br/>level 0: voice + its router · steps 1–3"]
    end

    subgraph prompts["louis-agent.core/Prompts (embedded)"]
        voices[("agent/voices.json<br/>voice → router; default · step 1")]
        pv["agent/puls-default-af · -en"]
        lv["agent/louis-default-en · louis-code-review"]
        pr["agent/puls-orchestrator · step 2"]
        lr["agent/louis-router · step 2"]
        spec["specialists/ · toolsets/<br/>(unchanged)"]
    end

    disk[("Skills/agent/functions/*.md<br/>the agent's own skills · step 3")]

    taal --> chat --> sessions
    review --> sessions
    cli --> factory
    sessions --> factory --> host
    voices -.-> host
    pv & lv & pr & lr & spec -.-> host
    disk -. "with the skills toolset" .-> host
```

### Communication: Puls switches to English

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant P as Puls (ChatPage)
    participant A as puls (API)
    participant F as AgentEngineFactory
    participant H as AgentHost
    participant R as Prompts (assembly)

    P->>A: GET /personalities
    A-->>P: {default: louis-default-en, agent: [puls-default-af, puls-default-en], specialists: [puls-cook], …}
    Note over P: Taal lists af and en: a voice exists for each
    U->>P: Taal → English
    Note over P: kept in this browser; the open chat keeps its voice
    U->>P: a new chat: "What can I cook for four tonight?"
    P->>A: POST /sessions {toolsets: [agents, web], personality: puls-default-en, …}
    A->>F: GetOrCreate([agents, web], puls-default-en, level Agent)
    F->>H: LoadSkills(…, puls-default-en, Agent)
    H->>R: agent/voices.json → router puls-orchestrator
    H->>R: agent/puls-default-en.md, agent/puls-orchestrator.md, toolsets/agents.md, toolsets/web.md
    H-->>F: the prompt (Puls in English, Puls's routing only)
    F-->>A: the engine (cached by its key)
    A-->>P: 201 {session_id, personality: puls-default-en}
    P->>A: the turn, as today: Puls hands the cooking to the cook
```

## Steps (one commit each, test first, stop after each)

| Step | What | Check |
|---|---|---|
| 0 | **Docs:** this plan; ADR 0019 *A level-0 session is a mode: a voice and its router*; the TODO line; the plans index row | — |
| 1 | **Voices and routers in code, the text unchanged:** `git mv` `puls.md` → `puls-default-af.md`, `personality.md` → `louis-default-en.md`; `default.md` copied to `puls-orchestrator.md` and `louis-router.md`, then removed; `voices.json`; `PersonalityFile`, `LoadSkills` and `Available(Agent)` read it; `IsReserved`, `DefaultFile`, `Router` go. Callers: Puls's `ChatPage` (`puls-default-af`), both checks' `run_check.py`. `SkillsLayoutTests`: every `agent/*.md` is a voice or a router in `voices.json`, and every name there has its file | Tests first: a voice gets its router; no name gets the default voice and its router; a router named as a voice is refused; an unknown voice is a 400 listing the voices; the layout rule. **Golden file unchanged** (same voice, same router text) |
| 2 | **Split the routers' text:** Puls's rows and rules in `puls-orchestrator.md`, the coding rows in `louis-router.md`; the `PersonalitiesTests` that read `default.md` read Puls's router. The fixed prefix before and after, from the start-up log's "about N tokens of fixed prefix", per profile (free) | Tests first (Puls's router has the cook and the briefing shape, the coding router has neither; the coding router has every coding toolset's row); golden file: only the router's text |
| 3 | **`AGENT_FUNCTION` goes; code review is a mode:** the option, its setting and `LoadSkills`' branches go; `code-review.md` goes; with `skills`, every function skill on disk loads; `louis-code-review.md` (draft for Louis); the code-review app names it; the orchestrator's line goes | Tests first (an agent-written function loads with `skills` and no setting; the code-review app's voice is offered; `AgentFunction` is gone); golden file: the code-review section and the router's line about functions |
| 4 | **Puls in English:** `puls-default-en.md` (draft for Louis's wording, the same sections as the Afrikaans voice); the *Taal* menu as af / en from `GET /personalities`, kept in the browser, new chats only | Tests first: both Puls voices have the same section headings (a structural check that the two languages don't drift); the web app by hand (no Puls test project): pick English, a new chat's `POST /sessions` names `puls-default-en`, the open chat doesn't change |
| 5 | **Docs and the rule:** *Rules that always apply* (modes); *Adding a personality* becomes *Adding a mode or a role*; `AGENT_FUNCTION` out of SETUP, the views and the glossary; the link check | Link check clean; `SkillsLayoutTests` |
| 6 | **Rebuild and check by hand (free):** every image; `GET /personalities` on both stacks; a Puls session in each language and a code-review session open with the right voice and router in the start-up log; the CLI starts on `louis-default-en` | By hand |
| 7 | **Measure (Louis's go; a few rand):** the cook check and the delegation check on `puls-default-af` against their saved baselines (F25, F29): routing right, cost per answer, prefix tokens; one English cook question to see the English voice hand off | `--report` on the saved runs first (no model calls); then the paid run; the numbers in BENCHMARKS.md and the CHANGELOG entry |

## Reused, not built

- `specialists/roles.json` and `Personalities.RoleFor` as the pattern for `voices.json` (a JSON file read through
  `SkillFileSource`, rules as data).
- `CurrencyState`'s way of keeping a choice in the browser, for the language.
- `GET /personalities` as the source of the languages (no new endpoint).
- `SkillsLayoutTests` and the golden file as the regression tests.
- Considered, not used: a `language` field on `POST /sessions` beside the personality (two settings that must agree);
  the router chosen by name prefix (above); keeping one router with per-product sections (both products still pay for
  both).

## Risks to watch

- **Puls's routing after the split** (step 2): rows move, and a row Puls relied on could be lost. The delegation and
  cook checks (step 7) measure it; step 2's tests pin the cook row and the briefing shape in Puls's router.
- **The English voice's quality** is a prompt Louis hasn't tuned; step 7 sends one English cook question, and the
  voice's wording is Louis's call before the step is committed.
- **The name *orchestrator***: `puls-orchestrator` shares a word with `louis-agent.orchestrator`, the microservice POC;
  the coding agent's router is `louis-router` to keep the clash to one file (Louis, 2026-10-16).
- **F30's ADR number moves again:** main's licence took 0016 and CL8 0017–0018, so this plan's ADR is 0019 and F30's
  minions ADR becomes 0020.
- **Open Rider chats** keep their container; a rebuilt image takes effect at a **New Chat**.

## Verification

- `dotnet build louis-agent-solution.sln`; `dotnet test` on core and API each step; the whole solution before the PR.
- The golden file unchanged in step 1; in steps 2 and 3, only the router's and the code-review text.
- Every image rebuilt and checked by hand (step 6); the paid run only with Louis's go (step 7).

## Concepts

- **Context engineering** (goal skill 11): a mode's prompt holds its own routing and nothing else; the voice and its
  router are paired as data, not left to a naming habit.
- **Cost of the fixed prefix** (goal skill 13): every request pays for the router; a smaller one is measured per
  profile, free, from the start-up log, before any paid run.
- **One source of truth across front end and back end:** the language the menu offers is read from the voices the API
  has, so the two can't disagree.

## Approved during the build
