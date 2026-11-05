# ADR 0080: Make a tool when it improves what the prompt does

> **Explanation.** Why we reach for a tool, not another prompt rule, when the model keeps getting a job wrong that code
> could do.

## Context

The cook scales a recipe's amounts to the number of people. We kept tightening the prompt about it, and it kept slipping:
in one stopped run, 1 teaspoon of ginger × ⅔ came back as 2⅔, and five of the nine answers the judge failed had a wrong
amount. A clearer sentence asks the model to be more careful at something it's not built to be reliable at. Code gets a
sum right every time.

The same thinking applied to retyping ([ADR 0079](0079-show-a-specialists-report.md)): no prompt could make a faithful
copy free, so the engine does it instead.

## Decision

**When a tool would make a prompt's job work better or come out more accurate, make the tool rather than adding to the
prompt.** Before writing or tightening a prompt rule, ask whether code could hold it. If it could, the model decides what
to ask for and the tool does the work.

This is for work with one right answer, or a rule code can enforce: sums, counting, formats, looking something up. It
isn't for judgement, such as which recipe fits or how to word an answer, which stays in the prompt.

A tool the model asks for is a pack's ([ADR 0076](0076-core-ships-no-agent.md)), built by
[Build a pack](../guides/build-a-pack.md), and takes its arguments as data, never through a shell
([ADR 0004](0004-arguments-as-data.md)). A session gets only the tools its role needs
([ADR 0006](0006-toolsets-per-session.md)).

## Consequences

- The job gets more accurate where the model uses the tool. The prompt still has to tell it when, so a tool is only as good
  as the guide that routes to it.
- A tool isn't free. Every call is at least one more request, and its description sits in every request's prompt. So a
  tool that's meant to save money or time is measured like anything else ([ADR 0007](0007-measure-every-request.md)), and
  stays only if the numbers hold up.
- A prompt can get shorter once a tool holds its rule, which is worth checking when a tool is added.
- The first example is the calculator pack, `packs/louis-agent.pack.calculator`, which the cook uses to scale amounts.

## Links

- [Build a pack](../guides/build-a-pack.md)
- [ADR 0079: an instance can show a specialist's report](0079-show-a-specialists-report.md)
- [ADR 0055: keep it simple](0055-keep-it-simple.md)

---
[Docs index](../README.md) · Related: [ADR index](README.md)
