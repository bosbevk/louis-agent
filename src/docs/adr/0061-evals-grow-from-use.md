# 0061. Evals grow from use

**Status:** Accepted · **Date:** 2026-10-31 (the F49.4 plan's findings O and P, and Louis's picks during its build on
2026-11-01) ·
**Recorded:** 2026-11-03

## Context

An eval is a folder of files beside the identity it proves (ADR 0058): its scenarios, transcripts and checks live in
git, and a pull request changes them. Until F49.4 the only way to add a scenario or a known-bad transcript was to write
the file by hand in Rider. But the best samples are the ones real use turns up (Anthropic's *Develop test cases*:
start from real failures and grow the dataset from use), and the admin portal already holds them: stored runs, their
conversations, and the verdicts people gave them in human review. Louis's goal was "it should be simple to make evals,
edit them and from the admin panel, living in identity".

Weighed for where a sample made in the portal lives before it is part of an eval: a store, published to git by a later
step (taken); files written straight into the workspace's eval folder (simplest on Louis's PC, nothing on Azure, a
half-made sample sitting in the checkout); both at once. Weighed for where a sample's expected keys come from: the
scenario the conversation ran, read from the workspace (taken); the API loading the eval through the harness (every
host would carry it); free JSON typed by the person, checked only when the runner next loads it.

## Decision

- **A sample or a transcript made in the portal is a draft in a store, never a file yet.** `IEvalDraftStore` (memory
  for the tests, Mongo's `eval_drafts` for a real instance) holds sample drafts and transcript drafts, each for one
  eval (ADR 0033's scope). A draft never changes an eval by itself: F49.5's publish writes the files and the pull
  request, so git stays the truth.
- **A sample is a variant of the scenario its conversation ran**: the first question is its input, the later ones its
  follow-ups, the place, time zone, expected keys and categories the scenario's; the person gives the id and changes
  values, and a key the scenario doesn't have is refused, since no check would read it.
- **A transcript is kept only when people's verdicts say what it is**: a reference when every decided verdict on the
  conversation is a pass, a known-bad when one is a fail, with its reason in its file name; an unsure offers neither.
  The person is the gate: no model decides what becomes part of an eval.
- **The API reads an eval's folder only through the runner's listing**, behind the guard the manuals use
  (`EvalPaths`), and never references the harness: the API's `SampleBody` is held equal to the harness's
  `SampleFile` by a test where both are referenced.
- **What becomes a file name is kebab-case** (a sample's id, a known-bad's reason), checked by a small reader
  (ADR 0057), so nothing typed can reach outside its folder.

## Consequences

- A sample is made from a chat without Rider (F49's done-when for F49.4), on Louis's PC and on the hosted instance
  alike; the drafts wait on the Evals page, each eval's under its card, until F49.5 publishes them.
- A draft outlives its run: deleting a run leaves the drafts made from it, which carry what they need (the sample's
  body, the transcript's lines).
- A Puls chat (not a stored run) as a source waits: it has no scenario to take the keys from, so its form is bigger.
- The verdict rule is a judgement call: one judge's fail makes a conversation a known-bad even when another judge
  passed it, since the reason names what that fail was.

## Links

[F49 Evals as content](../architecture/evals.md#where-f49-came-from) · [0033](0033-what-the-agent-writes-has-a-scope.md) ·
[0048](0048-mongo-through-records.md) · [0057](0057-regex-is-the-last-option.md) ·
[0058](0058-an-eval-is-a-folder-of-files.md) · [write-up 12](../learning/12-evaluation.md#evals-grow-from-use-f494) ·
[louis-agent.api](../projects/louis-agent.api.md) · Anthropic, *Develop test cases*

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
