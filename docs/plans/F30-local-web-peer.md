# F30 · Local web peer: plan

> Approved 2026-10-15 · Feature: [F30 Local web peer](../features/F30-local-web-peer.md) · Branch:
> `feature/F30-local-web-peer`, off `main` after #45 (F25–F29)

## Context

Louis's draft (2026-10-15): Puls (Claude Haiku) keeps the reasoning; a local model on Ollama does the web searching
and page reading for prices, and returns a short, checkable report. Measure whether it cuts cost per answer with
quality held. GOAL.md skills 1 (model choice by measured quality and cost), 10 (delegation), 12 (evaluation), 13 (cost).

What changed from the draft, from the code and Louis's answers:
- **F30, not F25** (F25 is the cook), on its own branch off `main` once F25–F29 were merged.
- **The model is chosen per session, not per container** (Louis: "can we switch it on session?"). `puls_agent` keeps
  Haiku for the cook and serves the local model to a sub-session that asks for it. The draft's step 1 (puls-agent on
  `LLM_PROFILE=ollama`) would have moved the cook to Ollama too.
- **Price questions only.** Meals stay with the cook on Haiku (it can't call another specialist: one level deep).
- **qwen3:4b-instruct-2507** (the only local model that made real tool calls, BENCHMARKS.md *Local triage*), on
  Louis's **RX 7600 (8 GB)** through **native Ollama for Windows**: Docker Desktop doesn't pass AMD GPUs to
  containers (`docs/MODELS.md`), and the existing endpoint `http://host.docker.internal:11434` reaches it. Model and a
  32k context fit in 8 GB, so it runs all-GPU. Everything else stays in Docker (Louis's choice); Ollama in Docker
  would be CPU only, ~1–5 minutes just to read a 10–20k-token request, over the 5-minute sub-session timeout.
- **The saving is small in rand** (a Haiku web answer is ~$0.01–0.03); what the run decides is whether a free local
  model is good enough for this task, and step 7 keeps it only if the numbers say so.
- **No `roles.json` change:** prices and research both use `web`, so the rule can't tell them apart; Puls names
  `puls-prices` itself.

## Steps

One commit each, test first, the review stop after each; the Puls image rebuilt after each code step.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | **A session's LLM profile, in core.** `LLM_SESSION_PROFILES` (e.g. `ollama-web`) lists the profiles a session may name; each read from `config/.env.{name}` into a dictionary and passed to `LlmOptions.FromEnvironment(getVariable)` (no change to the process environment). `AgentHost.Build` creates a client, usage mapper, tools flag and thinking per profile through the same `ILlmClientFactory`; `AgentEngineFactory.GetOrCreate(…, llm)` adds `\|llm:{name}` to its key. An unknown or unlisted name is refused before anything is built | `AgentEngineFactoryTests` first: two `FakeChatClient`s, a session naming the profile gets the second; unknown refused; the key separates them |
| 2 | **The API and `StartSubSession` pass it.** `POST /sessions` takes `"llm"` (kept by the session, like its toolsets; a 400 like an unknown personality); `GET /llm-profiles` lists them; `StartSubSession(…, llm)` sends it in the peer's body | api `AgentSessionsTests`, `AgentPeerToolsTests` (the stub sees `llm`); golden files |
| 3 | **Configuration:** `config/.env.ollama-web` (qwen3:4b-instruct-2507-q4_K_M, `host.docker.internal:11434`); `LLM_SESSION_PROFILES: ollama-web` on `puls-agent` in the Puls compose; MODELS.md: native Ollama on Windows for AMD, `OLLAMA_CONTEXT_LENGTH=32768`, the pull command | by hand: a session with `llm: ollama-web` and `web` answers a price; `ollama ps` shows 100% GPU; the ledger row names the qwen model at $0 |
| 4 | **The `puls-prices` role:** `Skills/personalities/puls-prices.md` on the shared `puls-*` base: per item shop, rand price, date, URL, and the exact words the price came from; `unknown`, never a guess; one sub-session for the whole list. Its row in `agents-skills.md` (`web`, `puls-prices`, `llm: ollama-web`); `puls-prices` in `PERSONALITIES_ENABLED` in the Puls compose (on for the measurement, F29's gate) | `PersonalitiesTests` first; golden files |
| 5 | **`VerifyQuote(url, quote)`** in `WebTools`: fetches through `GetFollowingRedirectsAsync` (the SSRF guard on every hop) and `HtmlToText`, compares with whitespace collapsed and case folded; answers found, not found, page empty (a JavaScript page), or failed with the reason. No model call; texts as `ToolMessage` members; a line in `web-skills.md` | `WebToolsTests` first on `StubHandler`: present, absent, different whitespace or case, page not loading, empty page, blocked address; golden files |
| 6 | **Puls uses a price only when its quote checks out:** a rule in `agents-skills.md`'s relay section (a verified price with its link; otherwise `unconfirmed`, with why) | skills test first; golden files |
| 7 | **Measure (Louis's go first; a few rand).** `run_check.py --prices`: five price questions for Stellenbosch, modes `haiku-web` (`web` only: Puls looks itself) and `haiku-local` (`agents`, `web`: delegates to `puls-prices` on `ollama-web`), as `--personalities` does. Per answer: real, dated prices over the total, quotes that failed `VerifyQuote`, cost from the ledger (the sub-session roll-up, F5; qwen priced 0), seconds. A portal check beside *Puls alleen* | `--report` on a saved run first (no model calls); then the paid run, its numbers in the feature doc and BENCHMARKS.md. Keep the local peer only if real prices hold and cost drops |

## Reused, not built

- `LlmOptions.FromEnvironment(getVariable)`, `ILlmClientFactory.Create` and `CreateUsageMapper` (step 1): no new
  provider code.
- `AgentEngineFactory`'s key and cache (step 1); `AgentPeerTools`' body and limits (step 2: 3 per turn, 5 minutes).
- `WebTools.GetFollowingRedirectsAsync`, `HtmlToText`, `SsrfGuard` (step 5).
- `run_check.py`'s modes, scoring and ledger summary, and the portal catalog's arguments (step 7).
- Considered and not used: a second peer container (Louis chose per session); a model per container (it moves the
  cook).

## Risks

- **A 4B model reading long pages:** FetchUrl returns up to 8,000 characters a page; the role asks for small
  `maxChars` and few pages. Step 3's hand check shows whether it calls tools well with the `web` guide.
- **The quote check fails on JavaScript pages** (empty text): those prices come back `unconfirmed`, which is honest
  but may lower the real-price count against Haiku, which reads Tavily's rendered text. Step 7 reports both.
- **F15 isn't built** (`num_ctx` per request): the context is the server's `OLLAMA_CONTEXT_LENGTH`.

## Verification

- `dotnet build louis-agent-solution.sln`; `dotnet test` on core and api for each step; the whole solution before the PR.
- Golden files regenerated and reviewed on steps 2, 4, 5 and 6.
- Steps 3 and 7 by hand and as a paid run, with Louis's go.

## Approved during the build
