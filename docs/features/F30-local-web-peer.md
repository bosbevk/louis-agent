# F30 · Local web peer: a free local model looks up prices for Puls

> **Status:** planned (2026-10-15), on `feature/F30-local-web-peer` · **Milestone:** — · **Depends on:** F5
> (sub-sessions), F21 (roles), F25 (the cook's price work and check), F29 (the role gate)
>
> **In the TODO:** **F30 Local web peer** under *Now*, beside F25 ([TODO](../../TODO.md)) · **Plan:**
> [plans/F30](../plans/F30-local-web-peer.md)

Puls reasons on Claude Haiku, and every page it reads for a price is paid for again on each later request. A local
model on Ollama costs nothing per token. Louis's question (2026-10-15): can a local model do the searching and page
reading for prices, and hand Puls a short report it can check, so an answer costs less with its quality held?

## User stories

**F30-S1 — A session names its model.** As an *operator*, I want a session to say which LLM profile it runs on, so
one instance serves Haiku to the cook and a local model to a price lookup.
- *Given* `LLM_SESSION_PROFILES=ollama-puls` *when* a session opens with `"llm": "ollama-puls"` *then* its requests go
  to that profile's model, and the session keeps it.
- *Given* a profile that isn't listed *then* it's refused with a 400 before any engine is built, naming the ones
  there are.
- *Given* no `llm` *then* the session runs on the instance's own model, as today.

**F30-S2 — Puls hands price lookups to the local model.** As a *user*, I want a price question answered with each
item's shop, rand price, date and link, looked up by the local model, so it costs less.
- *Given* a price question *then* Puls hands it to a specialist (the researcher), which hands each shop page to a
  minion with `FetchUrl`.
- *Given* a meal question *then* the cook (on Haiku) hands each shop page to a **minion**: one task, one tool from the
  cook's own toolbox (`FetchUrl`), in the role `puls-minion-web-price-lookup`, on the model `MINION_LLMS` names for that role
  (`ollama-puls`; Haiku if needed).
- *Given* a price the minion couldn't find *then* it says `unknown`, never a guess.

**F30-S3 — A price is used only when its quote checks out.** As a *user*, I want every price Puls shows to be on the
page it links to, so a small model's mistake doesn't reach me.
- *Given* a minion's quote for a price *when* the specialist calls `VerifyQuote(url, quote)` *then* it learns whether those words
  are on the page, without a model call.
- *Given* a quote that isn't on the page, or a page that can't be read *then* that price is shown as
  `unconfirmed`, with why.

**F30-S4 — Measured.** As *Louis*, I want five price questions and the cook check run with and without minions, so I
keep minions only if real prices hold and the cost drops.

## Design notes

- **Per session, not per container.** `AgentEngineFactory` already keys an engine by toolsets, personality and
  router; the profile joins the key, and `AgentHost.Build` makes one client per allowed profile through the same
  `ILlmClientFactory` (the provider strategies stay in `LlmClientFactory`). A profile file is read into a dictionary
  for `LlmOptions.FromEnvironment(getVariable)`, so the process environment never changes.
- **Ollama runs natively on Windows** for the RX 7600 (Docker Desktop doesn't pass AMD GPUs to containers); the
  containers reach it at `host.docker.internal:11434`.
- **The quote check is code, not a model:** fetched through the SSRF-guarded path, compared with whitespace collapsed
  and case folded.
- **The cook stays on Haiku;** what moves to the local model is reading pages, through minions (level 2): one task
  and one tool handed down from the specialist's own toolbox, on the model `MINION_LLMS` names for its role.

## Done when

- A Puls chat's cook and researcher hand shop pages to minions on `ollama-puls`, and the ledger shows the qwen model
  at $0 under the specialist's turn.
- `VerifyQuote` is tested for present, absent, whitespace and case, unreadable and blocked pages.
- The five-question run is in the admin portal's store, run from the portal ([F31](F31-benchmarks-portal.md)), and the
  decision to keep or drop the peer is written here.

## What the build changed

- **A profile takes nothing from the instance but its API key** (step 1). The first idea was to fall back to the
  process environment for whatever a profile file leaves out; that would hand the local model Claude's
  `LLM_THINKING`. Only `LLM_API_KEY`, `ANTHROPIC_API_KEY` and `ANTHROPIC_WORKSPACE_ID` fall back (they belong in
  `.env.secrets`, never in a profile file); anything else takes its default.
- **The profile goes to the peer as data, and the peer decides** (step 2). `StartSubSession(…, llm)` sends the name in
  the session's body, trimmed; the peer refuses one it doesn't offer with the same 400 as an unknown personality, so the
  parent never needs to know which models the peer has. `GET /llm-profiles` lists them, beside `/personalities`.
- **32k fits on 8 GB only with flash attention and an 8-bit cache** (step 3). At Ollama's full-precision cache the
  model and a 32k context took 7.7 GB and ran 7% on the CPU; with `OLLAMA_FLASH_ATTENTION=1` and
  `OLLAMA_KV_CACHE_TYPE=q8_0` they take 5.3 GB, all on the GPU (MODELS.md, *Native Ollama on Windows*).
- **The local model reads English, not Afrikaans** (step 3). Asked in Afrikaans for "drie groente in een sin", it took
  *sin* for an orange and answered in Dutch over 600 tokens. The role is written in English and Puls hands it the task
  in English; Puls writes the Afrikaans answer.
- **Minions: a second level, by capability** (step 3, Louis). Drawing the design showed the cook, which prices most
  answers, couldn't reach the local model: sub-sessions were one level deep. Now a specialist may hand one task and one
  tool from its own toolbox to a minion (level 2), which never delegates; each minion has a role (`puls-minion-*`, one function), and its model is configuration per role
  (`MINION_LLMS`: the local model, or Haiku if needed). A per-role rule (`SUB_SESSION_DELEGATION`) was drawn first and
  dropped. The plan's *Approved during the build* has the steps and both diagrams.
