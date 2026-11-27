# 14 · Reliability: what F8 taught

> **Skill:** #14 in the [goal](../GOAL.md)'s map · **Stages:** F8 *Reliability* (2026-11-27) · **Written by** Claude,
> for Louis to read · **Next:** F180, a second model that takes over when the first keeps failing

![Reliability: a busy moment retried before any words, an account limit said plainly, both found by a probe first](diagrams/14-reliability.svg)

What this answers: how the agent rides out a provider's busy moment without ever saying an answer twice, why an
account limit is treated as the opposite of a busy moment, and what a free probe saved us from building.

## F8: a busy moment retried, an account limit said plainly (2026-11-27)

F8's feature doc was a month old and planned retries for every provider, with a client of our own. The first thing
the research found was that Anthropic's C# SDK already retries: twice, on 429 and every 5xx, waiting as `retry-after`
says. But its docs also said "the SDK doesn't retry an error in a stream", and every turn of ours is streamed, so it
was easy to conclude that none of our turns were retried at all. Louis asked for the research to be done properly
before choosing, and a free probe settled it in a minute, with no network: the SDK's own client over a stub answering
429, then 529, then 400, then an overload inside a stream. The first two were retried and answered, the 400 wasn't,
and only the overload that arrived as an event inside a stream that had begun failed at once. So F8 built only that
gap, not the whole thing the old doc imagined.

The design rests on one rule: ask again only before any words. A retry after part of an answer has gone out would say
it twice. The surprise was that a real stream opens with an event that carries no words, and the SDK turns it into an
empty update before the overload arrives, so a client that counted any update as "spoken" would never retry the very
case F8 is for. The retrying client holds such empty updates back until the first one with content, and drops them on
a retry, so the person sees the opening once. A test on the SDK's own client over a stub proves it: a stream that opens
and then is overloaded is asked again and answered.

An account limit is the opposite of a busy moment: asking again only fails the same way, and hides what is wrong.
Anthropic says it three ways (a spend limit a person set answers 400, the tier's monthly cap 429, a billing problem
402), and each now becomes one exception carrying Anthropic's reason and the time access comes back, read from
Anthropic's own sentence. We met the 400 ourselves that same morning, mid-eval, which is the wording the tests use. A
chat now says it in our words, with that time, because the provider's sentence names its workspace and limits, which
a visitor shouldn't read (FX239). And an eval run stops at the first one, the agent's or the judge's, where it used to
fail every conversation after it.

What it cost: nothing was paid for. An overload can't be caused on purpose, so everything was proved over stubs on the
SDK's own client, and the probes were deleted once they had answered. What didn't work, or nearly: I committed the
first step without showing it, a step of the rule Louis set, and step 2's test wrote Anthropic's error types as strings
that share our own wire names' spelling, which the rule tests caught a step later. The second model that takes over
when the first keeps failing was left for its own feature, F180, at Louis's pick.

Concept: *14 Reliability* in the [goal](../GOAL.md): retry what passes, never what is final, and never after the
answer has started; and measure what a library already does before building around it.

---
[Docs index](../README.md) · [Learning](README.md) · [Goal](../GOAL.md)
