# Changelog

> **Reference.** What was finished, newest first, one entry per feature, fix or cleanup. **Elsewhere:** what's still
> open in the [TODO](TODO.md); how each was built in its feature, fix or plan doc (linked from its entry).

- 2026-10-15 — **Licence: AGPL-3.0** (was MIT): still free to use, change and share; whoever changes it and runs it as a
  service must publish the changes, so a hosted Puls's improvements stay open ([ADR 0015](docs/adr/0015-licence-agpl.md))
- 2026-10-14 — **F5 follow-up, sub-session usage rolled up**: a delegated answer's usage line, the chat total, `/usage`
  and `GET /sessions/{id}` include the specialist's requests, tokens and cost, as one `sub_session` record per reply
  (with `requests`, the model requests it stands for) that is also a row in the parent's ledger; ledger reports list
  those rows without totalling them, since the specialist's own rows are in the ledger it ran on
  ([plan](docs/plans/F05-usage-rollup.md))
- 2026-10-14 — **FX9 Cancelled requests in the ledger**: a non-streaming request that times out or is cancelled (the
  orchestrator's triage) is written to the ledger as `cancelled` with unknown tokens, as a cancelled stream already
  was; provider errors stay unrecorded ([fix](docs/fixes/FX09-cancelled-request-usage.md))
- 2026-10-14 — **CL7 Docs tidy**: the top of `docs/` keeps the eight docs most readers need. The session picture moves
  to `docs/specs/images/` (the 43 KB SVG; the 1.1 MB PNG of it goes), How a turn works joins arc42's runtime view, and
  the runbooks move beside the orchestrator's project doc; every link repointed ([plan](docs/plans/CL7-docs-tidy.md))
- 2026-10-14 — **Test first**: a step that changes behaviour starts with its tests, run and seen failing, then the
  code; one commit with both, the red output in the step's explanation ([ADR 0014](docs/adr/0014-test-first.md))
- 2026-10-14 — **CL6 arc42, ADRs and C4**: the architecture in arc42's twelve sections in `docs/architecture/`
  (ARCHITECTURE.md's text moved unchanged, plus goals, constraints, C4 context, container and component diagrams,
  quality scenarios and a glossary), and 13 decisions as ADRs in `docs/adr/`, each written from where it was
  recorded and linking its sources; the guide's rules link their ADRs, and a plan that makes a lasting decision adds
  one ([plan](docs/plans/CL6-arc42.md))
- 2026-10-14 — **CL5 Guides by folder**: the development guide, which loads into every Claude Code session, keeps what
  every task needs (~11.4k → ~6.2k tokens); core's how-tos move to `louis-agent.core/CLAUDE.md` (~4.8k), the tests' to
  `tests/CLAUDE.md`, the hosts' to `src/CLAUDE.md`, which Claude Code loads only when working there (checked with
  `/context` in a fresh session). None in `Skills/`, which core embeds and reads as personalities
  ([plan](docs/plans/CL5-guides-by-folder.md))
- 2026-10-14 — **CL4 One type per doc**: every doc is one Diátaxis type (tutorial, how-to, reference, explanation;
  plans a fifth kind), named in the docs index and in a quoted opening line. Six mixed docs split, moving facts, not
  dropping them: the README 230 → 152 lines, the TODO's Done to this file and its ticked lines removed, the benchmark
  runs to `docs/BENCHMARKS.md`, Architecture's settings to SETUP and MODELS, How a turn works' how-tos to the guide.
  Five out-of-date facts found on the way were fixed in their own commits (the default model, tool registration since
  F5, F5's status, deleting `personality.md`, "no shell"); a link check ran after every step
  ([plan](docs/plans/CL4-doc-types.md))
- 2026-10-14 — **F5 Toolset profiles**: a session names its toolsets when it opens and keeps them; an engine per
  toolset list with only those tools and their guides; sub-sessions on a peer (`StartSubSession`), which Puls's
  delegation runs on. Short benchmark: prefix −50% (~48.5k → ~24.4k tokens), run −23%, 4/4 fixes, the cache still hits
  ([numbers](docs/BENCHMARKS.md#f5-benchmark-short-run-2026-10-13)); the full 10-fix run waived by Louis
  ([F5](docs/features/F05-toolset-profiles.md))
- 2026-10-14 — **F20 Puls look**: a landing page and a dark three-panel dashboard in Puls's colours, with the Werk panel showing each delegation live (specialist, steps, sources, cost); checked in a real browser with a live delegation ([F20](docs/features/F20-puls-look.md)).
- 2026-10-14 — **FX14 Sub-session prompt budget**: `SUB_SESSION_PROMPT_BUDGET` (80k, under the 100k price step) stops a specialist's prompt growing into long-prompt prices; it answers from what it has ([FX14](docs/fixes/FX14-sub-session-prompt-budget.md)).
- 2026-10-14 — **FX13 Counting to the analyst**: Puls's router sends counting to `files` + a script, and the reader hands counting back instead of reading every file ([FX13](docs/fixes/FX13-counting-to-the-analyst.md)).
- 2026-10-14 — **FX12 Env templates readable**: `.env.example`, `.sample` and `.template` files are no longer treated as secrets ([FX12](docs/fixes/FX12-env-templates-readable.md)).
- 2026-10-14 — **FX11 Empty sub-session answer**: a specialist that ends with no text (its 10 rounds used) is asked once more in the same session instead of Puls starting over ([FX11](docs/fixes/FX11-empty-sub-session-answer.md)).
- 2026-10-14 — **FX10 Optional peer name**: `StartSubSession` without `agent` uses the only peer; Puls's calls without it were refused ([FX10](docs/fixes/FX10-optional-peer-name.md)).
- 2026-10-12 — **Ollama on the GPU and local triage**: `ollama-nvidia` and `ollama-amd` services sized for a 4 GB card
  (32k context, flash attention, 8-bit KV cache); Ollama's client waits 10 minutes, not 100 seconds. The short demo's
  triage on `qwen3:4b-instruct-2507` (profile `.env.ollama-triage`): 4 fixed, 1 escalated, as on Claude, triage $0, the
  run $0.042 against $0.049, 2.4× slower; llama3.1 and qwen3's thinking variant failed
  ([Local triage](docs/BENCHMARKS.md#local-triage-2026-10-12),
  `samples/sample-output-short-local-triage/`)
- 2026-10-12 — **CL3 Messages and logging**: every exception, log line, tool error and user text is a message enum
  member with a `[Description]` template (486: 313 in core, the rest in each host's own `Messages/`), proved to render
  as before; `ToolMessageSnapshot.json` pins the 190 texts tools return to the model. Logging is `ILogger` with
  Serilog: stderr only, the run file and the JSON-lines records as sinks, holes kept as fields; `AgentLog` is gone.
  The orchestrator logs to stderr too, and `decisions.jsonl` is snake_case like its other records
- 2026-10-12 — **CL2 Split AgentEngine**: 1,078 lines become the engine (the turn loop, 360) and `ToolRegistry`,
  `ToolCallGuards`, `SkillTools`, `AgentBuiltTools` in `Agent/`; pure checks in `Guardrails/` (`WorkspaceBoundary`,
  `SensitiveFiles`, `SsrfGuard`); the console chat in the CLI. A golden file pins what the model is sent; it found that
  the prompt and two descriptions took the checkout's line endings, now always LF
- 2026-10-11 — **CL1 Project layout**: core in folders by concern (`Agent`, `Llm/{Name}Provider`, `Tools/{Toolset}`,
  `Skills`, `Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`, `Config`, `Messages`, `Logging`), the namespace the folder
  path, one type per file; PascalCase namespaces in every project; the tests mirror the source; `ArchitectureTests`
  fails on a namespace off its folder, two types in a file or a dependency against the layers. Behaviour unchanged:
  the same 779 tests pass. Moved with `git mv`, so history follows
- 2026-10-11 — **F4 Prompt caching**: two cache markers on Anthropic (the system prompt with `LLM_CACHE_TTL`, the
  conversation by automatic caching, 5 minutes), as a provider strategy; tools sorted; writes priced at their lifetime
  (`cache_write_1h`); `usage-probe --cache-gaps` recommends the lifetime from the ledger. Full demo $0.13 against M1's
  $0.66 (−80%), louis-agent's input cost −87%, 10/10 fixes; samples in `samples/sample-output-cached/`
- 2026-10-11 — **F14 Display currency**: costs in the PC's currency in every host (rands from `HOST_CULTURE=en-ZA`,
  written by `usage-probe --host-culture`; `R0.034`, `.` decimals), at a dated rate (Frankfurter mapped into our
  `rates.json`, `config/rates.json` as the fallback, served at `GET /rates`), with the rate and the USD charged beside
  converted totals; a currency picker in the web app. The ledger stays in USD. Both demo samples rerun in rands

- 2026-10-11 — [FX8](docs/fixes/FX08-fix-result-quotes.md): a FIX-RESULT whose summary quotes without escaping
  ("Cher") is still read, instead of asking louis-agent to continue three times; found by F14's full demo rerun

- 2026-10-11 — **F3 Show usage** (M1 done): every answer says what it used and cost — a line on the CLI's stderr,
  under each answer in the web app (with the chat's total), in Rider with `USAGE_IN_CHAT=true` — `/usage` in every
  host, `usage` on the API's `message_delta` and `GET /sessions/{id}`, and cost per reply, per error and per run in the
  orchestrator's comms log. The short demo's comms log total equals its ledger's exactly ($0.2526, 57 requests)
- 2026-10-11 — [FX7](docs/fixes/FX07-timeout-ends-run.md): a timed-out request fails that error, not the
  orchestrator's whole run; found with the new `run-demo.ps1 -OrchestratorProfile`

- 2026-10-11 — [FX6](docs/fixes/FX06-usage-on-failed-turns.md): a failed louis-agent turn still reports its usage (on
  the API's `error` event), so the web app's chat total and the orchestrator's comms log match the ledger; built in F3

- 2026-10-11 — [FX5](docs/fixes/FX05-null-conditional-ledger.md): a `?.` in F3's first step skipped the ledger write for
  requests outside a usage scope; F1's tests caught it before the commit

- 2026-10-10 — **F2 Prices and cost**: every ledger record priced from a tracked `config/prices.json` (Haiku 5.5's
  long-prompt tier included), or from the API's `GET /prices` with `PRICES_URL`; `usage-probe -- --ledger` totals a
  ledger; M1 baseline $0.6586 for the full demo ($0.0625 a fix), both sample runs rerun with priced ledgers
- 2026-10-10 — Full demo on Claude Haiku 5.5 with the usage ledger: 10/10 fixes confirmed, 2 ignored, 1 escalated, ≈ $0.66
  (`samples/sample-output/`, now with `usage-2026-10.jsonl`); F2 prices that ledger for the M1 baseline
- 2026-10-10 — [FX1](docs/fixes/FX01-round-limit-tools.md): the engine keeps the tools on the request after the
  10-round limit (`tool_choice: none`), which Haiku 5.5 otherwise rejects; the short demo went from 2/4 to 4/4 fixes
- 2026-10-10 — **F1 Usage ledger**: every model request of every host in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, with
  host, session, turn, purpose and the orchestrator's fix and run; `tools/louis-agent.usage-probe`; a short demo
  (`run-demo.ps1 -Short`) saved with its ledger in `samples/sample-output-short/` (4/4 fixes, ≈ $0.27 on Haiku 5.5)
- 2026-10-10 — Default Anthropic model is Claude Haiku 5.5 (about 7.5× cheaper than Haiku 4.5; usage, thinking and
  thinking text checked with `tools/louis-agent.usage-probe`); LLM profiles `config/.env.anthropic` / `.env.ollama`
- 2026-10-07 — Specs and feature plan for usage, budgets and cheaper responses (`docs/specs/`, `docs/features/`); the
  session-architecture and orchestrator designs moved from this list into `docs/specs/`
- 2026-10-07 — Docs: [how the agents and endpoints talk](docs/AGENT_COMMUNICATION.md), [runbooks](docs/projects/orchestrator-runbooks.md), a
  docs index in reading order
- 2026-10-07 — Web app Branches tab: merges and deletes are queued instead of silently dropped while another runs
- 2026-10-07 — Comms log keeps louis-agent's text blocks apart
- 2026-10-06 — Orchestrator demo in Docker: ten bugs → ten fix branches, Markdown comms log, sample output with a
  recording of the merges
- 2026-10-06 — Web app Branches tab and history endpoints: all commits, branch and commit diffs, merge, delete merged
- 2026-10-06 — Root `.dockerignore`: builds no longer send `config/.env.secrets` or `.git` into the build context
- 2026-10-06 — Orchestrator POC: runbook per API method, fixes delegated to louis-agent.api and verified; `AgentEngine`
  `toolsets:` option
- 2026-10-06 — MCP server: Streamable HTTP transport (`MCP_TRANSPORT=http`) with its own `MCP_API_KEY`
- Earlier — Skill files standardised (titles, tool tables, Python fences); DevOps blockers query fixed (tag, not type);
  sensitive-file checks added to `DeleteDirectory` and `CopyFile`; all 108 tool descriptions rewritten
