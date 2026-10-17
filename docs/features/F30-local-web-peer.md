# F30 · Local web peer: a free local model looks up prices for Puls

> **Status:** built, not in use (2026-10-17): no session is given `minions` until F31 measures it; on
> `feature/F30-local-web-peer` · **Milestone:** — · **Depends on:** F5
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
- `VerifyQuote` is tested for present, absent, whitespace and case, unreadable and blocked pages; a price is checked
  against its product ([FX16](../fixes/FX16-short-quote-match.md)).
- The five-question run is in the admin portal's store, run from the portal (F31, its plan on `docs/F31-benchmarks-portal-plan`), and the
  decision to keep or drop the peer is written here.

## What the build changed

- **Step 4 (2026-10-17):** a minion's engine is the principles, its product's base (`minions/puls-minion.md`), its role
  and one tool, no toolset guide and no router; every file in `minions/` but a base is a role at level 2 (a minion with
  no role is refused). **No `minions.json`** (Louis, reviewing the step: "the specialist must give him its tools"): the
  specialist's own toolbox is the only list, so a role is one file. `POST /sessions` takes `tool` at depth 2 only, with
  `parent`: the API finds the parent session on its own instance and refuses a tool the parent lacks or a delegation
  or self-extension tool, before anything is built. The minion's model
  is the caller's `llm` until step 5's `MINION_LLMS` maps it per role. Louis, reading the price role: the answer gets a
  `per_kg` line (the page's per-kilogram price, or worked out from a stated pack weight with the working shown) and a
  `location` line (the branch or delivery area the page's price holds for), since prices differ by area.
- **Step 5 (2026-10-17):** the `minions` toolset (`MinionTools`: `StartMinion(role, tool, task, stronger)`,
  `ContinueMinion`) on the specialist tools' child path, which `SpecialistTools` (renamed from `AgentPeerTools`, Louis:
  "just rename it") now exposes to both; a minion is refused before any HTTP for a tool the specialist doesn't hold
  (`AgentTools`, a per-turn ambient the API sets like the attachments), one never handed down, or a role that isn't a
  minion's. `MINION_LLMS` maps a role (or `*`) to a cascade of profiles, `instance` for the specialist's own model,
  checked at start-up; `stronger` takes the second model, once. Six minions per user turn, counted on Puls's turn tag so
  every specialist under it shares them; two minutes each. The minion's roll-up row (`minion:{role}`) lands in the
  specialist's turn, so the chat's roll-up carries it up. `toolsets/minions.md` is the guide; ADR 0021.
- **Step 6 (2026-10-17):** `puls-agent`'s only peer is itself (`AGENT_PEERS: puls-agent=http://puls-agent:8080`) and
  `MINION_LLMS` defaults to `*=ollama-puls>instance`; the price minion role is in `PERSONALITIES_ENABLED`'s default on
  both containers. Puls's routing and the specialists guide give the cook `web`, `minions`; the cook's and researcher's
  roles got a *Minions* section (one page, one of your tools, the seven-line answer; a price without a quote is
  unknown) and their "can't delegate" line now says a minion isn't delegation. The quote check itself is step 7's
  `VerifyQuote`; until step 8 the roles read the quote by eye.
- **Step 6b (2026-10-17, Louis: "we must also somehow display when a minion was called on the front end"):** a
  `StartMinion` or `ContinueMinion` call is a `Delegation` with `Minion` set (the role, the one tool, the model from
  the footer), so the answer's delegation card draws a minion card nested inside the specialist's, and the Werk panel
  shows the minion under the specialist's steps: role, tool and model chips, its task, its own step and its cost. The
  minion's stream events already arrived nested (`sub_session` inside `sub_session`); the fold now matches any
  delegation call, so they land on the minion's block. Checked by hand in step 8's run.
- **Step 7 (2026-10-17):** `VerifyQuote(url, quote)` in `WebTools`: the page through the same redirect-following,
  SSRF-guarded reader as `FetchUrl`, the quote compared with entities decoded, spacing (a non-breaking space too) and
  case ignored and its surrounding quotes dropped, against the readable text and the whole body (a price per kilo can
  sit outside `<main>`). An empty page is rendered once (`ReadRenderedPage`'s renderer, a credit) and never otherwise;
  no renderer means *Page empty*. Four answers, each a `ToolMessage`: found, not found (unconfirmed), page empty
  (unconfirmed), or the fetch's error.
- **Step 8 (2026-10-17):** the cook's and researcher's *Minions* sections check every quote with `VerifyQuote`
  before a price is used; *Not found* or *Page empty* gets one more try with `stronger: true`, checked the same way;
  still not found, the price is unconfirmed ("unknown" in the cook's list, named under *Unsure*). This replaces
  step 6's read-the-quote-by-eye rule.
- **The hand check (2026-10-17, Malmesbury breakfast):** no minion ran (the cook drew Pick n Pay and Spar, which its
  role priced from search results), and `VerifyQuote` answered *Not found* for three real Pick n Pay prices: the
  pages are JavaScript shells, and their menu and footer kept them from counting as empty. Fixed: a large body with
  under 300 characters of readable text is rendered; only minion quotes are checked; every shop's price goes through a
  minion, with the cap raised to 16.
- **The second hand check (2026-10-17):** five minions ran on qwen at $0, 14 to 28 seconds each, the ledger chained
  Puls → cook → minion and rolled each up into the cook's turn; but every Pick n Pay render failed, as the role had
  measured on 2026-10-15. Pick n Pay and Woolworths now get a minion with `WebSearch` (the price from the shop's
  search result, `source: search`, marked as the search index's, never checked against the page).
- **The third hand check (2026-10-17):** the Pick n Pay minions ran (3 to 13 seconds each, $0), but qwen3:4b set a
  freshness filter on its own searches (most came back empty), copied the answer template's `| unknown` into its
  lines, priced the wrong products (a combo deal for eggs, a 6 x 1 L pack for 1 L of milk) and wrote the same date,
  2023-10-05, on six answers. Louis: "make the minions use haiku5.5": `MINION_LLMS` defaults to `*=instance` on
  `puls-agent`, Claude Haiku 5.5 at $0.10 / $0.50 per million tokens, about a tenth of a cent a minion. The local
  model stays one setting away (`*=ollama-puls>instance`); with `instance` alone, `stronger` has no next model.
- **One minion per ingredient, every shop** (Louis, 2026-10-17: "the cook should give each minion a ingredient and
  let them look for it across all shops. The minions must report back each stores price"): the cook no longer picks
  two shops or product pages; it gives each minion one ingredient, the town and every shop in it with its website, and
  the minion searches each shop's site (`WebSearch`, never a freshness filter, the short name) and answers with one
  line per shop (price, size, per kilogram, location, link, the exact words). Every price is the search index's, so
  the cook no longer uses `VerifyQuote` or the page readers, and the report always carries the search-index line.
- **The fourth hand check (2026-10-17):** the Haiku minions found the right products and invented nothing, but a
  search returns an excerpt picked by the query: the short name ("eggs") gave the description, while the cook's own
  "PnP large eggs 6 pack R" gave R22.99. Spar and Usave showed only promotions and their home pages. The cook retried
  all six minions with `stronger` (no next model), and its `ContinueMinion` calls failed because kept minions had
  evicted each other from the chat's three places. Louis: "why not always fall to the short name and pick the
  closest", then "do all three": the minion searches twice per shop (the short name to pick the closest product, its
  exact title and "price" for the price), Spar and Usave once ("no online prices"); a minion answers once and closes
  (no `ContinueMinion`); the cook never retries a minion or searches the shops itself.
- **Prompts restored (2026-10-17, Louis):** the cook's, researcher's, Puls's routing and the specialists guide are back
  to their pre-step-6 text; the minion feature stays in the code, unused by Puls until a measured run says it pays.

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
