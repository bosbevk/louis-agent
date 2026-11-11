# Draft: F133 A website with an agent, set up by talking to it

> **Plan draft, not yet approved.** Written on 2026-11-11 from Louis's question "How about I build a wix with my
> platform", after a review of louis-agent.dev and a comparison with the South African market (the doc
> [louis-agent vs the South African AI chatbot market](https://claude.ai/artifact/FuVGT5YgXdFUBJyiX9iDYF)), and his
> follow-up "how far am i from that". The open choices below go to Louis one at a time, three options each (the
> development guide, rules 10 and 11); when they are settled this becomes the parent plan he approves, and each child
> gets its own plan, with the four diagrams and the namespace map, when it starts.

## Context

**What Louis asked.** Whether to build a website builder, like Wix, on the framework, so a business owner can make
their own site with an agent in its corner.

**Not a Wix.** Wix already sells this: its own AI chat (Smart Chat) learns from a site's published pages and comes
with a Wix plan's AI credits since mid-2026, from about $17 a month. What keeps people on Wix is its drag-and-drop
editor, templates, domains, email, payments and support, which one person can't match and which teach nothing on the
skill map. So the idea is narrowed to what the framework is already good at: **the owner talks, the agent writes the
business down, and the site and its agent are made from that one record.**

**Why it matters for selling.** From the market comparison (2026-11-11):

- No South African offer found bundles a website, hosting and an agent near R500 a month; the same pieces bought apart
  cost about R600 to R1,300 a month (a monthly website package R299 to R799, plus a chatbot R299 to R499).
- The cheapest rivals start without a call: DoubleDown AI goes live in about 30 minutes at checkout from R299 a month.
  louis-agent.dev today needs a call and a quote, and every site is built by hand.
- Self-serve also cuts Louis's hours per customer, which is what makes R500 a month tight.

**What exists today** (checked in the code on 2026-11-11):

| Piece | State | Where |
|---|---|---|
| An agent that answers from facts, opens a place on the page and passes a lead on | Built | the showroom identities, `ShowPage` (ADR 0105), the leads pack |
| Sign-in, passkeys, a second factor, a tenant from the host name | Built | F61, `louis-agent.web.accounts` |
| Every request priced in the usage ledger | Built | ADR 0007 |
| Page, section and card components where every place has an address | Built, as markup | `MockPage`, `MockSection`, `MockCard` (ADR 0107) |
| A new site from a template | Built, as code | `dotnet new louis-site` |
| A site as a stored record, read by one generic business identity | Planned | F131 (`SiteRecord`, F131.1 to F131.4) |
| Tenants that own sites, durable sessions | Planned | F43 |
| A customer on their own host name | Planned | F130.1 |
| A monthly rand budget per site | Planned | F7, F131 |
| Admin as a role, not a key | Planned | F108 |

**The gap in one line.** A business is code today: its facts are typed twice, once in its identity's `voice.md`
(embedded at build) and again in its site's markup (`SeemeeuSite.razor`), and it is one line of C# per business
(`GuesthouseIdentity`). Self-serve needs one stored record per business that draws both the site and the agent, with
no build and no deploy.

## Design (proposed)

### The shape, in plain words

- **One record per business.** It holds the facts the agent answers from and the pages the site shows (pages,
  sections, cards, photos, prices), owned by a tenant (F43). How it relates to F131's `SiteRecord` is choice C1.
- **The site is drawn from the record.** Three templates, one per kind of business already in the showroom
  (guesthouse, restaurant, practice), render the record through the existing `MockPage`, `MockSection` and `MockCard`
  shapes, so every place keeps its address and `ShowPage` works unchanged. Colours from a short list, no free editor.
- **The agent reads the same record.** F131's one generic business identity, so a fact changed once changes both.
- **Setup is a conversation.** A setup pack whose tools write the record (`SetBusiness`, `AddRoom`, `SetHours` and so
  on), each checked in code and returning every problem at once; the setup agent asks one thing at a time and shows
  the site filling in beside the chat. Its behaviour is proved by evals, like every agent here.
- **Preview, then pay, then live.** The owner sees their site with a working agent on their own content before paying;
  checkout starts a monthly debit order; going live puts the site on its host name (F130.1).
- **Done-for-you stays.** The same record and templates are what Louis fills by hand for customers who want a call.

### Proposed children

Each its own plan, branch, pull request and write-up when it starts; the order and the cut are part of the approval.

| Child | What | Builds on |
|---|---|---|
| F133.1 | The business record holds the pages as well as the facts, with its contract test | F131.1, choice C1 |
| F133.2 | A site drawn from the record: three templates over the page, section and card components | F133.1 |
| F133.3 | The setup pack: tools that write the record, checked in code, and its evals | F133.1 |
| F133.4 | Photos: an owner's upload, checked, resized and stored per business | F133.1, F55's upload |
| F133.5 | Preview to live: a preview address per draft, then the site on its own host name | F130.1, F43 |
| F133.6 | Checkout and a monthly debit order, and what happens when a payment fails | choice C4 |
| F133.7 | The privacy page and the operator's terms: what is stored, where, for how long, who sees a lead (POPIA) | F43 |

## Open choices, one at a time

- **C1 The record's shape** (to settle before F131.1 starts, since nothing is backward compatible until a sale,
  ADR 0049). A: F131's `SiteRecord` grows the pages, one record and one aggregate per business (Fowler,
  [DDD Aggregate](https://martinfowler.com/bliki/DDD_Aggregate.html)); simplest, but every page edit rewrites the
  record. B: two records, the site's settings and its pages, linked by the site's id; pages edit apart, at the cost of
  a second store contract. C: pages stay code and only the facts become data; no new shape, but no self-serve site.
  Leaning A for its simplicity (ADR 0055); for Louis to pick.
- **C2 When.** The roadmap puts the framework first and no sale before Phase 9. A: after Phase 9, as planned. B: as the
  framework's showcase, after F131 and F43. C: only F133.1 and F133.2 now, so done-for-you sites stop being code.
- **C3 The price of self-serve** beside done-for-you (R500 and R1,000 today), from the comparison's numbers.
- **C4 The payment provider** for South African debit orders (PayFast, Yoco, Paystack): researched first, three options.

## Verification (proposed)

- Each child's own tests, test first; the record's store held to one contract test for memory and MongoDB, as F131's.
- A template test per kind of business: a record in, every place on the page has its address, every fact the agent
  holds is on the page.
- The setup pack's evals: an owner's conversation in, the record out, scored in code; recorded and replayed at $0, a
  live run only with Louis's go.
- One manual check before any sale: a new business from first message to a live site on a test host name.

## Concepts

- **Structured outputs** (skill 5): the setup agent proposes typed changes, code validates them, nothing is saved
  unchecked.
- **Tool design** (skill 4): tools that write a record, each returning every problem with its valid options.
- **Human in the loop** (skill 16): the owner approves the preview before anything goes live or is paid for.
- **Evaluation** (skill 12): a setup conversation scored on the record it leaves, not on its words.
- **Safety and security** (skill 15): tenants, an owner's uploads and POPIA as the operator of many sites.

---
[Docs index](../README.md) · Related: [F131 The chat on any website](F131-the-chat-on-any-website.md) · [TODO](../../../TODO.md) · [Roadmap](../../../ROADMAP.md)
