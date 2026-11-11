# Draft: Phase 7: FX4, F41, F42

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 3007 to 3214 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.

### Phase 7 · Checked and guarded: FX4 Append-only history, F41 Drafts and approvals, F42 Safety and audit

Planned to the feature here, as Phases 5, 8 and 9 are: each of its three items gets its own plan file, written and
approved by Louis before its first step, with its four diagrams (architecture, interfaces, objects, communication), its
namespace map and its folder trees (today and after); *Approved during the build* goes in that file, never in
`platform.md`. FX4 comes first, on `fix/FX4-append-only-history` with `docs/plans/FX04-append-only-history.md` (a fix
of more than one step gets a plan, `docs/CLAUDE.md` rule 3). Then F41 on `feature/F41-drafts-and-approvals`
(`docs/plans/F41-drafts-and-approvals.md`) and F42 on `feature/F42-safety-and-audit`
(`docs/plans/F42-safety-and-audit.md`). Neither feature has a doc yet (`docs/features/` ends at F34, checked), so each
plan's first step writes one. The parts below are input to those plans, not approved steps.

It starts after Phase 6, whose typed results (a C# record's schema, validated strictly) the drafts reuse, and it builds
on Phase 4's invoice simulation and Phase 5's "ask", which reaches the hosts as the same `ToolApprovalRequestContent`
(*inferred* from both phases; F41's plan proves it with a test). GOAL skills: 5, 16 and 15. Paid runs: the injection
simulations, with FX4's hand confirm on Haiku 5.5 in the same sitting (`docs/fixes/FX04-append-only-history.md:78-81`),
on Louis's go. It removes known limitations 9 (FX4) and 5 (F42) (`TODO.md:400-405`, `:388`). It is done only when Louis
has written its learning write-up himself, without the code open (part 9).

**FX4 Append-only history, first** (skills 2, 3 and 9, where FX4 is *Planned*, `docs/GOAL.md:23,24,30`, and skill 4's
frozen tool list, `:25`; ADR 0008). Each conversation keeps the system prompt and the tool list it opened with, the
loop's final request included. A skill reload, an approved or rejected tool and a tool the model creates reach new
chats only. FX4 leaves *Later* and opens this phase because two later items rest on it: Phase 8's MCP packs (every chat
keeps the tool list it opened with, ADR 0006 and 0008) and F43's durable sessions (a chat resumed after a restart must
send what it opened with). Anthropic's extended-thinking docs state the rule for preserved thinking: "freeze system at
session start" and "leave the tools array as you first sent it" (as the engine research quotes them; not re-read
here). FX4's own doc cites the preserved-thinking page, which counts "Add, remove, rename, or edit a tool in `tools`"
as an edit that invalidates later thinking (`docs/fixes/FX04-append-only-history.md:18-19`).

- **Today in the code.** `BeginTurn` rewrites `history[0]` when the skill documentation changes
  (`louis-agent.core/Agent/AgentEngine.cs:257-259`) and hands every request the engine's live list (`:268`,
  `chatOptions.Tools = _registry.Live`; `louis-agent.core/Agent/ToolRegistry.cs:22-26`). FX1's final request sends the
  live list too (`louis-agent.core/Agent/ToolCallGuards.cs:85`). Since F5 an engine is built per toolset list, and up
  to 16 are cached and shared by every chat with that list (`louis-agent.core/Hosting/AgentEngineFactory.cs:34`), so
  one chat's new tool changes every open chat with the same toolsets (`TODO.md:403-404`). FX4's doc predates that: its
  cause table still says the API has "one engine for all sessions" (`docs/fixes/FX04-append-only-history.md:28`), so
  its plan starts by bringing the cause table up to date. From Phase 1's FX20, approving a skill file reloads the
  skills too, one more way to change `history[0]` mid-chat; until FX4, known limitation 9 holds, and a new chat is
  started after `/approve`.
- **The fix, as its doc has it** (`:32-57`). A conversation keeps a copy of its tool list from its first request, and
  `CreateTool`'s reply says the tool is "available in your next chat" (`:38`) instead of today's "You can call it now"
  (`louis-agent.core/Messages/ToolMessage.cs:711`). FX1's final-request middleware restores the conversation's list from
  a key in `ChatOptions.AdditionalProperties`, removed from a copy of the options before the provider sees it.
  `history[0]` is kept, and after a skill reload a short system message is appended (`:52`). F9 is designed append-only
  too (`:56`).
- **Proved free, by tests.** The doc's six tests (`:66-76`): `CreateTool` mid-message, `/approve` or `/reject`, another
  session's new tool, the round-limit final request (`AgentEngineRoundLimitTests`, extended), the key never reaching the
  provider, and a skill reload. One more, the engine research's measure: after each of those changes, the next request's
  system prompt and tools serialise byte-equal to the chat's first. The golden files stay byte-equal but for
  `CreateTool`'s reply, reviewed.
- **Confirmed once, paid.** The doc's hand check on Haiku 5.5 (`:78-81`) is a message in which the model creates a tool
  and keeps working, then `/approve`, `ReloadSkills` and a second open chat, each followed by a message, with no 400.
  It costs a few requests on the paid model, so it runs in the same sitting as this phase's injection simulations, on
  Louis's go. Its 400 has never been seen in a run: the doc's symptom is "expected from the code and Anthropic's docs;
  not yet seen in a run" (`:8`), and Haiku 5.5 enforces the check for accounts created on or after 2026-08-31, and for
  older accounts only when a request asks for it (`:12-13`). So the same sequence runs first on `main`: if it gives the
  400, the fix's "no 400" is the proof; if it doesn't, FX4's doc records that under *Confirm*, and the byte-equal test
  above is the proof.
- **Later options, as provider strategies (ADR 0003).** Anthropic's `tool_addition` and `tool_removal` blocks (beta
  `inline-tools-2026-09-15`), which FX4's doc calls "worth adding later for Anthropic, behind `LlmClientFactory`"
  (`:62`): that is *Later, after FX4* (a tool added mid-chat). And `prefix_mismatch_behavior: "drop_block"` (beta
  `thinking-binding-controls-2026-08-01`), which the doc keeps only as "a safety net, not a design", because Anthropic
  measured 4.6% to 67% more output tokens with it (`:63`). Both betas are named as the docs name them; neither was
  tried.

**Each tool declares its effect** (`ToolEffect`; skill 4's annotations, `docs/GOAL.md:25`, and skill 16). Read-only,
destructive, idempotent and open-world, declared on each tool, in place of the per-toolset `ParallelSafe`
(`louis-agent.core/Agent/ToolsetDefinition.cs:16-19,32`). It has four readers: parallel rounds (ADR 0026), F41's
approval per risk, F42's audit line, and the annotations our MCP server publishes (Phase 8). It lives in
`louis-agent.core/Tools/`, because `ArchitectureTests` put `Tools` at layer 2, below `Agent` at 3, and fail any `Tools`
file that uses `Agent/` (`tests/louis-agent.core.tests/ArchitectureTests.cs:12-20`); only the policy that reads it sits
in `Agent/`. Its ADR amends ADR 0026 (*Parallel tool calls for read-only toolsets*), whose status line then names it, as
ADR 0022's names 0029. A tool that declares nothing counts as destructive and open-world, the MCP specification's
defaults for a bare tool (Part 3, row 19). An MCP `ReadOnly` hint comes from `ToolEffect`, never from `ParallelSafe`. It
is the first part of F41's plan, because F41's policy is its first reader.

**A timeout per tool, beside its effect** (from Existo's per-tool timeouts, ADR 0030). Each tool declares how long it
may run next to the effect it declares, so a slow tool fails with its reason (ADR 0005) instead of holding the turn.
Today `ToolCallGuards` sets no timeout of its own: it catches a failed call and summarises an oversized result
(`louis-agent.core/Agent/ToolCallGuards.cs:38-69`). The timeouts that exist are written into each kind of call: a
skill's shell command stops after 30 seconds (`ShellTimeoutMs`, `louis-agent.core/Agent/Tools/SkillTools.cs:22,242,282`);
an agent-built script tool after its own `TimeoutSeconds`, 60 by default
(`louis-agent.core/Tools/Scripts/ScriptToolDefinition.cs:18-30`, `louis-agent.core/Agent/Tools/AgentBuiltTools.cs:200,210`);
the bash and PowerShell tools after a `timeoutSeconds` argument the model passes, 60 by default and at most 600
(`louis-agent.core/Tools/Shell/BashTools.cs:39,54`, `PowerShellTools.cs:53,69`); git calls report their own timeout
(`louis-agent.core/Tools/Git/GitTools.cs:333,367`); and some HTTP clients set theirs (Paymo 10 seconds, DevOps 30, Rider's
MCP client 30: `PaymoTools.cs:24`, `DevOpsTools.cs:32`, `RiderMcpClient.cs:23,29`). Other tools have none. Whether the
guard enforces the declared timeout for every tool, and what a tool that declares none gets, is F41's plan's to decide
(*inferred*).

**F41 Drafts and approvals** (skills 5 and 16; OWASP LLM06:2025, excessive agency). For an action that changes
something outside the workspace, the agent proposes a typed draft. Code validates it and returns every problem at once,
with the valid options (skill 4's "errors that list every problem at once with the valid options", `docs/GOAL.md:25`,
and skill 5's drafts, `:26`); the model retrying on them is an evaluator-optimizer loop with code as the evaluator.
Nothing is applied until a person approves it in the control room.

- **Approval policies per action and risk level** (skill 16's *Missing* item, `docs/GOAL.md:37`): Auto, Ask or Refuse
  per effect and per identity (the engine research's #13). Reading needs none, writing in the workspace follows the
  session's setting, and anything outside it asks. An ask goes through Microsoft.Extensions.AI's
  `ApprovalRequiredAIFunction` and `ToolApprovalRequestContent`, streamed to every host, the same content Phase 5's
  budgets that ask use, so nothing is hand-built for it. The shell toolsets, which run any code
  (`louis-agent.core/Agent/ToolsetCatalog.cs:46-63`), get their effect and so their policy here too.
- **Orange packs ask by default.** Every tool of someone else's MCP server counts as an action outside the workspace, so
  it asks. Phase 8 feeds this policy: a server's annotations can only tighten it, except `readOnlyHint` from a server
  named as trusted in settings, which may relax it (Phase 8's *Trust*; the MCP specification: annotations are untrusted
  unless the server is trusted).
- **One place to approve.** What waits for approval today moves to the control room: agent-built tools (`/approve`,
  `/reject`, `louis-agent.core/Agent/Tools/AgentBuiltTools.cs:12-13`) and, from Phase 1's FX20, the skill files the
  agent writes. The control room also gains a skills page listing the agent's own skills (`Skills/agent/functions/`)
  and the built-in ones (core's and each pack's `Prompts/`), the other half of `TODO.md:245`.
- **The invoice simulation's match becomes a draft**, and the simulation grades how often the first draft is valid.
  The same policy refuses the demo's writes in code (Phase 4's *Risks*).

**F42 Safety and audit** (skill 15; OWASP LLM01, LLM02, LLM05, LLM06, LLM10).

- **One untrusted-content frame** (Part 3, row 21): a random boundary per call, the source, `as_of` and a length cap, in
  one `ToolMessage` member, around web pages, shops, sub-session answers and MCP results. Today the web fence is written
  inline in C# (`louis-agent.core/Tools/Web/WebTools.cs:140,142`) and pinned by no golden, the shops' fence is already a
  message member (`ToolMessage.cs:285-286,289`), and a sub-session's answer goes back unfenced (`:768-769`). The inline
  fence becomes message members (ADR 0009), so the golden pins it. A fixed fence can be closed by a page that contains
  its end line, and a random boundary per call can't (*inferred*; OWASP LLM01:2025 asks for external content to be
  segregated). `SourcedFact` (source, URL, `as_of`, terms) waits for *Sources as data* on Puls's track, when a second
  source needs it.
- **An audit log** of every action with an effect (who, which customer, which tool, its arguments, the result, the
  approval), append-only, beside the ledger. It reads `ToolEffect`, so a read-only call writes no audit line (the plan
  decides; *inferred* from row 19).
- **Rate limits** per key and per customer, with ASP.NET Core's rate limiter (`Microsoft.AspNetCore.RateLimiting`,
  partitioned per key, in memory per instance): the host's half of OWASP LLM10:2025 (unbounded consumption), whose
  prevention list includes rate limits and user quotas. Phase 5's budgets are the engine's half.
- **Abuse handling and moderation** (`TODO.md:150`): a moderation check on what users send and on what the agent
  answers, and a block per key or per customer. With it, the **output guardrails** (`TODO.md:356`): nothing private in an
  answer or in an image address, beside the rule both web apps already have (an image a model writes loads only over
  https from a shop's image host, F33, `docs/GOAL.md:36`).
- **The egress proxy for the browser**: a name resolved once and internal addresses refused, so DNS rebinding and a
  Worker's WebSocket can't reach our own services (all three are skill 15's *Missing* items, `docs/GOAL.md:36`). Today
  the only rule is the one written by hand (`louis-agent.core/Guardrails/SsrfGuard.cs:29-34,67-70`), which Chrome
  resolves again after it; whether the fetch tools also move behind the proxy is F42's plan's to decide (the MCP
  security best practices prefer a proxy to address checks written by hand, Part 3, row 22). Phase 8's HTTP MCP servers
  go through the proxy too.
- **A suite of prompt-injection simulations**: an injected web page, document and tool result (and, from Phase 8, an
  MCP result), graded in code: no forbidden tool call, nothing private in the answer or in an image address. It doubles
  as the red-team suite (`TODO.md:356`), and Phase 9's pipeline runs it before a release. Paid, on Louis's go.
- **Repository mounts and branch protection** where the agent can push, and merges recorded under the person who
  clicked.
- **A sandboxed runtime for agent-written skills, weighed** (from Existo, which runs skills created at run time in
  TypeScript with Deno and in Python with uv, ADR 0030). From Phase 1's FX20 a skill file the agent writes waits for
  approval, but once approved it runs its bash, PowerShell or Python with the agent's own rights. F42's plan weighs
  running those skills, and the agent-built tools, in a runtime that grants only what a skill asks for: Deno's
  permission model (no file, network or environment access unless granted) for TypeScript, uv for Python (an isolated
  environment per script, which pins its packages but is no sandbox by itself), or a container per call. *Inferred*,
  and to be decided in F42's plan, with what each costs in start-up time.
- **`IncludeDetailedErrors = true` reviewed before a public Puls** (`AgentEngine.cs:125`). In process the model sees a
  thrown exception's message; over MCP the SDK replaces it unless it is an `McpException` (the MCP check's reading of
  `McpServerImpl.cs:1699-1729`). An exception's message can carry a path or a setting into the model's context and so
  into an answer (*inferred*), so the review decides which failures reach the model as their reason (ADR 0005) and which
  only the log.
- **Already elsewhere:** the place and the user's text are taken out of the logs in Phase 3, step 4f (Part 3, row 14);
  a retention setting and a delete path come with F43's accounts.

**Recovery, audited and approved** (Phase 6's recovery contract, B5a). F42's audit log takes in Phase 6's recovery
records: each `RecoveryRecord` becomes an audit line with who ran the workflow, the tenant once F43 exists, the
`RecoveryPolicy` version and any person's approval, and `recovery.jsonl` goes. A recovery that would change something
outside the app (a `Compensate`, or a retry after a side effect) is never run by the workflow on its own: it is a draft
a person approves under F41, by the effect its tool declares (`ToolEffect`), and the approver's name fills the record's
`ApprovedBy`. A recovery that only reads (a refetch, a quarantine inside the app) needs no approval, as reading needs
none.

One invoice match, drafted, checked, approved and audited (the draft tool's name is F41's plan's to choose):

![One invoice match: the draft is checked until valid, a person approves the write, and the approval is audited beside the ledger](diagrams/phase-7-checked-and-guarded-phase-7-checked-and-guarded-fx4-append-only-history-f41-drafts-and-approvals-f42-safety-and-audit.svg)

**Its plans start from these parts**, in order (one commit each, test first, stop after each):

| # | Part | Done when |
|---|---|---|
| 1 | **FX4**, in its own plan: the tool list frozen per conversation and `CreateTool`'s new reply; the final request carrying the frozen list; `history[0]` kept and a reload appended; the fix doc's cause table brought up to date | the doc's six tests and the byte-equal test above; the golden files byte-equal but for `CreateTool`'s reply (reviewed); FX4 closed as *Finishing a fix* says (`docs/fixes/README.md:42`), its hand confirm left for part 8 |
| 2 | **`ToolEffect` on every tool**, its ADR amending ADR 0026; parallel rounds read it, and `ParallelSafe` goes | a convention test: every tool in core and in every pack declares an effect; ADR 0026's parallel-round tests green without `ParallelSafe`; the golden files byte-equal |
| 3 | **F41: the approval policy** (Auto, Ask or Refuse per effect and per identity) through `ApprovalRequiredAIFunction` and `ToolApprovalRequestContent`, shown by every host (the API streams it; how the CLI and Rider ask is the plan's to name) | a fake client's call to an Ask tool waits for the person and runs once after approval; a Refuse tool returns its reason as a message member; a write by the demo's clerk is refused in code; an orange-pack tool with no settings asks |
| 4 | **F41: typed drafts and one place to approve**: the invoice match as a draft, validated in code; the control room's approvals page (drafts, agent-built tools, FX20's skill files) and its skills page | a draft with three problems returns all three, with the valid options; approving from the page does what `/approve` does today; the invoice simulation, on recorded transcripts, reports the share of first drafts that are valid |
| 5 | **F42: the untrusted-content frame** for web pages, shops, sub-session answers and (from Phase 8) MCP results | a page that contains a fence's text can't close the frame; a sub-session's answer arrives framed; `ToolMessageSnapshot.json` shows the new members and loses the inline fence (reviewed) |
| 6 | **F42: the audit log and rate limits**; Phase 6's recovery records move into the audit log, and a recovery with an effect outside the app becomes a draft approved under F41 | every call with an effect writes one append-only line, a read-only call none; the limit per key returns 429 with its reason, and one key's burst leaves another key's requests served; a recovery record becomes an audit line with who, the policy version and the approval, and a `Compensate` waits for a person and runs once after approval |
| 7 | **F42: moderation and output guardrails, the egress proxy, repository mounts and branch protection; `IncludeDetailedErrors` reviewed** | a flagged message is refused with its reason; Chrome reaches a public address and is refused a private one, a name re-resolved to an internal address included; the review's outcome in F42's doc |
| 8 | **F42: the injection simulations**, and FX4's hand confirm in the same sitting | graded in code on recorded transcripts first (free); then **the paid run, on Louis's go**: no forbidden tool call, nothing private in an answer or an image address; FX4's confirm: the 400 seen on `main` first, then none on the fix (or, if `main` gives none, that recorded in FX4's doc) |
| 9 | **Docs and the learning write-ups**, written by Louis himself without the code open: skill 16's new `docs/learning/16-human-in-the-loop.md` (approval per effect, drafts, the control room); skill 15's `docs/learning/15-safety-and-security.md` (written in Phase 1) gains the frame, the audit log, the rate limits, the egress proxy, the injection results and Phase 3's hashed logs (step 4f); skill 5's write-up from Phase 6 (`docs/learning/05-structured-outputs.md`, named by the index's rule, `docs/learning/README.md:3-5`) gains drafts. FX4's lesson stays in its fix doc's *Lesson* (`docs/fixes/FX04-append-only-history.md:83-86`) and opens Phase 9's skill-9 write-up. F41 and F42 closed as *Finishing a feature* says (`docs/features/README.md:124`); limitations 5 and 9 deleted from the TODO | Louis's write-ups written; links checked. The phase is done only then; if he can't explain a part, that part goes back |

**Verification.** Every part but part 8 is free: fakes, recorded transcripts, the touched projects' tests and the golden
files (the frame's and the approvals' texts are message members, so they show in `ToolMessageSnapshot.json` and are
reviewed). Paid: the injection simulations and FX4's hand confirm, in one sitting, on Louis's go.

