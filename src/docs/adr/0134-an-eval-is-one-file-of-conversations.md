# 0134. An eval is one file of conversations, and the old evals are gone

**Status:** Accepted · **Date:** 2026-11-22 · **Recorded:** 2026-11-22 · Supersedes
[0022](0022-benchmarks-recorded-from-the-protocol.md), [0029](0029-simulations-in-csharp.md),
[0047](0047-protocol-v2-one-event-contract.md), [0056](0056-a-check-is-a-catalogue-entry.md),
[0058](0058-an-eval-is-a-folder-of-files.md), [0059](0059-an-identity-ships-readers-not-checks.md),
[0061](0061-evals-grow-from-use.md), [0062](0062-the-studio-writes-what-git-keeps.md) and
[0073](0073-packs-reference-no-evals.md)

![An eval is one file of conversations beside the identity it proves: facts told in code, criteria a judge marks, a person's flip, run over the sessions API](diagrams/0134-an-eval-is-one-file-of-conversations.svg)

## Context

Nine ADRs built the old evals. An eval was a folder of files with its readers and checks (0058, 0059), found by
type through a check contract of its own (0056). A runner wrote a protocol of events (0047) that the API recorded
into MongoDB (0022), and a studio in the admin wrote drafts back to git (0062) and grew evals from use (0061). It
worked, and it was large: a harness, a protocol, a contract, a runner, a studio's API and its pages, for three evals.

Louis, 2026-11-09: "we can redesign and throw everything away, I do not like the evals how we do it", "it is not
simple, the idea is too make it simple and user friendly and have an elegant back end design that still works", and
"we cannot have 23000 lines of code for something simple". His bar that day: someone who has never seen the code
writes an eval in five minutes, and a run's result is understood at a glance. On 2026-11-22 he picked to delete
the old evals first, before anything new is built, and to record the new shape here.

## Decision

An eval is one file of conversations, and the old evals are gone.

- **Today's evals are deleted whole, and nothing is ported.** No stored run is moved, since nothing we store is
  kept compatible before a sale ([ADR 0049](0049-nothing-is-compatible-before-a-sale.md)).
- **An eval is one `{name}.eval.json` beside the identity it proves.** It holds conversations, a conversation holds
  turns, and each turn has its own facts and criteria.
- **A fact is told in code**, from a closed list parsed once into an enum. Everything else is a criterion, which one
  model judge marks yes or no with a reason, one call per criterion.
- **A conversation passes only when every fact and every criterion of every turn is met, in every repeat.**
- **A person corrects the judge by flipping a mark.** There are no label files and no percentage of agreement.
- **The eval part is a library a site's own API maps.** A run asks the agent over the sessions API, like any app.
- **An eval file is read through the same file providers as the prompts.**
- **Two rules carry over.** A pack and an identity reference no eval project, as 0073 said, so either can follow
  the engine into a package alone. And a live run waits for Louis's go, as 0029 said, since it spends money.

## Consequences

- Until the runs come back in F126.3, the repository can run no eval, and the admin has no Evals area. The first
  new evals are written in F126.5.
- The framework's site keeps the evals family in its place among the five, with no project under it, and says the
  evals are being rebuilt.
- What the old shape held is in git, so a later child copies back what it needs, such as the session client, with
  its test first.
- The nine old ADRs stay readable, each pointing here, and none is edited beyond its status line.

## Links

- F126, F126.1; [ADR 0049](0049-nothing-is-compatible-before-a-sale.md), nothing is compatible before a sale;
  [ADR 0011](0011-prompts-are-content.md), prompts are content
- Michael Nygard, [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions):
  a decision that is replaced stays, marked superseded

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0049](0049-nothing-is-compatible-before-a-sale.md) · [0029](0029-simulations-in-csharp.md) · [0073](0073-packs-reference-no-evals.md)
