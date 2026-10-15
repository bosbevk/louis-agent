# F25 · Local cook: Puls's first local specialist

> **Status:** in progress (2026-10-15), on `claude/project-thread-381vmw` · **Milestone:** — · **Depends on:** F21 (roles), F22 (`GetToday`, the user's place, `ConvertCurrency`), F5 (sub-sessions)
>
> **In the TODO:** **F25 Local cook** under *Sessions and memory*, beside F21 and F22 ([TODO](../../TODO.md)) · **Plan:** [plans/F25](../plans/F25-local-cook.md)

The end product is a general agent that knows where its users are and looks up what changes instead of relying on
training ([TODO](../../TODO.md), *South African general AI*). Puls's specialists so far are generic: a researcher, a
reader, a historian, an analyst. The cook is the first **local** one, Louis's idea: one question ("what should I cook?")
gets an answer for where the user is: a South African dish, a shopping list priced in rand at the shops near them, and
a total, so they can drive to the shop and buy it. It is also the first test of whether a specialist's role can make it
use local sources, and whether Puls routes a kind of question to a specialist that shares a toolset with another.

## User stories

**F25-S1 — A meal for today, where I am.** As a *user*, I want to ask Puls what to cook tonight and get a dish that
fits (people, time, budget, season), the recipe, and a shopping list with the shop and the price of each ingredient,
so that I can go and buy it without asking again.
- *Given* "Wat kan ek vanaand kook?" with my town set *then* the answer names a dish, the recipe, each ingredient's
  shop and price in rand, and the total.
- *Given* an ingredient the cook couldn't price *then* it is marked unknown and the total says how many it leaves out;
  no price is invented.
- *Given* no town *then* the answer is for South Africa in general and says so.

**F25-S2 — Puls hands it to the cook.** As *Puls*, I want my router to name the cook for a meal question, so that it
goes to the role that knows the shops, not to the researcher.
- *Given* a meal question *when* Puls delegates *then* `StartSubSession` carries `web` and `puls-cook`, and the task
  names the user's town.
- *Given* a plain web question (the prime rate) *then* it still goes to `puls-researcher`.

**F25-S3 — Measured, not assumed.** As a *developer*, I want a check that shows which role Puls chose and which sites
the cook read, so that "routes properly" and "uses local sources" are numbers from a run, not a reading of the prompt.
- *Given* the cook check *then* each question records the role Puls passed, the hosts the specialist fetched, whether
  the prices are in rand and whether any ingredient is marked unknown, with the cost from the ledger.

## Design notes

- **The role** is `Skills/personalities/puls-cook.md`, on the shared `puls-*` base (built in the Claude project): South
  African first, buyable today at Checkers, Pick n Pay, Woolworths, Spar or Shoprite, every price from the shop's own
  page, never invented. Its tools are the web toolset's (`WebSearch`, `FetchUrl`) and `GetToday`, which every engine
  has (F22).
- **Routing** is two table rows: `default.md` (the request → `web` + `puls-cook`) and `agents-skills.md` (the role's
  job, and "put their town in the task"). Nothing in code changes: a role exists for Puls once its tables name it (F21).
  `roles.json` is unchanged: a web sub-session with no role is still the researcher; the cook is always passed.
- **The check** reuses the delegation check (`samples/puls-check/run_check.py`): its sessions, streaming and ledger
  reading. New: the specialist's own tool calls, read from the `sub_session` events the API already streams, so the
  cook's `FetchUrl` hosts are recorded; and a score per answer (role, local hosts, rand, unknown rather than invented).
  Two modes: Puls as built (`agents` only) and, at Louis's request, Puls with `web` itself, to see what it saves on
  plain web questions and whether it still hands a meal to the cook.

## Done when

- **F25-S2:** both routers name the cook with `web`. *Met:* `AMealToCook_GoesToTheCook_WithWeb` (both files), the
  golden file showing only the new row.
- **F25-S1 and F25-S3:** in the cook check, every meal question goes to `puls-cook`, the control question to the
  researcher, every price the cook reports comes from a South African shop's page it fetched, and unpriced ingredients
  are marked unknown. *Open:* the run.
- Suites pass; the run's README is in `samples/` and its numbers in [BENCHMARKS.md](../BENCHMARKS.md).

## What the build changed

*(filled in as the steps land)*

---
[Features](README.md) · [Plan](../plans/F25-local-cook.md)
