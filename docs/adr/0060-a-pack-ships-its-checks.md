# 0060. A pack ships its checks

**Status:** Accepted · **Date:** 2026-10-31 (Louis's picks on the F49.3 plan's findings K, L and M, and during its
build) · **Recorded:** 2026-10-31

## Context

The platform's rule is *identity = who, pack = what it can do, eval = how we prove it* (ADR 0027). After F49.2b an
identity ships readers, not checks (ADR 0059), but the shop rules (`untooled-shops`, `estimates-at-shop`, and
`priced-at-shop`, kept without an entry) still sat in Puls: they knew the shops pack's tool names and its shops, yet
the pack that has the tools had no way to ship the rules that prove them. Around them sat about 600 lines of helpers,
some shop knowledge, some plain markdown reading that Puls's own price reading uses too.

Weighed for how a pack lists its checks: a static registry beside the pack, gathered by the composition (taken), a
`Checks` member on core's `IAgentPack` (core would have to reference the check contract), and a second interface in
the contract the pack implements. Weighed for what moves where: shop knowledge to the pack and plain markdown reading
to the contract (taken), everything to the pack with a copy of the table reader in Puls, and Puls referencing the pack
(forbidden). Weighed for the one thing only Puls knows, whether a price is given as fact: a reader the file names
(taken), the pack's own rand-only rule, and dropping the rule.

## Decision

- **A pack ships the checks that prove its tools**, in its `Checks/` folder and a registry of its own
  (`ShopsChecks.All`), which the composition's `EvalChecks.All` gathers; an eval file names them by type. The pack
  references the check contract for them, never the harness.
- **What two owners share moves down to the contract**: plain markdown reading (`LouisAgent.Evals.Markdown`: tables,
  the shopping list, links, a site's host) is read the same way by an identity, a pack and the harness.
- **What only an identity knows comes in through a reader the file names**, as ADR 0059's compares do: the shops
  pack's rules take an `IPriceReader`, and Puls ships `rand-prices`. A pack's check reads the turn from the engine's
  events, never an identity's turn reader.
- **A small table both sides need may live twice**, the pack's copy and the identity's, held equal by a test in the
  composition's tests, the one project that sees both (`ShopTableTests`). Anything bigger moves down to the contract.
- **A check moves with what only it reads**, never before: a helper whose only user stays behind stays with it.

## Consequences

- Puls has no check of its own: `PulsChecks` is gone, its eval names the pack's rules, and its `Evals/Checks/` keeps
  only the expectations its judge reads and the site table its prices read. The cook's grades held through every step
  (`CookGradesTests`, 230 grades on 26 transcripts).
- The pack's rule tests use a fake reader; the real reader is tested beside the identity that ships it, and the eval's
  pinned grades prove the two together.
- A scenario key's values the pack reads (`shop`, `town`) are the pack's to parse and refuse (`ScenarioShop`), so a
  bad value is refused when the rule grades, not when the identity's dataset tests read it.
- A second pack's checks are one list and one line in `EvalChecks.All`; the guide is
  [Build a pack](../guides/build-a-pack.md#9-its-checks-when-it-proves-its-tools).

## Links

[F49 Evals as content](../plans/F49-evals-as-content.md) · [0027](0027-engine-identities-and-packs.md) ·
[0043](0043-repeated-names-are-enums.md) · [0055](0055-keep-it-simple.md) · [0056](0056-a-check-is-a-catalogue-entry.md)
· [0057](0057-regex-is-the-last-option.md) · [0059](0059-an-identity-ships-readers-not-checks.md) ·
[write-up 20](../learning/20-extensibility.md#a-pack-ships-its-checks-f493) ·
[louis-agent.pack.shops](../projects/louis-agent.pack.shops.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
