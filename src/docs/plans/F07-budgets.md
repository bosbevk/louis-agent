# Draft: F7 Cost you can see and stop (Phase 5)

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 1959 to 2004 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.

### Phase 5 · F7 Cost you can see and stop

Branch `feature/F7-budgets`. Planned to the feature here: before step 1 its own plan, `docs/plans/F07-budgets.md`, is
written and approved by Louis, with its four diagrams (architecture, interfaces, objects, communication), its namespace
map and its folder trees; *Approved during the build* goes in that file. It takes in F6 (estimates) and F11 (the usage
view and the check against the bill), whose docs say so from Phase 0 (step 3). OWASP LLM10:2025 (unbounded consumption)
is the standard; ADR 0007 the rule. GOAL skills: 13 and 16; a budget is also a stop condition beyond a round count, so it
partly meets one of skill 6's *Missing* items (`docs/GOAL.md:27`). Paid runs: one simulation run, in step 2, on Louis's go.

**What exists to start from.** F7's doc already holds four of the user stories (budgets set in a file, warnings before
the limit, a clean stop, an ask to continue; `docs/features/F07-budgets.md:15-36`) and a design: a `BudgetChatClient`
before every model request, and the history kept when it stops, so the turn can continue (`:56-62`). It was written for
the M3 cost roadmap (`:3`), before customers, identities and simulations; the plan file keeps its stories and rewrites
its design around them. Its fifth story, the orchestrator deferring what it can't afford (`:37-41`), is Phase 6's B4
(below). Today the only budget in code is a sub-session's prompt budget (FX14,
`louis-agent.core/Agent/ToolCallGuards.cs:90-104`); everything else stops only at the provider account's own limit.

**Built from Microsoft.Extensions.AI's own pieces** (the engine research's change 14; Part 3's row 15). Budgets are
`IChatClient` middleware in core's `Llm/`, reading the ledger's totals, per session, per customer and per day. "Ask"
goes through `ApprovalRequiredAIFunction` and `ToolApprovalRequestContent`, both in Microsoft.Extensions.AI.Abstractions
10.10.0, which core's Microsoft.Extensions.AI 10.10.0 depends on (`louis-agent.core/louis-agent.core.csproj:23`; checked
in the packages' metadata and API docs), so an ask reaches every host as the same content F41's approvals use in Phase
7. How the two meet is the plan file's to prove with a test (*inferred*: an ask budget, once reached, marks the
session's tools as needing approval, so the loop's next tool call waits for the person and nothing more is spent). A
budget per turn tree (a parent and its children together) comes with Phase 6's `DelegationLimiter` (A3), which then
carries money beside the prompt budget. This budget is the engine's half of rate limiting (tokens and money per
customer and per day); the host's request limits per key are F42's (Phase 7, OWASP LLM10:2025). An account-limit
refusal from the provider is not a budget: stopping on it is F8's (Phase 9), and budgets act before it
(`docs/features/F07-budgets.md:67`).

| # | Step | Done when | Concept (GOAL skill) |
|---|---|---|---|
| 1 | **A customer on every row:** a `customer` on every session and ledger row (a row names host, session, turn, task, run and service today, but no customer, `louis-agent.core/Usage/UsageRecord.cs:28-44`), from the API key until Phase 9's accounts; which key names which customer is a setting the plan file names | the ledger totals per customer; a row with no customer is counted apart, never as someone's | Cost attributed to who spent it, so a price per customer is measured, not guessed (13) |
| 2 | **Estimates:** a run's or a turn's likely cost from the ledger's history per role and mode, as a range with its basis (F6's stories: the 25th to 75th percentile of at least five similar runs, else an estimate from the model's prices, `docs/features/F06-estimates.md:20-30`) | estimate against actual shown per simulation run; **one paid simulation run, on Louis's go**, to compare the estimate with the actual | An estimate as a range with its basis, checked against the actual (13) |
| 3 | **Budgets** per session, per customer and per day: warn, ask (a typed question to the user, through `ApprovalRequiredAIFunction` and `ToolApprovalRequestContent`) or stop, cleanly and append-only (ADR 0008): what the turn produced stays in the history, and the stop carries its reason | a fake client's turn stops at its budget with a reason; the ledger shows it; an ask reaches the host as a `ToolApprovalRequestContent`, and nothing more is spent until it is answered | A stop condition beyond a round count, and a budget that asks a person (13, 16; in part 6) |
| 4 | **The control room's cost view:** per customer, per identity, per simulation, in Phase 4's room; the ledger checked against the provider's own usage report (F11's fourth story). That report needs the provider's organisation admin key, which F11's doc calls `ADMIN_API_KEY` (`docs/features/F11-usage-tab-and-reconciliation.md:31,44`), the name the admin portal's own key has had since F27 (`src/louis-agent.api/Config/AdminOptions.cs:24`), so the plan file gives the provider's key a setting of its own; without one, the view says why the check is unavailable (`:34`) | the two agree, or the difference is shown with its reason | Reconciliation: our own ledger checked against the provider's bill (13) |
| 5 | **Docs and the learning write-up for skill 13** (it counts as learned here), written by Louis himself without the code open: `docs/learning/13-cost-and-token-economics.md` gains estimates, budgets and the check against the bill, as its index row says it will (`docs/learning/README.md:10`). Skill 13's only *Missing* item closed in Phase 3 (the replay cache), and three of its four *Planned* items, estimates, budgets and reconciliation, are built and measured here (`docs/GOAL.md:34`; the fourth, each model's cache minimum and price step, is F15's, in Phase 9). Skill 16 gains its *Planned* "budgets that ask" (`docs/GOAL.md:37`) and counts only after Phase 7's approval policies. F6, F7 and F11 closed as *Finishing a feature* says (`docs/features/README.md:124`) | Louis's write-up written; links checked | Writing it up (13, 16) |

**Moved to Phase 6:** the orchestrator deferring what it can't afford is not a step here. It needs `TriageWorkflow`,
which only Phase 6 builds, so it is B4's: `TriageWorkflow` defers an error whose estimate exceeds its F7 budget
(outcome `Deferred`; test: a fake triage defers when its budget is spent), and F7's fifth story goes with it.

**Verification.** Every step free but step 2: fake clients and recorded ledgers, the touched projects' tests and the
golden files (a budget's texts are message members, so a new one shows in `ToolMessageSnapshot.json` or the API's own
enum, reviewed). Paid: the one simulation run of step 2, on Louis's go.

