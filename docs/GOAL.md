# Goal

**Become an AI engineer by building louis-agent: learn each AI-engineering skill hands-on, measure what it changes,
and be able to explain every design decision behind it.**

This is the one fixed aim. The plan to get there changes as I learn: the [TODO](../TODO.md) says *what's next*,
[features](features/README.md) say *how* and [specs](specs/) say *why*, and their targets live there, not here. This page
changes only when what I want to learn changes. (The same text, as plain text, is used as the coding coordinator's goal.)

## What I'm learning: the whole agent architecture

The map below covers every layer of a production AI agent, not just what the TODO plans today. For each skill it says
what louis-agent already has, what the TODO plans, and what is **missing**: a missing skill is still part of the goal,
and gets a line in the TODO's backlog (*Agent architecture gaps*) until it is planned. When the TODO changes, update the
*Planned* and *Missing* columns, not the skill.

| # | Skill | Have today | Planned (TODO) | Missing |
|---|---|---|---|---|
| 1 | **Models and providers** | One `IChatClient` over Anthropic and Ollama (`LlmClientFactory`, [MODELS](MODELS.md)) | Fallback model (F8), summary model per route (F10) | Model choice by measured quality and cost per task (routing on evals) |
| 2 | **Reasoning and extended thinking** | Thinking resolved per model (adaptive, or budget for pre-4.6), streamed to every host, kept in history across tool calls | Thinking level per route (F10) | Measuring what thinking buys: quality vs tokens per level on the evals; interleaved thinking between tool calls; when not to think |
| 3 | **Prompt design** | System prompt built from `Skills/*.md` | Cache-friendly prefix (F4) | Prompt versioning, and A/B comparison of prompt changes on the evals |
| 4 | **Tool design** | ~108 described tools, agent-built tools with approval, guarded results | Toolset profiles (F5), results as tool calls (F8) | Tool search / on-demand tool loading for large toolsets |
| 5 | **Structured outputs** | — | Results between agents as tool calls (F8) | Schema-validated outputs across the toolset |
| 6 | **The agent loop and planning** | Streaming tool loop, 10 rounds per message | Rounds per route (F10) | Explicit planning, self-check before answering, stop conditions beyond a round count |
| 7 | **Context engineering** | Oversized results summarised | Clear stale results, compaction (F9) | Context building per turn: retrieve, rank, budget (session architecture) |
| 8 | **Memory and retrieval (RAG)** | — | — (backlog: long-term memory) | Embeddings, a vector store, retrieval over code and past sessions, and measuring retrieval quality |
| 9 | **Sessions and state** | In-memory sessions | — (backlog: durable sessions) | Durable sessions, lifecycle states, resume after restart |
| 10 | **Multi-agent orchestration** | Orchestrator delegates fixes and verifies them | Build mode (Next) | Agent-to-agent handoff patterns compared (tool call vs session vs MCP); parallel sub-agents |
| 11 | **Protocols** | MCP server (stdio, HTTP), ACP for Rider | — | A full MCP client (use other servers' tools) |
| 12 | **Evaluation** | The orchestrator demo: 10 fixes, each verified | Benchmark with baseline (M1), long-chat recall check (F9) | An eval suite: task datasets, graded scoring (including LLM-as-judge), regression gates in CI |
| 13 | **Cost and token economics** | — | Ledger, prices, display, caching, estimates, budgets, reconciliation (M1–M5) | — |
| 14 | **Reliability** | Cut-off calls never run, failures carry reasons | Retries, account limits, fallback (F8) | Timeouts and idempotency for long tasks; resumable runs in the core |
| 15 | **Safety and security** | No shell for model text, sensitive-file blocking, SSRF-guarded fetch, tool approval | — (backlog: repo isolation, branch protection) | Prompt-injection defences and red-team tests, guardrails on outputs, rate limiting, an audit log |
| 16 | **Human in the loop** | Approve agent-built tools; a person merges branches | Budgets that ask (F7) | Approval policies per action and risk level |
| 17 | **Observability** | stderr and JSON-lines logs | Usage ledger and Usage tab (F1, F11) | Tracing per turn and tool call (OpenTelemetry), latency and error dashboards |
| 18 | **Testing and shipping** | Core and orchestrator unit tests, Docker images | — (backlog: CI, API/web/e2e tests) | CI, deploy pipeline, evals as a release gate |
| 19 | **Multimodal and computer use** | — | — | Images and documents as input; browser or computer-use tools |

A skill can be added, dropped or reworded at any time; that is a change to this page.

## A skill counts as learned when

1. **It's built.** Its *Missing* column is empty: the work that covers it is shipped and meets its *Done when* in the
   feature doc or TODO.
2. **It's measured.** Before and after numbers on the benchmark (see [features](features/README.md#the-benchmark)),
   with quality held: the targets themselves are set in the feature docs.
3. **It's written up.** A short write-up in [docs/learning/](learning/): what was measured before and after, what
   changed, the trade-offs, and what didn't work. Together the write-ups form a portfolio of AI-engineering practice.
4. **I can explain it.** The write-up answers *why this design and not the alternatives*, without needing the code open.

## How to work

- **Source of truth:** [TODO.md](../TODO.md) (what's next), [docs/features](features/README.md) (how),
  [docs/specs](specs/) (why). Don't duplicate them here; update them.
- **Learning over finishing:** when a choice is between done faster and understood better, prefer understood better,
  and say what was learned in the write-up.
- **One feature at a time**, in the TODO's order; one implementation step per commit, each with its test. Keep the
  existing suites green.
- **Measure, don't assume:** rerun the benchmark for every change that affects cost or quality, keep a change only if
  quality holds, and record the numbers.
- **Keep everything in step** when a feature finishes: follow
  [Finishing a feature](features/README.md#finishing-a-feature).
- **Ask first** before anything that spends real money (benchmark or demo runs against the API), before pushing, and
  on open design decisions listed in the TODO.
- **Provider-neutral core:** keep provider-specific code in `LlmClientFactory`.
- **Stop and report** rather than guess when a result can't be verified.

---
[Docs index](README.md) · [TODO](../TODO.md) · [Features](features/README.md)
