# 0021. Minions: a specialist hands one task and one of its own tools down, two levels deep and no further

**Status:** Accepted (built, not in use: no session is given `minions` until F31 measures it) · **Date:** 2026-10-16
(decided, F30 step 3) · **Recorded:** 2026-10-17

## Context

Puls's cook priced most answers by reading shop pages itself on Claude Haiku: every page stayed in its context and was
paid for again on each later request (400–800k tokens, up to three minutes a meal turn). A free local model (qwen3:4b
on Ollama) reads a page well enough to copy a price off it, but not well enough to run the cook's whole job. The
measured pattern is MinionS (Narayan et al., 2025): the cloud model splits the work into small single-step tasks over
short pieces, the local model runs them, and the cloud model keeps 97.9% of its quality at 5.7× less cost; a free-form
chat between the two kept only 87%.

Considered: a `puls-prices` specialist on the local model (steps 4–7 of the first plan: the cook couldn't reach it,
sub-sessions were one level deep); a per-role rule saying which specialist may use which child on which model
(`SUB_SESSION_DELEGATION=role>child@llm`: every new use needs configuration); a list of tools per minion role in a data
file (`minions.json`, step 4's first form: a second list that drifts from the toolsets, and the small model choosing
among tools).

## Decision

We add one level below the specialists: **a minion**, a session at depth 2 that runs one minion role on a cheap model,
with exactly one tool, for one task, and never delegates.

- **Capability delegation, in code.** The specialist hands the minion one of its own tools (`StartMinion(role, tool,
  task)`); the API refuses, before anything is built, a tool the parent session doesn't hold and any delegation or
  self-extension tool. The specialist's toolbox is the only ceiling: a minion can never do more than its sender.
- **A role is one file** on its product's base (`Prompts/minions/puls-minion.md` says what every minion is;
  `puls-minion-{function}.md` what this one does and its answer's fixed shape). The base is no role; a minion with no
  role is refused.
- **The model is configuration:** `MINION_LLMS` maps a role (or `*`) to a cascade of profiles, cheapest first, with
  `instance` for the specialist's own model. The specialist can ask for the next model (`stronger`) after its own check
  fails, never name one. An unknown profile stops start-up.
- **The limits are in code:** 16 minions per user turn across every specialist of that turn (one per ingredient of a
  recipe), two minutes each inside the specialist's five, depth 2 is the end of the tree.
- **One child path, never kept:** a minion is opened, asked and rolled up by the same code as a specialist
  (`SpecialistTools`, renamed from `AgentPeerTools`), then deleted once it answers: a chat keeps three children, and
  kept minions evicted its specialists. Its roll-up lands in the specialist's turn, so the chat's roll-up carries it up
  unchanged.

## Consequences

- What the cook would have read into its context is read by a cheap model (Claude Haiku 5.5 by default, or the local
  one), and the cook gets one line per shop back; the measured run (F31) decides whether real prices hold and cost
  drops, or minions go. Until then no prompt hands work to a minion.
- A minion's answer is a claim on a cheap model. The price-lookup minion's prices come from the shops' search results,
  which nothing checks in code: `VerifyQuote` can't read the pages that draw their prices with JavaScript, so the
  answer carries the result's `url` and exact `quote`, and the report says the prices are from the search index. The
  principles (ADR 0020) hold at level 2 as everywhere.
- Every host that runs a specialist sets the session's tool names as a per-turn ambient (`AgentTools`), as it sets
  the attachments and the place, or no tool can be handed down.
- A third level is a new decision, not a setting.

## Links

[F30 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F30-local-web-peer.md) (*Approved during the build*: the minions design) ·
[F30 feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F30-local-web-peer.md) · [ADR 0017](0017-skills-by-level.md) (the levels) ·
[ADR 0020](0020-principles-for-every-agent.md) · [ADR 0004](0004-arguments-as-data.md)
