# The template eval

> **Reference.** What `template-example` checks and how to read its grades. Copy this folder to start an eval;
> **Elsewhere:** the steps in [Add an eval](../../../docs/guides/add-an-eval.md).

Replace this page with your eval's manual: what it asks, what a pass means, and what to do when a check fails. The
portal opens it from the eval's card.

## What it asks

One question, in the dev split (`Dataset/scenarios/dev/example.json`). Replace it with the real task your users send.

## Its mode

`example`: the role `template/voice` with the `web` toolset. A mode is one way of running the same conversations;
add one per way you want to compare.

## Its checks

| Check | Required | What it means |
|---|---|---|
| `no-error` | yes | The turn finished without an error from the API or the model |
| `within-time` | no | The turn took at most 120 seconds; shown and counted, but the outcome doesn't wait on it |

Every check here is one of the harness's general checks, so the eval needs no C#. A check of your own is a class with
an `Entry` listed by your identity or pack.

## Its transcripts

`Transcripts/example.reference.txt` is a run that passes; `example.bad.error.txt` the same run with an error, which
`no-error` must fail. Every required check needs a known-bad it fails, so a check that can't fail is caught.
