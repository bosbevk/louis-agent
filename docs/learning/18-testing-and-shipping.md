# 18 · Testing and shipping: how the cut was proved

> **Skill:** #18 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for the goldens · **Stage:** the platform
> plan's Phase 2, CL15 *Engine, identities and packs* (2026-10-19), and CL19 *Config and Docker by place*
> (2026-10-24) · **Written by** Claude, for Louis to read (2026-10-19, 2026-10-24) · **Next:** simulations in C# as the release gate (F18, Phase 3); the web app's first tests (F40,
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
  to Puls's own routing. Each diff was read line by line. The one paid cook run of the phase was not spent here but on the finished branch,
  after the layout reviews (`b4a6ea22b178`, in [write-up 07](07-context-engineering.md)), because a run proves the
  code that ships, not a step on the way. It found nothing the cut changed: the no-shop town's rows carrying the
  default store looked new until the stored events were read, and were Puls alone's, in every run on `main` too.
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

## Rules a person can maintain

Halfway through the build Louis stopped a review: "AI likes big files", and he has to keep this code himself. Two rules
came out of it ([ADR 0034](../adr/0034-code-a-person-can-maintain.md)): a `.razor` holds markup only, and a file holds
at most 200 lines (a test file 400). Both arrived on a codebase that broke them 38 and 45 times, so each became a
ratchet like the one above: `FileSizeTests` and `RazorHasNoCodeTests` fail on a new offender *and* on a listed file
that no longer offends, so the lists (33 and 5 at the merge) can only shrink. A rule in `docs/CLAUDE.md` tells a
session what to do; the test is what stops the session that forgot.

The splits were refactors, so their proof was the same as the cut's: the goldens and snapshots stayed byte-equal across
every commit. One host had no golden. The ACP server was about to move into eight classes, so it was pinned first with
a transcript test (`AcpTranscriptTests`), and only then moved. *Test first for a refactor* means that: the test that
proves nothing changed, written before the change.

## Shipping: one folder per place (CL19)

Five days after the cut, Puls ran on Azure Container Apps, and getting it there had left the configuration in four
places with no line between this PC and the cloud: the app's definition git-ignored with its secrets inline, a
start-up script that installed Tailscale on every boot, two copied compose files, Dockerfiles in two folders. The
app crash-looped for three hours on a `nc` that wasn't in the image, and 44 dead tailnet nodes piled up before anyone
could read why. CL19 sorted it by one rule, **a folder is one kind of thing for one place**: `config/` is what the app
reads wherever it runs (unchanged, because the code finds it by name), `docker/` is this PC, `deploy/azure/` is the
cloud. Four things carried the shipping lesson:

- **The sidecar pattern.** The app image is the one the local compose builds, untouched. A second container in the
  same pod owns the one cross-cutting concern, the network path to the home PC, and talks to the app over the pod's
  loopback. Restarts take seconds instead of an apt-get, and the app's log is the app's.
- **Secrets by reference.** The tracked yaml names every secret by `secretRef` and sets no `secrets:` block; the
  update is a patch, so values set once on the app survive every deploy. The deploy script copies them from the same
  git-ignored files the local stack uses and prints only their names. A grep for the four values over the tree is
  part of the check.
- **Verify the environment, not the plan.** The plan's home end of the tunnel was a compose override publishing two
  ports on the Tailscale IP. The cloud check found the PC already forwarding them with `tailscale serve`, which the
  app had been using all along, and Docker couldn't bind the same ports beside it; the override briefly took the local
  browser and Mongo down. The override went, the serve commands went in the how-to, and the plan records the reversal.
- **Config has tests too.** `docker compose config` on every stack after every move, a failing `until` loop for a
  missing variable, a probe from inside the pod with curl's exit code (52 is Mongo closing an HTTP request; 7 would be
  a refused tunnel). None is a unit test; each is the cheapest check that the paths still resolve.

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
- **Measure, then stop.** One paid run for the phase, on the finished branch, and a diff review for every step. The
  goldens made the steps free to prove; the run's one surprise was read from its stored events, not re-run.
- **Read a surprise against the baseline before blaming the change.** The run's `wrong_store` rows looked like a
  regression from the cut. Its stored events, set beside the two runs on main, showed the same rows in every run, all
  from Puls answering alone in `puls-tools` mode: an old bug, now a TODO line, and no behaviour the cut changed.
- **A deploy is config, and config is code** (CL19). What runs in the cloud is readable only when it is in the repo,
  tracked, with its secrets by name and its how-to beside it; the day it lived in a git-ignored folder was the day it
  crash-looped for three hours. The first real step toward F44's pipeline is a yaml and five `az` calls, not a pipeline.
- **A shell script in a container is a test surface.** `wait -n` is bash, the sidecar's shell is busybox; `nc` was
  assumed and absent. `sh -n` and a look at the base image cost seconds; a crash loop cost an afternoon.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plans](../plans/CL15-engine-identities-packs.md), [CL19](../plans/CL19-config-by-place.md) · [Deploying to Azure](../../deploy/azure/README.md) ·
[Tests](../../tests/CLAUDE.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md)
