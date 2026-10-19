# 18 · Testing and shipping: how the cut was proved

> **Skill:** #18 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for the goldens · **Stage:** the platform
> plan's Phase 2, CL15 *Engine, identities and packs* (2026-10-19) · **Written by** Claude, for Louis to read
> (2026-10-19) · **Next:** simulations in C# as the release gate (F18, Phase 3); the web app's first tests (F40,
> Phase 4); a deploy pipeline with versioned contracts (F44, Phase 9)

What this answers: how a refactor that moved 30 files, split prompts and changed two contracts was shown to change
nothing the model reads except what was meant, and what a composition root and a catalogue's start-up checks gave the
tests.

## Before and after

| | Before CL15 | After CL15 |
|---|---|---|
| What pins the model's view | Two goldens: the coding agent's request and every `ToolMessage` | Four: those two, a Puls session (step 0, written from `main` before anything moved) and the shops pack's texts |
| Who knows Puls | Core's prompts, `ToolMessage`, `MinionTools`, `GetToday`'s South African default: 20 places | Nothing in core; a ratchet listed each piece until it left, and is empty |
| When a bad configuration shows | At the first chat, as a 400 or a missing guide | At start-up: an unknown `ROLES_ENABLED` name, a default mode with no file, an `IDENTITIES` name nothing provides, a tool name two toolsets share, a `Requires` cycle, an unknown `HOST_TIME_ZONE` |
| Where the test fakes live | `core.tests`, with `InternalsVisibleTo` opened to three other test projects | `tests/louis-agent.testing`, a class library the test projects reference |
| Test projects | 3 | 6 and the library; 1,555 tests green at the end of step 5 |

## Three goldens as a refactor's proof

A refactor promises that nothing the model reads changes except what the plan says. A golden file makes that promise
checkable: the whole system prompt, every tool with its schema, and every result text, rendered and compared byte by
byte. CL15 opened with step 0, a golden for a Puls session written from `main` before any move, so that every later
step had a diff to review against the plan's prediction:

- **Steps 1 and 2 left all three unchanged.** The catalogue, the contracts, the composition root and Puls's identity
  project were built with no prompt moving yet. A diff of zero lines was the test.
- **Step 3 changed them where the plan said:** the Puls lines left the coding agent's prompt, `GetToday`'s description
  lost its country, four `ToolMessage` texts lost the Puls names, and the shared specialists guide lost Puls's protocol
  to Puls's own routing. Each diff was read line by line, and the one paid cook run of the phase was spent here,
  because the split changed what the cook reads.
- **Step 4 changed exactly four things:** the coding agent lost the three shop tools and the routing row; core's
  `ToolMessage` golden lost 17 entries and the pack's new golden held the same 17, byte-equal; the Puls golden changed
  in three `area` texts and one routing sentence. The first regeneration showed the whole shops guide missing from the
  cook's prompt: the test fixture had not registered the pack's prompts. The golden caught a wrong test, not a wrong
  move, which is what it is for.
- **Step 5 left all four unchanged** while 32 comments were rewritten and the fakes moved.

The lesson is cheap to state and easy to skip: write the golden *before* the move, from the old code, and predict each
step's diff in the plan. A golden written after a step only pins what the step did.

## A composition root, tested on its own

Before CL15 a host built its engines from one static catalogue and the tests built theirs the same way, so a test
could never run "the Puls stack without the shops" or "the coding agent with the shops". `Plugins.FromSettings("puls",
null)` and `Plugins.FromSettings(null, "shops")` are now one-line test subjects: an unknown name is refused naming the
supported ones, unset loads nothing, Puls brings its packs, and `PACKS=shops` gives the coding agent the toolset only
with its settings and its browser. The API's tests build their sessions on the catalogue the Puls stack runs with,
Puls and the shops with no `SHOP_CATALOGUES`, so the cook's optional `shops?` is skipped the way an instance without
the setting skips it.

The catalogue's checks moved failures from a chat to start-up, and each is a test with a fake identity or pack: a
`FakePack` whose tool is named like core's `FetchUrl` fails naming both toolsets; one named `GetToday` fails naming it
as the engine's; two identities naming one pack get one instance; a name that resolves to nothing lists what is
registered. The fakes hold prompts in memory (`FakePrompts`, an `IFileProvider` of a dictionary), so no test writes to
disk and none depends on another's state.

## A ratchet that empties

The rule "core's model-visible text names no product" could not be true on day one: 20 places named Puls or South
Africa. `ArchitectureTests` got a ratchet: a list of today's pieces, each "file: word", with two tests. A new piece
fails; a listed piece that is gone fails too, so the list can only shrink and never goes stale. Step 3 removed 20,
step 4 the last, and step 5 extended the same rule from the model-visible text to every comment and example in core,
with a test that lists each line it finds. Made-up names (`acme`, `{product}`) replaced the real ones, because an
example that names a product is a product dependency a grep can't tell from a real one.

## What was learned

- **Fakes are a dependency.** Three test projects referencing `core.tests` for `FakeChatClient` and `StubHandler` made
  core's tests a library by accident, with `InternalsVisibleTo` as the seam. A class library of fakes, referenced as a
  global using by the test projects that use them, is the honest shape; a test now holds that no test project references another.
- **A set-up fixture per test assembly mirrors the host.** A host's `Build` registers the identities' and packs'
  prompts once, before the first read; each test assembly does the same in a `[SetUpFixture]`, with exactly the
  prompts its subject brings. Skipping one showed as a missing guide in a golden, silently; a start-up check for a
  guide missing from a pack's prompts is a cheap next step.
- **An optional toolset the catalogue doesn't know is refused, not skipped.** `shops?` on an instance that has the pack
  but not the setting is skipped with a log line; on an instance that doesn't have the pack it is an unknown toolset.
  The tests hit the difference first, and keeping it was a decision: a role naming a toolset nothing provides is a
  configuration error, not a missing setting.
- **A heredoc is not a file.** Twice in this phase a Python script passed through a shell heredoc lost its backslashes
  and wrote a backspace character into a regex. Scripts go to a file first; the shell runs the file.
- **Measure, then stop.** One paid run for the phase, placed at the step that changed what the cook reads, and a diff
  review for every other step. The goldens made the other steps free to prove.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plan](../plans/CL15-engine-identities-packs.md) ·
[Tests](../../tests/CLAUDE.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md)
