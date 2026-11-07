# Roadmap

> **Plan.** Where this is going and in what order: what we sell, what exists, the phases with their ids, how it earns
> money, what we watch and what could go wrong. **Elsewhere:** what is open, one row each, in [TODO.md](TODO.md);
> what was finished in the [work log](src/docs/work-log.md); the why of each lasting choice in the
> [ADRs](src/docs/adr/README.md); the goal it all serves in [GOAL.md](src/docs/GOAL.md). The long plan this replaced
> (Louis, 2026-11-07) is at the tag `pre-roadmap`; the feature plans it carried are drafts in
> [src/docs/plans/](src/docs/plans/README.md).

## In one minute

Puls is a South African AI assistant that knows your town. Ask it for tonight's braai and it answers in one go with a
local recipe, which nearby shops stock the ingredients and what they cost in rand. It runs on louis-agent, our own
agent engine in C# on .NET, built so Louis learns AI engineering: a learning choice, not the moat.

- **The problem.** General assistants know the world, not your town; a local, priced, buyable answer takes many
  follow-ups, and the prices they give are guessed.
- **Our edge.** Local data read in code rather than guessed by the model, a South African voice (English and
  Afrikaans), and the control layer around the agent.
- **Learning first, selling second** (Louis, 2026-10-19). When a sale and the learning pull apart, the learning wins.

## What we sell

Three offers on one engine. Nobody pays for the loop (Microsoft's `FunctionInvokingChatClient` is free); they pay for
the **control layer** around it: cost they can see and stop, answers checked in code, guards in code, every agent
measured, their data on their own model, their systems plugged in without code, configured rather than coded.

| Offer | Who buys | What they get | How it is paid | When |
|---|---|---|---|---|
| **Puls** | The public in South Africa; local businesses listed beside its answers | A local guide on cloud models, with sources and dates | Credits per question (F23), to be decided | After hosting; a hobby and the showpiece (Louis, 2026-10-30) |
| **A configured agent, hosted** | A business with no developers | Its own identity and packs, hosted and measured; every request priced by the usage ledger | A monthly fee per agent plus usage | First sale after Phase 9 |
| **The engine** | A team building its own agents on .NET | The application framework for AI agents in .NET: the package, the two contracts, the control layer, the eval harness | A licence per instance per year | Its own track, alongside configured agents (Louis, 2026-11-07) |

**Two tracks, side by side** (Louis, 2026-11-07: "they must run in parallel"). Configured agents are the money track
(we run them, so we see every failure); the framework is a track of its own that takes its turns between the phases,
as Puls's does, and is measured against Microsoft Agent Framework rather than described ([draft](src/docs/plans/F106-framework-track.md)).
No pilot or sale starts before Phase 9: until then an agent is only shown (the playground's simulations) and priced (the ten
conversations). A buyer compares us with free, so the loop is not the reason to buy; the control layer, on .NET,
fitted to South African buyers (POPIA, rand, local models), with simulations that show after every change whether an
agent still does its task, is.

## What exists

Phases 0 to 3 are done: the docs, three security doors closed, the engine cut apart from Puls into identities and
packs, every check a C# eval on one harness. Since then (2026-10-30 to 2026-11-07):

- **Evals as content** (F49): an eval is files beside its identity, a pack ships its checks, the admin portal has a
  studio, one Evals page, a review queue and the run page (F53), and a paid judge that can tell (FX45).
- **The cook** (F35, F48.1 to F48.3): 45 South African recipe sites read as data under every site's robots.txt and
  no-AI signals, a search over many sites at once, a find-recipe minion, a recipe library of its own with a counter,
  and the maths (`ScaleRecipe`, yields, a recipe or a meal). The hand-built shop reader was removed on 2026-11-04 when
  no retailer answered; prices now come from Stats SA's published averages, uploaded in the admin portal (F55), and
  branches from OpenStreetMap (F50.2); the packs' feeds are MCP resources (F56).
- **The engine, measured and shown**: every request traced through OpenTelemetry (F52); the engine's own website with
  two films (F54, F57); the hosts are libraries and core ships no agent (ADR 0076); the packs are one area (CL32).
- **Every site on the framework's web** (F59): the admin and chat frames, described screens, the pack web projects, a
  starter template (`dotnet new louis-site`), the eval pages; the coding agent's landing page.
- **Accounts** (F61.1 to F61.5): ASP.NET Core Identity on SQL Server, a tenant from the host name, social logins,
  passkeys and a second factor, as a library a site opts into (CL30).

The [work log](src/docs/work-log.md) has the counts and every row.

| What | Result | Measured |
|---|---|---|
| Puls's cook: a local recipe with shop prices (the shop reader since removed) | 31 of 31 answers right: 26 ingredients priced with sources, 5 correctly left unpriced where no supported shop is | Run of 2026-10-18, $0.118 |
| Shop data read in code, not pages read by the model | Sourced prices from 0 of 32 to 31 of 31 | Three runs, 2026-10-17 to 2026-10-18 |
| Recipes read as a page's own data, not its text | 45 of 74 South African recipe sites allow a reader; 14 publish Recipe data, 31 are read from their text | Checked 2026-11-04 (F35.1) |
| Prompt caching | A demo run from $0.66 to $0.13, all 10 fixes still correct | Engine benchmark |
| Tool sets per conversation | The fixed part of each request from about 48,500 to 24,400 tokens | Engine benchmark |
| Every agent tested by evals in C# | The cook 23 of 24, the routing 72 of 72, $0.37 together; a replay costs $0 | 2026-10-29 |

Puls is not yet open to the public: the shops refuse automated readers, so the cook gives estimates from Stats SA's
averages and says where to buy until a retailer says yes (F85). The two sites go live from this PC first (F58), then
from Azure (F61.4); the final cook's paid measure is still owed.

## The phases

One at a time, in this order; Puls's own track takes its turns between them. An id links the TODO's row; a phase is
done when Louis has written its learning write-up himself, without the code open.

| Phase | What | The selling point | Status | Ids |
|---|---|---|---|---|
| 0 to 3 | Docs and ADRs; three doors closed; the cut into engine, identities and packs; evals in C# | Measured | Done | — |
| 3b | Evals as content: an eval is files beside its identity, a pack ships its checks, a studio in the admin portal | Measured | Done | F49 |
| 3c | The cook: recipe sources read as data, then the cook as a home cook and teacher | Measured | F35 done; F48.1 to F48.3 done, F48.4 to F48.6 remain | F48 |
| 3d | Localisation: every text from a resource file beside its enum | Configured, not coded | Now | F46 |
| 3e | Puls's local specialists: the where-to-buy, shop-prices and weather packs, then one specialist at a time | Measured | Now; F50.2 done | F50 |
| 4 | The sales playground: the demo identity, each pitch a simulation over made-up data, cost on screen | Measured | Now | F40 |
| 5 | Cost you can see and stop: estimates, budgets that warn, ask or stop, cost per customer, the bill check | Cost | Next | F7 |
| 6 | Orchestration: one delegation function, typed results checked in code, the orchestrator as an identity | Checked, not trusted | Next | F39 |
| 7 | Checked and guarded: append-only history, drafts a person approves, safety and audit | Guards in code | Next | FX4, F41, F42 |
| 8 | Their systems, no code: someone else's MCP server plugged in as a pack, our guards around it | Their systems | Next | F36 |
| 9 | What a sale needs: accounts and tenants, reliability, local models, the engine as a product, hosting | Your data, your model | Next; F61.1 to F61.5 built the accounts, F61.4 (Azure) remains | F43, F8, F15, F44, F51, F61 |
| Framework | The framework's track: a builder for the common path, its own docs, an example outside Puls, a side-by-side with Microsoft Agent Framework, packaging in place; its own repo after the engine features | The engine | Between the phases, from now (Louis, 2026-11-07) | F106, F74, F47 |
| Puls | Places, local businesses, sources as data, credits, vision, the cook's waves | Puls | Between the phases | the TODO's *Puls* rows |
| Later | Knowledge and retrieval, long chats, settings per role, the coding identity, and the rest | — | Later | the TODO's *Later* rows |

**3b to 3e.** 3b is built (F49.1 to F49.5: the checks catalogue, evals as files, checks in packs, the studio). 3c: F35's
recipe sources, library and minion are built and F48's maths too (`ScaleRecipe`, yields, a recipe or a meal); the rest
of `WriteRecipe` (the list and its prices, F48.4), the pantry and the teaching answers (F48.5) and the cook's first wave
(F48.6) remain, in that order ([plan](demos/puls/docs/plans/F48-cook-and-teacher.md)). 3d is one feature, F46. 3e is
F50: the where-to-buy and shop-prices packs are built (F50.2: branches from OpenStreetMap, Stats SA's average prices
in Mongo, uploaded in the admin portal), the weather pack is next, then gardener, farmer, stargazer, braaimaster and
the rest, each a role on the packs it names with its eval, run before it is shown.

**4, the sales playground** ([draft plan](src/docs/plans/F40-sales-playground.md)). The demo identity: each pitch is a
mode, a role and a small pack over made-up data, born as a simulation, so a pitch is also a measurement. The first
three are the business plan's top ideas: the practice's invoice clerk, "get paid", the area guide; a pitch that sells
moves to a real identity or pack in its own feature. It never touches the engine or Puls.

**5, cost** ([draft plan](src/docs/plans/F07-budgets.md)). A customer on every ledger row and estimates (F7.1); budgets
that warn, ask or stop, the room's cost view, the ledger checked against the bill (F7.2). OWASP LLM10:2025 is the
standard.

**6, orchestration** ([draft plan](src/docs/plans/F39-orchestration.md)). Part A: one delegation function in the engine,
transport, credentials per peer, policy and limiter, typed results checked in code. Part B: the `voorman` identity on a
thin host, triaging a service's errors and confirming each fix with git, a replay and the tests.

**7, checked and guarded** ([draft plan](src/docs/plans/phase-7-checked-and-guarded.md)). FX4 first: each
conversation's prompt and tools frozen, so an approval no longer breaks an open chat. Then F41, drafts validated in code
and approved by a person, approval per risk from each tool's declared effect; and F42, the untrusted-content frame,
the audit log, rate limits, injection simulations as the red-team suite.

**8, their systems** ([draft plan](src/docs/plans/F36-mcp-packs.md)). An MCP client with our guards around someone
else's tools, listed once when the host starts; annotations that only tighten approval. Every pack stays convertible
to MCP until then (ADR 0028).

**9, what a sale needs** ([draft plan](src/docs/plans/phase-9-what-a-sale-needs.md)). F61 built sign-in, tenants,
social logins and passkeys; the move to Azure (F61.4, [plan](src/docs/plans/F61-accounts-and-azure.md)) is open, and
before it the two sites go live from this PC (F58). F43's durable sessions and the tenant on every ledger row, F8's
reliability (retries, a fallback model, timeouts), F15's local models given their whole prompt, F44's speed, metrics
and packages, F51's gateway and uptime remain.

**The framework's track** ([draft](src/docs/plans/F106-framework-track.md)). Louis, 2026-11-07: selling configured agents
and the framework "must run in parallel". The positioning: the application framework for AI agents in .NET, judged
by one question, does it make an agent application easier to build, extend, test or operate. Its first children: a
builder so a first agent takes a few lines (F106.1), the framework's own docs (F106.2), an example outside Puls
(F106.3), the same application built in Microsoft Agent Framework and compared (F106.4), and packaging in place
(F74, the [Phase 10 draft](src/docs/plans/phase-10-engine-package.md)'s Stage A). The engine's own repo (Stage B) still
waits for the engine features; a developer's product, never a business's.

**Puls's track** ([draft](src/docs/plans/puls-track.md)). Every source that knows the place answers through a few tools
whose sites are data: businesses that join, open place data, a site's own JSON-LD, a few hand-built readers where terms
allow, the open web cited last. Listings and credits are sold only with Phase 9's accounts.

## How it makes money

Every idea is the same product in different clothes: a hosted assistant that does one admin job inside a business's
own systems, with answers checked in code, cost in rand and a person approving what matters. Ten ideas scored for
South Africa (2026-10-30; the research is in the project files), the top four:

| Idea | Who pays | Why |
|---|---|---|
| **A practice assistant for bookkeepers and tax practitioners** | The practice | 19,000+ SAIPA and SAIT members; 79% use AI already; nobody does the work between systems (email, WhatsApp, PDFs, SARS letters) with checks |
| **"Get paid", a debtors agent for small businesses** | The business, through its accountant | 91% of SA SMEs are paid late; first-party collection stays outside the Debt Collectors Act |
| **A FICA agent for small law firms and estate agencies** | The firm | 25,000 practitioners and agencies registered with the FIC; one firm fined R7.7 million |
| **.NET agent engineering for companies**, services first, the licence later | A company on .NET | Money now, no new build, and the road to the engine licence |

**The path.** One product for practices (idea 1) whose first job is chasing late payments for the practice's clients
(idea 2): one product, two buyers, a practice a channel to 20 to 80 businesses, sold mostly in writing. .NET agent
work alongside, capped at two days a week. FICA as the second product once the first has three paying clients. Puls
stays the hobby and the showpiece: its measured numbers go on the CV and on a public results page.

| Step | Done when |
|---|---|
| Now (F62): one-question emails to 20 people, three one-page service offers, a public results page | 3 or more replies |
| Before the first invoice: (Pty) Ltd, SARS representative, bank account, B-BBEE affidavit, one attorney hour (POPIA) | Done |
| Phase 4: the invoice clerk and "get paid" as simulations with cost on screen, a four-minute video | 3 of 10 practices name a price |
| Phases 5 to 8: a price sheet (R1,500, R3,500, R6,000 a month), Xero and Sage developer accounts | Listed |
| After Phase 9: three 60-day pilots at R1,500, setup waived | 2 of 3 renew at full price |
| After the pilots: full price, a practice resells "get paid", FICA as product two | Ten paying clients |

**The money, one practice at full price:** fee R3,500; model about R600 (2,000 conversations at R0.30); hosting about
R130 (Azure South Africa North, shared over five clients); EFT R10; **about R2,760 left before Louis's time (79%)**.
Pricing rules: a fixed fee that covers expected usage three times over, the ledger shown to the client, a budget that
warns and stops, rand prices with a 20% buffer for the dollar.

**Rules before the first invoice** (general information, not legal advice): we are the operator for a client's data
under POPIA (a written operator agreement; the model runs abroad under section 72, Bedrock in `af-south-1` for clients
who want their data in SA); debt collection first-party only, never for a cut; a FICA agent fills the forms and a person
signs; health data not before a partner carries the compliance; (Pty) Ltd before invoicing.

## The numbers we watch

| Number | Target | If missed |
|---|---|---|
| Replies to the 20 emails | 3 or more | Change the subject line and the job named, not the product |
| Practices that name a price | 3 of 10 | Change the kind of business before Phase 4 |
| Cost per business conversation (the ledger) | Under R1 | Check the prompt's size |
| Pilots that renew at full price | 2 of 3 | Fix the job, not the price |
| Support hours per client a month | Under 4 | Fix checks, budgets and audit before the next client |
| Gross margin per client | Above 70% | Raise the fee or cap usage |
| Churn | Under 3% a month | Ask every leaver why |

## Risks

| Risk | What answers it |
|---|---|
| Local data is hard to get: one hand-built shop reader took 45 commits and was removed when no retailer answered (2026-11-04) | Published data first (Stats SA's averages, OpenStreetMap's branches); a retailer's own prices only with its permission or feed (F85); businesses that join supply their own |
| Website terms forbid reading | A source is turned on only after its terms are read (ADR 0023); every fetch obeys robots.txt (ADR 0068) |
| The grocers' own AI cooks and Google's local search | Puls competes on cross-shop, local, sourced answers, not a better chatbot |
| AI costs grow with use | Every request costed; caching cut a run by 80%; budgets that stop come in Phase 5 |
| Wrong answers | Checked in code; every agent measured before release |
| Nobody pays what we hoped | The 20 emails and ten conversations first; a pilot price first; services carry the bills |
| Xero or Sage builds it in | Sell the work between systems, outside their product |
| Two tracks for one person (configured agents and the framework) | Each track takes its turns between the phases, never both at once; the money track's numbers decide if the framework waits |
| One person | Ten clients is the first ceiling; checks, budgets and audit keep support under four hours a client; services capped at two days a week; no phase hurried for a sale |
| A data breach or a POPIA complaint | No hosting for a client before Phase 9's tenants and Phase 7's audit |
| The pace | One commit a step, reviewed; a phase is done only when Louis has written its write-up himself |

## What we are looking for

To be filled in by Louis: the ask (an amount, a partnership or a pilot business), what it pays for, and how to reach him.

## How we work through it

The repo's rules ([the development guide](src/docs/CLAUDE.md)): one step at a time, test first, each step a commit
Louis reviews; a feature holds about ten steps, bigger work is a parent with children; every paid run waits for his
go; every phase ends with a learning write-up. The TODO says which row comes next.

---
[Docs index](src/docs/README.md) · Related: [TODO](TODO.md) · [Work log](src/docs/work-log.md) · [Plans](src/docs/plans/README.md)
