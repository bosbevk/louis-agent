# The demos

> **Explanation.** What each demo on the site shows, how it was made, what is real and what is played back, and what
> it would mean for a business. The guide in the site's corner reads this to explain the demos. **Elsewhere:** why
> the public watches recordings in [ADR 0108](../../../src/docs/adr/0108-the-public-watches-the-coding-agent-recorded.md),
> how to record them again in [the recordings' how-to](../recordings/README.md).

The site's demos are at `/demos`. Each one is a real run of agents built on the framework, recorded once and played
back in the browser. Nothing runs and nothing is paid for while a visitor watches. The live agents run shell commands
and write files, so they run only for the people the project signs in, behind a key.

## The coding agent fixing a bug (`/coding-agent`)

**What it shows.** The coding agent gets a small C# library that matches bank payments to their invoices, and a
request in plain words: the payments are not matching, find out why, fix it and run the tests. It lists the project,
reads the code and the tests, runs the tests and sees two of them fail, finds the two bugs (a lower-case reference
was missed, and `INV-104` took the payment meant for `INV-1042`), fixes them, runs the tests again and sees all four
pass. It leaves the change for a person to commit and says what it changed and why.

**How it was made.** One session against the API, on Claude Haiku, on a fresh copy of the sample, recorded as the API
streamed it: seven requests, about R0.10, priced by the usage ledger. The page plays that stream in the framework's
own chat view, so a visitor sees exactly what a developer would: the thinking, every tool call, the answer and the
bill under it.

**What is real.** The tools, the tests, the bugs and the fix are real, and so is the price. Only the timing is played
back.

**For a business.** The same agent works on a company's own repository, from Rider, the terminal or a web app, with
only the files, git, dotnet, shell and web packs it is given, and every request measured. A team can have one built
for its own code and rules.

## The triage team at work (`/triage-demo`)

**What it shows.** A made-up order service fails on a retired discount code. A team of agents handles the error the
way a support team would: the sorter decides which department it belongs to, the manager decides what it gets, the
investigator finds the cause and where it sits in the code, the coding agent fixes it on a branch with a test, code
checks the fix by running the tests and the failing request, and the reviewer judges the fix. The case closes waiting
for a person to merge it. Nothing is ever pushed.

**How it was made.** One real run on Claude Haiku, a few rand, recorded by a script that does what the workflow will
do: it hands each agent its brief, keeps each agent's answer as a typed step, commits the coder's change and checks it
in code. The page plays the steps one at a time, lighting the agent at work, with a popup per step and the whole run
as a list.

**What is real.** Every agent's answer is its own, from its own typed tool, and the fix, its test and the checks are
real. The workflow that will run triages live is still being built, so for now the run is played back.

**For a business.** A company whose services throw errors can have a team like this watch its logs: sort each error,
decide what it needs, find the cause and propose a fix with a test, with a person deciding what is merged.

## What the demos have in common

- They are recordings of real runs, not mock-ups. The words on screen are what the agents said.
- They cost a few cents to a few rand to make, once, and nothing to watch.
- They end at the offer: an agent like these, built on the framework for a business's own work.

---
[Docs index](README.md) · Related: [the site's project doc](projects/louis-agent.code-review.md),
[the triage identity](projects/louis-agent.identity.triage.md), [recordings](../recordings/README.md)
