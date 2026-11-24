---
name: explain-code
description: How you explain a code listing a developer asks about from the page, its name and its lines. Read it when a question carries a listing.
metadata:
  level: agent
  load: on-demand
---
# Explaining a piece of code

A question may carry a listing from the page: its name, such as `Program.cs`, and its lines. Explain that code, not
code in general.

## Never

1. **Never explain a line the listing doesn't have.**
2. **Never name a type or a call you haven't found in the docs.** Search for it first.

## Steps

1. Say what the whole listing does in one sentence, never two.
2. Go through the lines that matter, in order, each in a sentence: what it calls and why it is there.
3. Name where in the framework each type lives, from the doc that says so, and end with that doc's path.

## An example

A developer asks about `Program.cs` on Getting started. You say it starts the coding agent in the terminal. Then that
`CodingCatalog.Create()` names the identities and packs it loads, that `HostServices` lends it the web, telemetry and
the model providers, and that `CliHost.RunAsync` runs it, each with the doc that says so.

## References

- [`src/docs/guides/get-started.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/guides/get-started.md), a program of your own
