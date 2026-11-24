# 18 · Testing and shipping: how the cut was proved

> **Skill:** #18 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for the goldens · **Stage:** the platform
> plan's Phase 2, CL15 *Engine, identities and packs* (2026-10-19), CL19 *Config and Docker by place*
> (2026-10-24), and F54 *The engine's website* (PR #133, 2026-11-03) · **Written by** Claude, for Louis to read (2026-10-19, 2026-10-24, 2026-11-04) · **Next:** simulations in C# as the release gate (F18, Phase 3); the web app's first tests (F40,
> Phase 4); a deploy pipeline with versioned contracts (F44, Phase 9)

What this answers: how a refactor that moved 30 files, split prompts and changed two contracts was shown to change
nothing the model reads except what was meant, and what a composition root and a catalogue's start-up checks gave the
tests.

![Testing and shipping: goldens that prove a refactor, rules a person can maintain, shipping by place, a site held to the code, and a change proved with numbers before it switches](diagrams/18-testing-and-shipping.svg)

The sections run in the order the work was done, so some names below were later replaced. F54's static site was
folded into the web app (F109.1) and rebuilt, so its `WebApplicationFactory` page tests and `SiteStylesheetTests` are
gone. A page's description is now the `Description` of its `PageHead`. And once every picture used the new flow
rules, the old way went (CL65.4): `IArrowRule`, `IRowRule` and `FlowRules` are gone, and the layout uses
`ApartArrows` and `LevelRows` directly.

## Before and after

| | Before CL15 | After CL15 |
|---|---|---|
| What pins the model's view | Two goldens: the coding agent's request and every `ToolMessage` | Four: those two, a Puls session (step 0, written from `main` before anything moved) and the shops pack's texts |
| Who knows Puls | Core's prompts, `ToolMessage`, `MinionTools`, `GetToday`'s South African default: 20 places | Nothing in core; a ratchet listed each piece until it left, and is empty |
| When a bad configuration shows | At the first chat, as a 400 or a missing guide | At start-up: an unknown `ROLES_ENABLED` name, a default mode with no file, an `IDENTITIES` name nothing provides, a tool name two toolsets share, a `Requires` cycle, an unknown `HOST_TIME_ZONE` |
| Where the test fakes live | `core.tests`, with `InternalsVisibleTo` opened to three other test projects | `tests/LouisAgent.Testing`, a class library the test projects reference |
| Test projects | 3 | 6 and the library; 1,555 tests green at the end of step 5 |

## Three goldens as a refactor's proof

A refactor promises that nothing the model reads changes except what the plan says. A golden file makes that promise
checkable: the whole system prompt, every tool with its schema, and every result text, rendered and compared byte by
byte. CL15 opened with step 0, a golden for a Puls session written from `main` before any move, so that every later
step had a diff to review against the plan's prediction:

Steps 1 and 2 left all three unchanged. The catalogue, the contracts, the composition root and Puls's identity project
were built with no prompt moving yet. A diff of zero lines was the test.

**Step 3 changed them where the plan said.** The Puls lines left the coding agent's prompt, `GetToday`'s description lost
its country, four `ToolMessage` texts lost the Puls names, and the shared specialists guide lost Puls's protocol to
Puls's own routing. Each diff was read line by line. The one paid cook run of the phase was not spent here but on the
finished branch, after the layout reviews (`b4a6ea22b178`, in [write-up 07](07-context-engineering.md)), because a run
proves the code that ships, not a step on the way. It found nothing the cut changed: the no-shop town's rows carrying
the default store looked new until the stored events were read, and were Puls alone's, in every run on `main` too.

**Step 4 changed exactly four things.** The coding agent lost the three shop tools and the routing row; core's
`ToolMessage` golden lost 17 entries and the pack's new golden held the same 17, byte-equal; the Puls golden changed
in three `area` texts and one routing sentence. The first regeneration showed the whole shops guide missing from the
cook's prompt: the test fixture had not registered the pack's prompts. The golden caught a wrong test, not a wrong
move, which is what it is for.

Step 5 left all four unchanged while 32 comments were rewritten and the fakes moved.

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

The sidecar pattern. The app image is the one the local compose builds, untouched. A second container in the same pod
owns the one cross-cutting concern, the network path to the home PC, and talks to the app over the pod's loopback.
Restarts take seconds instead of an apt-get, and the app's log is the app's.

Secrets by reference. The tracked yaml names every secret by `secretRef` and sets no `secrets:` block; the update is a
patch, so values set once on the app survive every deploy. The deploy script copies them from the same git-ignored
files the local stack uses and prints only their names. A grep for the four values over the tree is part of the check.

Verify the environment, not the plan. The plan's home end of the tunnel was a compose override publishing two ports on
the Tailscale IP. The cloud check found the PC already forwarding them with `tailscale serve`, which the app had been
using all along, and Docker couldn't bind the same ports beside it; the override briefly took the local browser and
Mongo down. The override went, the serve commands went in the how-to, and the plan records the reversal.

Config has tests too. `docker compose config` on every stack after every move, a failing `until` loop for a missing
variable, a probe from inside the pod with curl's exit code (52 is Mongo closing an HTTP request; 7 would be a refused
tunnel). None is a unit test; each is the cheapest check that the paths still resolve.

## Git stays the truth (F49.5)

Skill #18 again, with #12. When F49 finished (2026-11-03), the admin portal could make a whole eval, and the
question was where it should end up. Several hosted eval tools kept the evals in their own store, and when the tool
died the evals died with it. So the studio's evals live where the code lives: as files in git, changed by a pull
request ([ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)).

Publish is a commit, not a push. The eval's folder is committed beside its identity on a branch of its own,
`eval/{identity}-{name}`. You push it and open the pull request. Then a person reviews it like any other change, and
CI runs it. The API has no GitHub token, so it has no secret to leak and can't merge anything by itself.

It never touches your checkout. The commit is made with git's plumbing (the low-level commands under `git add` and
`git commit`) over a temporary index: the files are written into git's object store, a tree is built from them, and
the branch is pointed at a new commit off HEAD. Your current branch, your working tree and anything you have staged
stay exactly as they were. The git endpoints would have switched branches under you.

The gate before the commit is the gate CI runs. Only a folder the runner's `--check` passed is published, and that
check is the conformance test every eval folder meets in CI. A pull request from the studio shouldn't go red for a
reason the studio could have told you about.

Where there's no checkout, a zip. The hosted Puls has no git workspace, so a publish there hands you the folder as a
zip to add in a pull request. It's more work, but it still ends in git.

## A host that is only content (F54)

The engine's website was the first host with no engine in it (until F109.1 folded it into the coding agent's web app): `LouisAgent.Site` referenced nothing of ours and
runs no model. It is ASP.NET Core with **static server rendering**: a Razor component is rendered once on the server
into plain HTML and sent, no WebAssembly, no interactivity, no script. A page is its markup and one stylesheet, and
the two films of how the engine works are CSS keyframes over CSS 3D transforms. What that gave the tests:

A page is a string. `WebApplicationFactory<Program>` starts the site in memory and the test asks each route and reads
the HTML: the title, the film's class, no `blazor.web.js`. No browser in the test, no port, a second for all five
pages. `/health` answers the container's check the same way.

The behaviour is in the stylesheet, so the stylesheet is tested. The films' logic (where a part stands, when the
camera flies in, when a caption shows) is CSS. `SiteStylesheetTests` reads `app.css`'s import list against the files
on disk, holds tokens first, and refuses a keyframe name the two films could share (every name ends in `-s` or `-d`).
A rule a file could break silently is a test, whatever language the file is in.

A golden per film, as for the prompts: bUnit renders the markup and `Snapshots.Match` pins it, so a change to a film
is read in a diff. The detailed film's six slabs inside core are checked against the chat pipeline's order as a plain
array in the test, which is the one honest way to couple a project that must not reference core.

Headless eyes for a CSS animation. A film can't be looked at by waiting for it. Playwright pauses every animation and
sets its time (`document.getAnimations()`, `currentTime`), so a scene's midpoint is one screenshot and the whole film
is 1,800 frames ffmpeg joins into the MP4; the stylesheet stays the one source.

Numbers in parallel branches collide. Two branches opened the same day both took ADR 0065; the clash was caught by a
colleague's note before the push, not by a test. Next free is read from `main` *and* the open PRs, and a test that
refuses two ADR files with one number on merge is cheap.

**Measured.** $0. The site calls no model; the only spend was the time to render frames.

## Everything in Azure, as one file (F61.4)

The CL19 deploy reached the PC for its browser and its store through a Tailscale sidecar, because a Container App can't
hold Mongo (its disk is a file share, and Mongo's engine fails on it) and the browser's DevTools port must never be
public. F61.4 turned that round: the services moved to where the app is, on a small VM in a private network that the
apps' subnet alone can reach, and the sidecar, the ingress lock and the open key went with it, since the web app now
signs in and the gate takes a signed-in person. Three things to keep from building it:

A build is a file, not a sequence of commands. Louis asked for every `az` write to be recorded in config as a build
step, then for scripts that check for a resource and create or update it; a Bicep deployment is exactly that for every
resource it declares, so the template became the record, and the script's verbs are only what a template can't do:
copying files to the VM, building images, the app's revisions, the domain's certificate once the DNS records exist. A
verb reads what it needs (the VM's address) back from the deployment's outputs, never from a value someone copied.

A secret never travels in a definition. The SQL password and the SSH key reach the template as parameters read from
environment variables the verb sets from the git-ignored files; the VM's env files go up through run-command after the
first boot, because cloud-init is readable on the VM for good.

The old stays until the new answers. Nothing of the CL19 deploy or the F58 tunnels is deleted before both domains
serve from Azure; the READMEs describe both until then, and the retirement is one listed step.

## The site moves into its app (F109.1)

The static site of F54 had one job, content, and one rule, no script. Six steps later it is the landing of the
coding agent's web app, and nothing about the content changed. What made the move cheap is worth keeping:

A frame with slots, in the framework. The site's header, menu, footer and a corner for a chat widget are one
component, `SiteShell`, with the same shape as the admin's frame: the demo's layout fills six slots in twenty lines.
Louis's rule was the reason ("that custom layout can be reused, it should be in the framework"); ADR 0084 was already
the rule, and a test keeps a site's layout under forty lines.

Goldens prove a move. The films' markup was pinned before the move; copying the components across projects and seeing
the same goldens pass is the whole proof that no pixel of markup changed. The films' export hash does the same for the
rendered WebP: it fails on the moved files until the films are re-exported from the app.

A look stays where it belongs with one selector. The site's element rules hang off `.site`, the frame's root, so the
chat and the admin keep the framework's. The alternative, ordering a list so the right rule wins, works until the next
partial is added in the wrong place.

Tests pin behaviour, not transport. The site's tests fetched HTML from a server; the app is WebAssembly, so the same
assertions became bUnit renders. What went: the two tests about the transport itself (plain HTML, `/health`).

## A merge that ships itself (F109.5)

Until F109.5 a release needed Louis's PC, because that's where Azure trusted him. The fix was not to give GitHub a
password but to teach Azure to trust GitHub: **workload identity federation**. Each workflow run gets a token GitHub
signs, saying "this is `bosbevk/louis-agent`, on `main`"; Azure was told once to accept exactly that and swaps it for
an Azure token that lasts the run. Nothing secret is stored, so there is nothing to leak or rotate. Two habits come
with it: **least privilege** (the deploy identity can build and roll the app, never read a secret's value) and
**gating on the checks** (the deploy starts only when CI passed on `main`, so merging early can't ship a red build).
The workflow runs the very script the PC runs, so there is one way to deploy, tested in two places.

## Two apps on one build (F109.2)

The louis-agent app went to Azure in an afternoon because nothing had to be invented: it is Puls's build with a
second name in it. What the day taught:

A table beats a copy. One script with `-App` and a table of what is each app's (image, config, yaml, identity, share,
domain, the suffix on its own secrets) is twenty lines; a second script would have been three hundred that drift. The
shared parts (the VM, the environment, the database with a tenant per app) are shared on purpose, to keep the month's
bill at one VM, and ADR 0098 says how they split when they must.

Infrastructure as a file is a diff you can read. The Bicep what-if named four creates and the same six reference-only
modifies as the first deploy; that is the whole review of a change to production, before anything is written.

Keep a verb honest about what it can't finish. The domain verb bound Puls's apex in one run; here the certificate's
token was where Azure no longer returns it, and a bind on a pending certificate fails. The verb now reads the token
from the one API version that still carries it, stops with a message while the certificate is pending, and does
nothing twice on a second run. A deploy script's job is to be re-runnable.

DNS has a memory and so does the browser. Cloudflare imports the old host's records when a zone is added, so the first
thing to do on a domain is delete what you did not add; and Chrome remembered the old proxy's HTTP/3, which a reload
cleared.

## What was learned

Fakes are a dependency. Three test projects referencing `core.tests` for `FakeChatClient` and `StubHandler` made
core's tests a library by accident, with `InternalsVisibleTo` as the seam. A class library of fakes, referenced as a
global using by the test projects that use them, is the honest shape; a test now holds that no test project references
another.

A set-up fixture per test assembly mirrors the host. A host's `Build` registers the identities' and packs' prompts
once, before the first read; each test assembly does the same in a `[SetUpFixture]`, with exactly the prompts its
subject brings. Skipping one showed as a missing guide in a golden, silently; a start-up check for a guide missing
from a pack's prompts is a cheap next step.

An optional toolset the catalogue doesn't know is refused, not skipped. `shops?` on an instance that has the pack but
not the setting is skipped with a log line; on an instance that doesn't have the pack it is an unknown toolset. The
tests hit the difference first, and keeping it was a decision: a role naming a toolset nothing provides is a
configuration error, not a missing setting.

Measure, then stop. One paid run for the phase, on the finished branch, and a diff review for every step. The goldens
made the steps free to prove; the run's one surprise was read from its stored events, not re-run.

Read a surprise against the baseline before blaming the change. The run's `wrong_store` rows looked like a regression
from the cut. Its stored events, set beside the two runs on main, showed the same rows in every run, all from Puls
answering alone in `puls-tools` mode: an old bug, now a TODO line, and no behaviour the cut changed.

A deploy is config, and config is code (CL19). What runs in the cloud is readable only when it is in the repo,
tracked, with its secrets by name and its how-to beside it; the day it lived in a git-ignored folder was the day it
crash-looped for three hours. The first real step toward F44's pipeline is a yaml and five `az` calls, not a pipeline.

A shell script in a container is a test surface. `wait -n` is bash, the sidecar's shell is busybox; `nc` was assumed
and absent. `sh -n` and a look at the base image cost seconds; a crash loop cost an afternoon.

## Docs a test holds to the code

The site went stale without anything failing: CL29 changed the structure, and the films kept saying the old one. Prose can't be
compiled, so the fix was to make the parts that mirror code checkable (F57):

Say it once, and test the copy. The program on *Build your own* is shown as text, so a test reads the real
`Program.cs` and demands every line of it on the page. Change the program and the page fails.

Test the list against the source. Every folder under `packs/` must be on the *Packs* page. A new pack can't be
forgotten.

A generated file records what made it. A rendered film takes minutes and a test can't read its pixels, so the export
stores a hash of the film's source files, and comparing two hashes is instant. It's the same idea as a lock file.

Looking still found what tests couldn't. Frame by frame, one scene's card hid the very part the scene was about, and it had been
that way since the film was built. A test can hold words to the code; it takes eyes to see a picture is wrong.

What it measures: size decided the format. The same film was 22 MB as a GIF and under 3 MB as WebP at the same frame rate, which
settled a choice that opinion wouldn't have. Concept: *18 Testing and shipping* in the [goal](../GOAL.md).

## A site held to the solution (F109.4)

F57 held three things on the site to the code; F109.4 made the whole site that way, because a site that explains the
whole architecture drifts the moment the architecture moves. The step that mattered was the first: a test that reads
the solution file and asks that every project that ships is named on some page. Writing the Architecture page was then
filling a list the test already held, and the test says the day a project is added without a sentence about it.

Hold a claim to the thing it claims about, not to a copy. The rand on the Measured page are computed by the test from
the rates file the app itself falls back on; the packages the Built on it page names are checked against the project
files; the planned features against the TODO's open rows. Each test reads the source the page was written from, so a
wrong number can't be pinned in a golden.

What's planned is a page, not a line, when a test keeps it honest. ADR 0066 kept the plan to one line because nothing
could stop a planned feature reading as shipped. With every item held to an open row and labelled, the plan can be
shown in full: what ships fails the test and leaves the page.

The guide is a page too. The expert's voice names every page and the docs it came from, held by a test over the
routes, so the site and the guide can't tell different stories.

A film's layout needs eyes once more. The export's poster frame showed a slab under the caption strip in the wide
shot; the tests hold the words, not the picture. Concept: *18 Testing and shipping* in the [goal](../GOAL.md).

## A site a crawler can read (F121, 2026-11-09)

A Blazor WebAssembly site is a program the browser runs. Before F121 every address on every site answered the same
file, the landing's title and an empty `<div id="app">`, and the page only appeared once the app had downloaded and
started. A person never noticed. A crawler that runs no script, a link preview on WhatsApp or LinkedIn, an AI crawler,
saw every page as the landing with no words. The sitemap of F119 pointed them at 71 places that all said nothing.

**What changed.** The API now renders each page itself, once, and serves it as HTML: the page's own title,
description, link card, canonical link and words inside `#app`. The app still starts and replaces that body, so a
person sees no difference. Measured with `curl` on the review copy: `/how-it-works` went from the landing's title and
an empty body to its own title, a canonical link to itself and all four sections' words.

Prerendering, in one call. *Prerendering* means producing a page's HTML on the server before any script runs. ASP.NET
Core ships the tool, `HtmlRenderer`, which renders a component outside a browser. Beside the page it renders Blazor's
`HeadOutlet`, so whatever the page puts in the head (its `PageTitle`, its `PageDescription`) comes out too. A host's
program says `MapLouisAgentSite(...)` where it said `MapFallbackToFile("index.html")`, and that is all a site does.

Render once, keep it. A page is rendered the first time it is asked for and kept until the process restarts. A deploy
restarts it, so the cache can't go stale: there is nothing to invalidate. Louis chose this over a publish step that
writes files, which would need its own step in the build, and over a test that writes them into git, which would put
generated HTML in every diff.

Opt in by saying what the page is about. Only a page with a `PageDescription` is prerendered. The chat, the admin
screens and the account pages have nothing to rank, and some can't render without their live services. So "is this
page for search?" is answered by the page itself, in one line it needs anyway, not by a list in the host.

Fail to the old behaviour, loudly. A page that throws while rendering, because it injects something its site didn't
register, answers the plain shell, as before F121, and one warning goes to the log. A crawler gets less, and nobody
gets an error page. The chat page does exactly this on the review copy, and that is expected.

Test against the real file. `PageShellTests` fill the code-review site's actual `index.html`, not a sample, so a
change to that file that breaks the filling fails a test rather than a search ranking months later.

**What didn't work at first.** `MapFallback` was given a method that took an `HttpContext` and returned a task. ASP.NET
binds that shape as a plain request delegate, so the method's result was thrown away and every page answered an
empty 200. Taking the `HttpRequest` instead makes it a real endpoint whose result is written. A test through the test server
caught it, where a unit test of the method would have passed. And the standard HTML encoder escaped the site's "·"
into `&#183;`, valid but noisy in a title, so the shell escapes only the four characters HTML needs.

**Left as it is.** Once the app starts, the head holds the description twice, the shell's and the one Blazor's head
outlet adds. Both say the same thing, and a crawler that runs no script sees one. Concept: *18 Testing and shipping*
in the [goal](../GOAL.md), with [ADR 0109](../adr/0109-a-page-is-served-as-html.md).

## Facts a search engine reads (F123, 2026-11-09)

F121 let a crawler read every page. F123 tells it what it is reading. Louis's goal was page one for "c# agent
framework", and nobody can promise that: Google ranks mostly by links from other sites. So the work was the part code
can do, and a how-to for the part only a person can.

Structured data. A search engine reads facts about a page from *JSON-LD*: a script in the head, in the words of
schema.org, that says "this is an organisation, its logo is here, its LinkedIn is there" or "this page sits under this
site". It is the same idea as an agent's tool returning a typed result instead of loose text: a machine reads facts
best when they come as facts. The framework's records write them, the landing says who runs the site and what the
software is, and the API adds a breadcrumb to every other page by itself.

The bug a curl found. Three facts on the landing, each from its own head component, and the landing was served as the
empty shell again. Blazor keeps only the *last* `HeadContent` a page renders, so the facts pushed the description out,
and with no description the API serves the shell (F121's opt-in rule). The unit tests passed, because each rendered
one component alone. Only `curl` of the real page, then a test with two heads, showed it. Fixed by Louis's pick: one
`PageHead` writes the whole head, renamed by Rider on every page of the three sites.

A fair comparison is a rule, not a tone. The page for the search compares louis-agent with Microsoft Agent Framework
and Semantic Kernel. Every claim about theirs is taken from their own pages, read on a date the page shows, and
linked. What their pages don't say (Microsoft.Extensions.AI, cost tracking) isn't claimed either way. Every claim
about ours is held to the repo by a test. It leads with Louis's selling point: it runs on your own hardware, with no
cloud agent service in between.

The part no code does. Search Console, the sitemap submitted, the important pages asked for first, and links from real
places. The [how-to](../guides/get-a-site-found.md) lists them in order.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a test of the parts is not a test of the page, and a
`curl` of what ships is part of the check. With [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md).

## A layout proved with numbers, then switched in one step (F134.8, 2026-11-14)

**What it is.** Every flow in the docs, 93 pictures, was redrawn: a new look, straight lines, an arrow into a box
at a point of its own, and shapes. The AI-engineering idea is not about pictures. It is how to change something a
reader sees in 93 places without ever showing it half changed: **build the new way behind a rule, measure it over
the real data, and hand it over in one step.**

**What was built, in order.** The arrows' way of running became a rule the layout is handed (`IArrowRule`), with
the old way as its first implementation, so nothing changed. Then the new rule, a piece at a time: a point for
each arrow, one bend line a gap, and a second rule for where boxes stand (`IRowRule`), level with what feeds them.
Then shapes as a field in the data. Then the look, shared with the component diagram. Only then did the flow's
renderer ask for the new rules, and all 93 pictures changed in that one commit. Class charts never asked, so not
one of theirs changed. The site's pages followed, and last each picture's JSON named its shapes.

**What was measured.** A layout is a pure function of its data, so it is counted, not eyeballed. Throwaway tests
ran every rule over the docs' real flows:

| Over the 93 flows | Joined, before | Apart and level, after |
|---|---|---|
| Pairs of lines from different boxes lying on one another | 231 | 0 |
| Crossings | 161 | 65 |
| Forward arrows with no bend, of 830 | 226 | 377 |

Six pictures grew taller, most by 40 px, and eight grew shorter. The first number is now a test: no two lines of
different boxes may lie on one another in any flow.

**What changed on the way, and why.**

*"Parallel" meant straight and level.* You said the lines were not parallel, twice. I first gave every box a track of
its own, then lined the bends up. Both were wrong. A drawn alternative settled it in one answer: a box stands level
with the box that feeds it, so the line between them has no bend. A picture of two options is a better question than a
paragraph. *A literal rule made one picture 700 px taller.* "One row height for groups beside each other" was right
for two groups and absurd for a group with one tall box. The count caught it before any picture was committed. The
rule became: groups that start at the top share a row height, and a far taller one spans an odd number of rows. *The
shapes did not pop.* The first design had outlined badges and no colour bar, and you saw it at once beside the
component diagram. The fix was to share the look's parts, not to copy them. *The switch found what the counts could
not.* With boxes level, a box often stood at both heights a long arrow could run at, and the arrow looped round the
whole column. No count flagged it, since nothing overlapped. It was seen by looking at all 93 pictures on contact
sheets. Such an arrow now passes between two boxes. *A word on its line needs a ground.* On the site a word had only a
halo round its letters, and the line showed through the space between two words: "A TURN" read as "A—TURN". Seen only
in the browser.

**What the tests hold.** Each rule with numbers, on small made-up pictures. A golden file for the new drawing.
Over every flow in the docs: no line through a box it doesn't join, no word on a line that isn't its own, no two
lines of different boxes on one another. Every SVG is what its JSON draws, byte for byte. A shape that is no shape
is refused with the shapes there are.

**The patterns.** Strategy, twice: one job done two ways behind an interface, so a kind of picture that has not
had its turn keeps the old way and the old way is deleted later without touching the router. A Parameter Object
(`FlowRules`) hands the two rules over as one, so nobody can pair level boxes with joined arrows by accident.

**What didn't work, or cost time.**

A test that could not fail. My first test for the looping arrow asked that "all points inside the column" be between
two boxes. There were no points strictly inside it, so it passed on the bug. It now counts the points first. A test of
"all" needs a test of "some". I built the pass-between rule without putting it to you first. It was shown with the
step, but a choice about how lines run is yours before it is built, not after. Naming shapes took a rule three tries
to say. "By what the box is" needed two more lines: a class counts as data only when its label names the file beside
it, and a class at the top of a structure picture is not a start. Without them 468 of 845 boxes would have been pills.

**Where it sits.** Skill 18, testing and shipping: a change measured over real data and shipped in one step. Skill
20, extensibility, for the two rules.

## Built beside the old, switched in one commit (F134.9, 2026-11-14)

**What it is.** The cards picture, the overview every project doc opens with, was redone: the project is a
headline on a light, and its parts hang from a lit rail. You picked the design from three, each drawn from a real
picture's data. The engineering idea is the one F134.8 taught, used a second way: **when nothing keeps the old
way, don't build a switch. Build the new way beside the old, prove it alone, and swap them in one commit.**

**What was built, in order.** The reader's check that a link joins two neighbouring rows. The rails, grouped from
the links with no places at all. How tall each kind of card is. The layout, which places cards and stands rails.
The style. The writer's way to define a gradient. The drawing, with a golden. Then one commit that handed the
renderer the new drawing, deleted the old layout, and redrew all 45 pictures.

**Why no rule object this time.** F134.8 handed the layout a rule (Strategy), because class charts had to keep
the old lines. Here nothing kept the old drawing. A Strategy with one strategy is a switch nobody flips, so the
old and the new were two plain classes for six commits, and the old one was deleted in the seventh.

**How the layout was split so each part is proved alone.**

| Part | What it knows | What its tests need |
|---|---|---|
| `CardRails` | which links share a rail | ids only |
| `CardSizes` | how tall a card is, where words break | a card and a width |
| `RailPlaces` | where a gap's rails stand | the middle of each card, as numbers |
| `OverviewLayout` | rows, gaps, a rail's ends | the three above |
| `OverviewDrawing` | nothing about places or colours | the layout's result |

Each row was a commit with its tests seen failing first. None needed a browser.

**What was measured.** Over the 45 real pictures: 51 rails, 19 quiet cards, no gap with rails at two heights, and
3.6% more height in all. After the switch the docs' tool wrote 45 pictures and left the other 219 untouched,
which is the proof that no other kind of picture changed.

**What looking found that numbers did not.**

An arrowhead of a link up stood tight under a headline's text. A stem under a headline now starts a little clear of
it. In a row of four cards one title went under its badge beside three that didn't. The layout kept 18 at a card's
right where the approved mockup kept 14. A measure can be right and still differ from the design by a number. The
first mockup's rail said nothing about direction once motion is off. Small arrowheads were added before any code, in
the design step.

**What it cost to leave a fixed-width font.** The old picture set every word in the code font, so a line's width
was its length times one number, and a test could prove a line fits. The new look sets words in a font whose
letters differ in width. The layout now takes a character's width as a number chosen wide. The tests still say
every line fits by that measure, but the real proof is looking at the 45.

**What didn't work, or cost time.**

Two test cards were named "web" and "files", which are toolset names, and a rule test that reads the whole repo
failed. The fix was a rename, but it shows a made-up id is never free in a repo with rules about names. A test of mine
assumed a title would break over two lines on a half-width card. It fits on one. The code was right and the test was a
guess about the data. A command over Bash's length limit wrote nothing at all. A long file is written with the file
tool. CSS cannot give an SVG shape a gradient fill. I found that only at the style step, so the drawing step grew a
part: the writer learning to define a paint, and a page's copy learning to keep it.

**Where it sits.** Skill 18, testing and shipping: a change proved in parts and shipped in one step. Skill 20,
extensibility: one shared look under three kinds of picture, each adding only its own.

## One layout, two looks, by the numbers it is handed (F134.10, 2026-11-14)

**What it is.** The sequence picture, who calls whom in order, was redone in the cards' look. A participant is a
badge in its tone with its name under it. A call is a lit line that runs from violet to blue. An answer is a thin
dashed grey line. Under the steps the picture has a key. You picked the design from three, after asking how the
key would look on it. The engineering idea is the third way to change a drawing, after F134.8 and F134.9: **when
only the numbers differ, don't write a second layout. Hand the one layout its numbers.**

**The three ways, side by side.**

| Feature | What differed between old and new | What was done |
|---|---|---|
| F134.8, flows | how an arrow is routed, and class charts kept the old way | a rule object the layout is handed (Strategy) |
| F134.9, cards | the idea itself: rails instead of lines | a second layout beside the first, then the first deleted |
| F134.10, sequences | only sizes: a head's height, a line's step, a frame's reach | one value of numbers the layout is handed (Parameter Object) |

The sequence's layout already did what the new look needs: lifelines as far apart as the words between them
need, rows down the page, a frame round each block. Its numbers were constants in one class that four other
classes read. Step 3 turned them into one record, `SequenceMeasures`, with one instance holding the old numbers.
Step 4 added a second instance for the new look. Step 8 deleted the first.

**How step 3 was proved.** It changed 9 files and no picture. The proof was not a test I wrote: the diagrams'
409 tests passed unchanged, the sequence's golden among them, and the docs' tool, which redraws every picture
and compares bytes, wrote none. A refactor that touches a layout is proved by the pictures it does not move.

**How the shared look was proved.** The cards and the sequence wear the same title, light, glass and pill. Those
rules moved out of the cards' style into a shared one, and their names changed from `crd-` to `mrq-`. So all 45
cards pictures changed as text. To prove they did not change as pictures, each was shot before and after, in both
colour schemes: 92 images, and one hash over them that was the same both times.

**What was built, in order.** The measures as a value. The new look's numbers, with tests that hold relations
(heads apart, words between their lifelines, a frame round its rows) and a check over the 64 real pictures. The
shared look, then the sequence's own style. The key's layout, a pure function of the data. The drawing, with a
golden. The switch, and all the pictures redrawn. Then the old numbers deleted.

**What looking found that numbers did not.** The drawing's first picture passed every test and had three faults:

A frame sat on what followed it. An inner frame's bottom touched its parent's dashed line, and a note stood 4 under a
frame. The layout had no room under a frame, because the old look's frames were plain boxes that could touch. A new
measure, 12 under a frame, fixed it. A frame's tab stands 14 inside the frame in this look, and the layout's least
width for a frame did not count that. A long label in a narrow frame would have run to the frame's edge. A second new
measure fixed it. A long title over two participants was cut off at the picture's edge. The picture is now as wide as
its title when the title is the wider, as a flow's is.

All three were found by looking at the first pictures drawn, and none by the drawing's tests. Then all 65
redrawn pictures were looked at, on 20 sheets dark and 8 light, and nothing more was found.

**What the look cost.** In the old picture a line had its sender's tone, so you could follow one participant's
calls by colour. In this look every call is one accent, and a tone is on a badge only. You were shown that with
the design. The key is what makes up for it: it says what each badge's tone means.

**Three small things CSS and SVG would not do.**

A gradient can't fill a straight line: a line has no height for the gradient to be measured in. So a beam is a thin
box, two high. A box has no arrowhead. So the arrowhead sits on a short path of its own that shows nothing else. The
moving dots are hidden for a reader who asked for no motion. So the arrowhead can't sit on the dots' path either, or
the direction would disappear with them.

**What didn't work, or cost time.**

I committed step 3 without its wrapped lines and its docs. A script failed, and the commit after a `;` went through
anyway. Fixed with a second commit. A commit now runs in its own command, after the checks. My check for lines over
120 characters counted a line ending that new files don't have, so it let 121 through. It now strips the ending first.
A test asked NUnit for "all equal to this, and five of them" in one constraint, which asks each item for a count. Two
assertions say it. The plan said 64 pictures and four drawing classes. It was 65, since the plan's own communication
picture is a sequence, and five, since a frame is drawn in two goes and earned a class.

**Where it sits.** Skill 18, testing and shipping: a refactor proved by what it does not move, and a rename
proved by identical images. Skill 20, extensibility: one layout that serves any look whose numbers it is handed,
and one shared look under two kinds of picture.

## An adapter that kept paying, and what a real chart found (F134.11, 2026-11-14)

**What it is.** The class chart, a project's interface design, was redone in the cards' look. A type is a glass
card with a badge that says what it is. A link's kind is in its line: violet for what holds what and what is
what, blue for what creates what, thin grey for what only uses what. You approved one design, and shaped it
while it was built: "should these lines not also be animated like the rest?", then, of a flow's line with its
word in a pill, "can we do it like this", then "parallel align" of a picture drawn with the old routing.

**The idea: an adapter that kept paying.** A class chart never had a layout. From the day the kind was added,
`ClassFlow` turns it into a flow, and the flow's layout places it. The reason written on the adapter is that
every fix a flow gets then reaches a class chart too. It did, three times here:

The new look needed only a drawing. No layout was written. F134.8 built a routing rule for flows, where the lines of a
gap turn on one line. Class charts were left on the old rule then. Switching them was one argument, and all 59 charts
kept their words and lines clear by the new rule the first time the check ran. The check that no two lines from
different boxes lie on one another held flows only. It took four lines to hold class charts too, and "parallel align"
became a test.

**What was built, in order.** A line's ends in the shared writer: a hollow triangle, a diamond, an open
arrowhead. The class chart's style. What a type is, read from the words of its kind, and the key. The drawing,
with a golden. The switch, and all the charts redrawn. Then the old flow drawing deleted, since the class chart
was its last user.

**What a real chart found that the golden's chart could not.** The drawing has to know which placed line is
which relation, since a flow's arrow doesn't say. I matched them by the arrow object itself. Twelve tests and a
golden passed. Then I drew core's real chart and it threw: the layout hands back a copy of some arrows, and my
test chart had none of those. The fix was to match a line by the two types it joins, and the test chart got such
a link. A golden made from a chart I wrote proves the cases I thought of. Drawing five real charts before the
commit is what found the one I hadn't.

**What looking found.** A "holds" pill sat on its diamond. A flow stands a word just after a line's start, and
only a class chart's "holds" has something there. The pill is now moved along its line past the diamond. It is
the one place this drawing moves something the layout placed, and it is written down as that.

**What the repo's own rules found.**

The folder rule, twice. `Svg/` and `Svg/Theme/Looks/` each held six source files, the most a folder may, so the new
enum and the new style each got a folder of their own. The second leaves one kind's style apart from the other four,
which is untidy and is noted for the refactor. The rule against `ToString()` on an enum we declare. A badge's letter
was the first letter of the enum's name. It is a switch now.

**What the frame became.** With a class chart writing its own title and key, no kind the docs draw was left to
get the frame's title bar or legend. Both were deleted from the code with their tests. Their style rules stay,
because the frame's style is in every framed picture's file and removing two rules rewrites all of them.

**What it did not change.** The layout. A chart is as tall and as empty on the left as before, and a line still
comes down a row where its target stands lower, which is what you asked about with "why not just straight".
Packing a chart tighter is a layout change, and was left out on purpose.

**What was measured.** 59 pictures redrawn, the docs' 58 and the plan's own, and each one's JSON says `classes`,
so no other kind changed. All 59 looked at on 14 sheets dark and 7 light. With the old drawing went 13 tests and
4 goldens that read its markup.

**What didn't work, or cost time.**

A Bash command over its length limit ran nothing, again. Long files are written with the file tool. A `\n` inside a
line of C# in a scripted swap became a real line break and broke the build, for the third time on this branch. Any
line that holds one is edited with the edit tool. I switched the renderer before running its new tests, so they first
ran green. I put the old renderer back, saw the three fail, and switched again. The order matters even when the
outcome is the same.

**Where it sits.** Skill 20, extensibility: an Adapter that made a new look a drawing and nothing more, and a
small general part, a line's ends, added to the shared writer where one kind needed it. Skill 18, testing and
shipping: real data drawn before the commit, and an old drawing deleted in the step after its last user left.

## A look in one place, and a rule that had to be looked at (F134.12, 2026-11-14)

**What it is.** The flow and the component diagram were the first two kinds redone, and the three kinds after
them settled on a look these two did not have. You said "make the theme of the flow and component diagrams
resemble the others, it is now a bit out of date". Now every kind opens the same way: one small title over a
soft light. A box is a plain panel with a little body. A decision is a box with pointed ends, your pick of three
after "these look a bit weird". The keys match the other kinds' keys. On a light page nothing is plain white any
more: "composite diagrams white is too white", then "all whites overall can be a tad darker".

**The idea: a look that lives in one place.** F134.8 moved what a flow and a component diagram share into one
shared look and one set of shared parts. That paid here. The title, the light and the panel changed in two
small files, and both kinds changed with them. Neither kind's layout was touched: a look is class names and a
few shapes, and a layout never reads them.

*A panel has body, and no depth.* A component diagram nests boxes four or five deep. The old look gave each depth its
own colour, by a rule a depth. Now a panel is filled with a little of the page's ink, the same wherever it stands. A
panel on a panel is a step deeper by itself, because the two fills add up. Six rules and six variables went, and a box
five deep still reads. *One rule, where two kinds show the same thing.* Step 4 wrote the words of a flow's key in the
flow's own style. Step 5 needed the same words in a component diagram's key. A second copy would have let them drift,
so the rule moved to the shared look, and the 93 flows changed in one class's name. *A style with no theme in it is a
constant.* The flow's and the component's styles each took a theme only to write a glow a tone. With the glow gone
they are plain text, and say so.

**A test that makes a variable earn its place.** The shared look sets a handful of colours for a light page and
again for a dark one. A test now holds three things: every variable a rule reads is set, a dark page sets the
same ones a light page does, and none is set that nothing reads. The third is what removed the old look's
leftovers. Each step that deleted a rule failed that test until the variable went too.

**The routing fix, which you asked for on the way.** Of one picture you asked "can we fix this here since we are
here", a dashed line that stepped right, down, right, down, and "why did it take a longer route? and why is
that line not straight", a line that swung out past its target and back. Both were one method, the one that
leads a long line past the boxes in its way.

It looked at each long line once, in the order written. A line was judged taken by a neighbour that had not yet moved
to a line of its own. Now the long lines are looked at again while one still moves. It moved a line only when one line
was clear the whole way. Now a line may run on the line it leaves on as far as it must and on the line it arrives on
from there. Failing that, it runs on a third line past the boxes: between two of a column, or 18 beside one.

The ways are listed best first and the first clear one is taken. A shortest path worked out column by column
would find ways with more steps, and no picture in the docs needs one, so it was not built.

**What looking found that the tests could not.** The two new tests passed, and the rule that keeps words and
lines clear passed on all 263 pictures. Then every picture the fix changed was looked at, before beside after.
One was worse. Two lines from one box had shared a line and forked. Now one of them had taken a line beside a
box that stood only 8 from the next box, and ran 5 beside its sibling. The rule was tightened: a line beside a
box keeps 18 from every box of that column. That picture went back to what it was, and the other 18 stayed
better. No test would have said "worse" there. Both pictures were valid.

**What was measured.**

Ninety-three flows and one component diagram were drawn in the new look, and each was looked at on a sheet after
steps 3 and 4, in dark and in light. The plan counted 91 flows; the other two were its own pictures. The routing fix
changed 18 pictures, and in one of them a line that crossed four others now crosses two.

The style-only change was proved by text: with the style taken out, each of the other 169 pictures was the same before
and after, at every step. No site page draws a decision, and the site's flows wear the site's own look, so nothing
changed on the site. The web's 933 tests and the site's 632 pass.

**What didn't work, or cost time.**

My first test for the routing fault used a small made-up picture, and it passed before any fix. It proved nothing. The
test that failed was built from the real picture's own boxes and arrows. A command over the length limit ran nothing,
again. A script goes in a file. I measured a line's distance from a box on a scaled-down screenshot and got it wrong.
The numbers are in the picture's file. Read them there. For step 6 I looked at three of six sheets, not all six. The
change was one rule, the same in every picture, and the shapes were proved unchanged. That is said in the commit, so
nobody takes it for a full look.

**Where it sits.** Skill 20, extensibility: what two kinds share lives in one place, so one change reaches
both and neither can drift. Skill 18, testing and shipping: a variable that must be read to be set, a style
change proved by taking the style out, and a rule change judged by looking at every picture it touched.

## A new kind as a translation and a drawing, and a doubt drawn before the plan (F134.13, 2026-11-14)

**What it is.** A new kind of picture: UML's composite structure diagram. It shows one class from the inside:
the parts it is built from, the ports on its edge, and which parts work together. I first read your "134.13
composite diagram" as the component diagram redone. You said "no do research", with UML's own line for it. You
picked design C of three, "Pins": a port is a named pin on the class's edge. You picked the flow's layout
through an adapter. And you gave a new rule: "the type of diagram must always display".

**The idea: a kind without a layout.** A layout is the expensive part of a kind: placing boxes, routing lines,
keeping words clear. This kind has none. A small class, `CompositeFlow`, turns a composite into a flow: every
part and port a box in one group, every connector and port an arrow. The flow's layout places it. So the kind is
five records, that translation, and a drawing. Every fix the layout gets from now on reaches it for free, as it
does for a class chart.

**Columns do the reading order.** What is asked of the class stands in the first column. A part stands one past
the furthest part that uses it. What the class needs stands in the last. That one rule makes the picture read
from what comes in to what goes out, with nothing placed by hand.

**The doubt, drawn before the plan.** The adapter had one risk: UML puts a port on the class's edge, and a
flow's layout knows boxes and groups, not edges. I drew the real class as a plain flow twice, with a throwaway
test, before writing the plan.

With the ports outside the group, the layout put them in a band above it. Three lines crossed the frame's top 18
apart, too close for three pins. With the ports inside the group, in the first and last columns, it came out clean:
asked on the left, parts in the middle, needed on the right.

So the drawing keeps the layout's places and draws its own frame: round the parts, its edges through the
middle of the pins. That is the one place the drawing decides. Had the second drawing failed too, the plan
would have been a different plan.

**A label as a measure.** The layout sizes a box by the length of its label. A part's box holds a large type
and a chip, which is wider than its words. So the adapter pads each label to the width the drawing needs, and
the drawing writes none of that label itself. It is a small trick, and it keeps the layout unchanged.

**What looking found.** You saw the first drawing in your editor and circled a pill: "line issue". A line ran
across the words of "the pipeline". Two lines that leave one part share their first stretch, and I drew each
line's word right after its own line, so the next line ran over it. A flow draws every line first and every
word after. Now this does too, and a test holds the order.

**What I got wrong.** After step 2 I ran only the diagrams' own tests. A rule in another project asks an
example in the guide of every kind the reader knows. It went red with that commit, and two commits went in
with it red before I ran it. The fix was the guide's section, which was due anyway. The lesson is the one from
the skill: a shared file is tested by everything that reads it, and an enum of kinds is a shared file.

**What was measured.** Three test projects run each step from step 4 on. The golden, and the real picture in
core's design doc: `AgentEngine`, six parts, five ways in, three things it is handed. Both were looked at dark
and light. The real picture is busier than the hand-placed design, because the layout orders parts by which
uses which. Every line can be followed, and the docs' rules for clear words and lines pass on it.

**What is left.** Only this kind shows its type. The other kinds get theirs in the refactor, where each
picture's UML type is settled: a `flow` draws several UML types today, so its type can't come from its kind.
The key's items are placed by the same few steps in a fourth class. A part's note wraps at a flow's width
though a part's box is wider.

**Where it sits.** Skill 20, extensibility: an Adapter onto a layout that exists, so a new kind costs a
translation and a drawing. Skill 18, testing and shipping: a doubt proved with a throwaway drawing before the
plan rested on it, and a rule test that belongs to everything that reads a shared enum.

## One project cut in two along a line that was already there (CL65.1, 2026-11-14)

**What it is.** The diagrams were one project. You said ".diagrams must be split into a diagrams --> and what
builds the diagrams". Now `LouisAgent.Diagrams` says what a diagram is: its kinds, its records, its reader.
`LouisAgent.Diagrams.Engine` builds one: the layouts, the routing, the drawings, the looks, the writer. The
engine references the diagrams, never the other way.

**The idea: nobody takes what they don't use.** That is the Common Reuse Principle, which the repo already
follows for the API's contracts. Something that only writes or reads a diagram's JSON, a pack's tool or an
agent, should not carry four thousand lines of drawing code to do it. The small project is what both sides
agree on.

**The line was already there.** Before moving anything I asked of each file: does it say what a diagram is, or
build one? A file that said what a diagram is used only other such files. No record reached for a layout. So
the move was 130 files changing project and namespace, and no code changing what it does.

**The one thread across.** The reader's refusals and the drawings' words were one enum with one formatter.
That was the only real coupling, and it was cut first, in its own commit, while there was still one project:
two enums, and one formatter that fills either. Doing it first made the move itself mechanical.

**Two commits became one.** The plan moved the drawings first and the layouts after. But a key's layout uses
the drawing's words, and a drawing uses a layout's sizes. A half-moved state would have had the small project
open its internals to the engine, for the length of one commit. One move has no such in-between, so it was one
commit.

**The proof is that nothing changed.** After the move, the docs' tool redrew all 268 pictures and wrote none.
No golden changed. That one check says more than the test count: every layout, every line and every style
string came out the same, byte for byte.

**A rule so it stays split.** A test holds four things: the small project references only the enum-name
parser; the engine references only the small project; the small project holds no layout, renderer, drawing,
look or writer and never names the engine; every file of the engine is in its namespace. I saw it fail before
the move.

**What I got wrong.** I guessed two hundred files would move, and 130 did. I counted the enum's members as 69,
and there are 68. Both numbers were in the plan before I had counted properly, and both were corrected there.
And the shell halves a doubled backslash inside a script it is handed, which broke three scripts before I
wrote them as files.

**What was left as it was.** One test project still tests both, because most kinds' tests mix a reader's case
with a drawing's and share a sample. One design doc covers both projects and the tool, because its story
follows one picture across all three.

**Where it sits.** Skill 20, extensibility: a small project both sides agree on, and a rule test that keeps the
dependency one way. Skill 18, testing and shipping: a refactor proved by an output that does not change by a
byte.

## A kind is how it is laid out, a type is what it shows (CL65.3, 2026-11-14)

**What it is.** You sent a page of UML's diagram types and said "our graph types must match", and then "the type
of diagram must always display". Now every picture says what type of diagram it is, at the right of its title's
line: "Sequence diagram", "Class diagram", "Overview". And a flow's file has to say which type it is.

**The idea: two questions that looked like one.** A picture's `kind` answers "how is this laid out": boxes and
arrows, lifelines, cards. Its `type` answers "what is this a picture of". For most kinds the two go together.
Lifelines are always a sequence diagram. For a flow they don't: the same boxes and arrows draw the steps of a
procedure, the parts of a system, or the objects in memory. Your pick of three was to keep a kind a layout and
let a picture state its type.

**A default where there is one answer, a refusal where there are several.** That is the rule for data someone
else writes: fill in what can only be one thing, and refuse what you would have to guess.

Five kinds are only ever one type, so their files write nothing and the reader fills it in. A flow may be eight, so
one that says none is refused, with the eight. A ticket is a card in a chat. It has none, and one that writes a type
is told to take it out.

Each refusal says what may be written, so the person who gets it knows the fix without opening the guide.

**Reading 95 flows.** I gave each flow its type by reading it: 54 component, 16 activity, 16 object, 6 package
and 3 deployment. The surprise was the first number. Most of our pictures titled "how it works" are not
procedures. Their boxes are parts, and their arrows are calls. Only 16 flows have steps, a start and an end.
None is a state machine, a use case or a communication diagram. That says something about our docs: we draw
what a thing is made of far more often than what it does in order, and we leave the order to the sequences.

**Proving a change to every picture.** 267 of the docs' 268 pictures were redrawn, in two steps. The one left
was the composite, which already said its type. "They all changed" proves nothing, so a script took the type's
line out of each new picture and held the rest against the commit before. 260 were the same but for that line.
Seven were also wider, because a title and a type now have to stand side by side. I had counted which pictures
would grow before changing anything, and the count named those seven.

**What went wrong on the way.**

The plan put the type's names in the engine's `Messages` folder. A rule test holds that a `Messages` folder has only
message enums, so they are in a folder of their own. I wrote the plan's map without asking which rule tests read that
folder. The docs' test first looked for the type's line at the top of a picture's file. The docs draw a picture inside
a frame, one level down. So the test failed for every picture when the code was already right. Seeing a test fail is
not enough. It has to fail for the reason you expect. The import from Mermaid wrote flows with no type. Its own tests
found it the moment the refusal landed. It writes `activity` now, which is what UML calls a flowchart. The builder had
a step that kept two kinds as wide as their body whatever their title. With a type on the title's line, that would let
a type stand over a long title one day. I counted first: none of the 46 pictures of those kinds needed it. So the step
is gone, and one rule gives every picture its width.

**What is open for you.** Ten of the flows' types are doubtful, and listed in the parent's plan. A body narrower
than its picture stands at the left in the seven that grew: whether it should stand in the middle is your call.

**Where it sits.** Skill 5, structured outputs: a default where there is one answer and a refusal that names the
choices where there are several. Skill 18, testing and shipping: a change to every picture proved by taking the
change out again, and a count made before the change that says what should differ.

## Six methods that did the same thing, and one builder (CL65.2, 2026-11-14)

**What it is.** Six kinds of picture each had a method that put the picture together: work out its width and
height, draw the light and the title, place the body and the key, write the caption, join the styles. You said
"then use builder pattern for implementation", and picked one fluent builder with each kind's drawing as its
director. Now a drawing places with its own layout and tells `PictureBuilder` its parts. The builder does the
rest, once.

**The idea: write the steps once, and let what differs be said by name.** A Builder puts a complex thing
together step by step. The trap is a builder full of "if this kind, then". This one has none. I read all six
methods line by line first and wrote down what differed:

Four numbers: the room above, the room before the key, how far down the light is, how deep it is. They became one
record, with a flow's as the usual. A sequence hands in its own. Each kind's own fades, and the order they are defined
in. Whether a long title widens the picture. For a cards picture and a component diagram it does not. Where the light
is. A cards picture has one behind each highlighted card, so it hands in its own.

Each of those is something a kind tells the builder. So the builder never needs to know the kind.

**Nothing is written until the end.** A key's rows wrap at the picture's width, and the width depends on the
title, which may be wider than the body. So the key is handed over as a function of the width, and the builder
asks for it last. That one fact decided the builder's shape: it holds what it is told, and writes everything in
`Build`.

**The proof was the same as for the split.** After the six kinds moved onto the builder, the docs' tool redrew
all 268 pictures and wrote none, and no golden changed. A builder that put one part a pixel off, or defined one
fade out of order, would have shown as a changed file.

**A rule so it stays.** A test holds that nothing under the engine's renderers but the builder writes a title, a
caption or the SVG itself, and only a cards picture a light. I wrote it after the drawings had moved, which is
the wrong order. So I ran it against the drawings as they were, to see it fail, before trusting it.

**Two smaller things in the same child.**

Four kinds placed their key's items by the same loop with the same five numbers. One placer does it now. Five of seven
renderers only handed on to their drawing, in a folder that was full. Each now stands beside its drawing. That move
had to come before the builder, not after as planned: the builder's two files were a seventh and eighth in a folder
that holds six.

**What you added on the way, for later children.** The pictures a project needs should follow from its type
and its requirements, and stand where the text needs them. And the engine's arithmetic should be small types
that carry their own sums, on Microsoft's `RectangleF` and `Vector2`.

**Where it sits.** Skill 20, extensibility: a Builder with a Strategy as its director and a Parameter Object
for what differs, so a new kind writes a body and a key and gets the rest. Skill 18, testing and shipping: a
refactor proved by output that does not change by a byte, and a rule test seen failing even when it was written
late.

## Sums that live on the thing they are about (CL65.6, 2026-11-14)

**What it is.** You looked at the engine and asked: "with all these math in the egine, can we not have something
like a calculator which does it neatly." Then: "we cannot write spaghetti code". Now a box knows its own edges
and middle, a face knows how wide words are, and a path writer writes a path. A layout and a drawing ask them,
and work nothing out where they stand. The rule behind it is ADR 0123, *Ask the type*, and this was the first
code brought under it whole.

**The idea: a number with a meaning wants a type.** The engine held a box as four bare numbers, in thirteen
records, some spelled `W` and `H` and some `Width` and `Height`. Every place that needed its right edge added
the left to the width again. I counted before planning: a right edge worked out in 33 places, a bottom edge in
38, half a size in 39, words measured by their length in 20 with 11 different widths of a character, and a path
built as text in 20. That shape has a name, Primitive Obsession, and its cure is a small value type that carries
its own sums. Your pick was small types, not one calculator class: a calculator would still be handed the four
numbers each time.

**Two families behind one seam.** You wanted Microsoft's `RectangleF` tried and ours kept: "our own will be
Box<T>", then "keep ours and MS's but use ours." So a box and a point are interfaces, and one geometry makes both
of the same family. That is an Abstract Factory. Ours is written once over any number type, by .NET's generic
math, and runs in `double`. That choice paid twice. Run in `float`, our own box answers exactly what Microsoft's
does. So when Microsoft's differs from ours in `double`, we know all of it is single precision and none of it is
the library.

**The proof was a picture that does not change.** Every step from 5 to 9 moved one kind onto the types. After
each, the docs' tool redrew every picture and had to write none. It wrote none of the 269, after every one of
those steps. To keep that, each sum on a box is written in the order the layouts wrote it by hand: `left + width / 2`,
never `(2 * left + width) / 2`. The two are the same number on paper and can differ in the last digit in a
computer, and a layout that halves a number and rounds it to one decimal shows that digit.

**A ratchet, and then just a rule.** A test read the engine's source for four shapes and held a list of the
files that still had each: 13 records, 22 files, 10 files, 16 files. A file new to a list failed it, and so did
a listed file that had been cleaned and not taken off. So each list could only shrink. It caught its own author:
in the last kind I wrote "where a line of words ends" as a start plus a width, and the test named my new file.
With the lists empty the test is four plain rules, and a fifth: no drawing picks its own geometry.

**"Always fix touched files" decided the size of a step.** The flow could not move in two commits. The class
that turns a picture, the router and two drawings each read a box and also read an arrow's path back as text,
number by number. Touching one meant fixing it whole, and fixing it whole reached the next. So the flow was one
commit of 66 files, and an arrow's way became a type of its own, which the plan had not listed.

**Tests moved by the compiler's own word.** The records lost their four numbers, and in the flow's step alone
268 places in the tests read them. Rider's rename can't resolve a record's positional parameter. So a script built the tests,
took each place the compiler said no longer compiled, by file, line and column, and replaced the member there
and nowhere else. A search and replace over `X` and `W` would have changed things that were never a box.

**What both families draw.** A test now draws each of the docs' pictures three ways. There were 269 when it
was measured, five of them this plan's own, which went with the plan. Microsoft's family
refuses none. Our own in `float` draws every one exactly as Microsoft's, to the byte. Beside ours in `double`,
184 pictures are the same, 78 have a few numbers a tenth of a pixel apart, and in 7 flows an arrow takes another
way. I looked at one of the seven: two arrows that arrive at the same box had swapped places on its top edge,
and the picture read as well either way. Which of the router's decisions turn on single precision I did not look
into.

**What went wrong on the way.**

The moves were not test first. They were held by the pictures, and the tests for the members a box and a point learnt
on the way were written after the code. Each commit says so. A member built ahead was thrown away. Step 2 gave a box
"joined with another" because a box obviously joins. Nothing ever asked, a group's frame asks the geometry for the box
round several, and step 9 took it out. The rule is in the ADR: a member is added by the step that first asks for it.
My first count in step 10 said 84 pictures differed in shape. The pictures were fine and the count was wrong. The same
comparison with one option of the regular expression taken off said 2, and reading both drawings as XML said every
picture has the same shapes in the same order. A measure gets checked like code: by a second way of counting. The
ticket wrote its paths a third way, with a space before each letter. I moved it onto the writer the other drawings
use, so two golden files changed, by spaces only. A golden that changes in a refactor needs its own proof, so I read
each path as letters and numbers before and after and held them equal.

**What is open for you.**

The names picked unattended: a pin's `On` for the box it stands on and a placed box's `Component`, since `Box` is the
box itself now. The ticket's paths on the common writer, which changed its golden by spaces. The bound in the last
test, nine pictures in ten the same to a tenth. It is loose on purpose, since the docs' pictures change with every
plan. Left for CL65.4's list: one way to write a path where there are two, the look's glass and accent defined with
the same numbers in five drawings, and the name of the `ticket` kind now that it is no longer drawn as one.

**Where it sits.** Skill 20, extensibility: an Abstract Factory with two families, and generic math so one type
does its sums over any number. Skill 19, measuring: what single precision does to a picture, shown by running
our own type in `float` beside the other. Skill 18, testing and shipping: a ratchet whose lists only shrink, a
refactor proved by output that does not change by a byte, and a second implementation that is only kept if
something runs it.

## Taking out what nothing draws, and proving a rewrite by undoing it (CL65.4, 2026-11-15)

**What it is.** Every kind of picture was redone in one look, a kind at a time, and each time a little of the
look before stayed behind: style rules for shapes nobody draws, two layout rules no picture is laid out by, a
second way to write a path. This cleanup took it out. The engine now holds only what a picture is drawn with
today, and three rule tests keep it so.

**The idea: code that is tested looks alive.** The two old layout rules, arrows that join and boxes centred,
were drawn by nothing. Every drawing and the web's `GraphView` asked for the new pair by name. But the layout's
own default was still the old pair, so 47 calls in the tests laid a flow out by rules no picture had. They
passed, and passing made the old rules look used. I measured before planning: with the default switched for a
moment, 8 of the 714 tests failed and 706 held. So I read the eight one by one. Two were about the rules that
went, and went with them. Six were about the layout and now hold under the rules that are drawn. A test says
the code works. It does not say anything uses it, and a default is where unused code hides.

**A strategy is worth its interface while it has two.** The two rules sat behind two interfaces and a record
that paired them. With the old pair gone each interface had one implementation, and ADR 0055 says that is a
class. So the layout asks `ApartArrows` and `LevelRows` themselves. Three of the answers on those interfaces
had only existed so the two rules could differ. How far apart two lines keep became a number. Whether an arrow
may pass between two boxes was always yes, so the branch it guarded went too. A seam costs more than its lines:
it keeps questions alive that have one answer.

**A rule test written first found more than I had.** The plan named two things styled twice, a word's pill and
the faint line over a key. I wrote the test first, that no kind's style has a rule a shared look has, and it
named eleven. A box's name, a class's kind, the icon in three kinds' badges, a sequence's ring, a card's text:
each had a twin. All eleven are folded. Reading the styles had found two, and the test found the rest in a
second.

**A change to every file, proved by undoing it.** Three steps rewrote every picture of the docs: 283 files,
then 277, then 161. Nobody can look that over. So each step had a script that takes exactly its change out
again, and what is left had to be the commit before, to the byte.

*Class names moved to the shared look:* map the eighteen new names back, and every drawing is the same. *Old rules out
of the style:* compare the drawings and leave the style out, then read the two styles as rules: 61 went and 11 came,
and each of the 11 is named. *One way to write a path:* read each of the 2,458 paths as its letters and numbers, and
hold them equal.

After each I also drew a few pictures before and after in headless Chrome and compared the pixels. They were
the same. The script proves the file, and the pixels prove the script reads the right thing.

**Move the readers first, then change what they read.** An arrow's path was written with a space round every
letter, and every other path without. The writer's own remark said why: other code read that text back. The
other code was the tests, about 45 lines that split an arrow's text on its spaces to find where it turns. I
moved those onto asking the arrow for its places, through one small helper, and left the text alone. All 696
tests passed, so the move had changed no test's meaning. Only then did the text change. Had both changed in one
go, a red test could have meant either. Then I tried keeping an arrow's numbers exact, where they had been
rounded to the one decimal the text held. The docs' tool wrote none of the 269 pictures, so the rounding by
writing and reading a text is gone.

**A fade is named once.** The look's accent was written out in six drawings and its glass in four, each time
with its id, the way it runs and the same stops. Now a fade is a small record, named beside the style that
colours its stops, and a drawing asks for it by name. The plan named the look's three. Six more were written
out the same way, so nine are named.

**What went wrong on the way.**

Step 2 was planned as a refusal and built as a default. The plan said a cards picture with no `links` is refused by
name. Read again, no links can only mean none, and one picture of the docs already says so. The rule from CL65.3 is a
default where there is one answer and a refusal where there are several. I had planned against our own rule. One test
had no chart left. It laid arrows out by the new rule on boxes placed by the old one, a pair no picture ever had.
Under the drawn rules its chart does something else, so the test now says what that is. The first rule test for the
fades named the writer too. The writer is where a run becomes markup, so it must name one. I read the list a failing
rule test gave me and narrowed the rule to the drawings. A rule test's first failure is read like a finding, and not
only made to pass. The diagrams' own interface picture, drawn from the code, had 24 boxes and 30 lines, and its middle
was a knot. It is two pictures now, the way in and a kind's own parts. The old one read well because it left two
renderers and the builder out.

**What is open for you.**

The picks made unattended, in CL65's plan as of commit `a9a875cda`: the strategy folded, the cards' default, one test
project and one design doc for the two projects, a kind's class beside a shared one, nine fades, two pictures. The
names of two kinds, each put to you there with three options: `components`, which reads as UML's component diagram and
shows a composite structure, and `ticket`, which is a glass card now. Since then `components` is `nested`, your pick,
and `ticket` is CL66. I could not read uml-diagrams.org for the first, its certificate failed, so that finding rests
on the GeeksforGeeks page you named. Six messages still named `Legend…`, from when the docs' frame had a legend.

**Where it sits.** Skill 18, testing and shipping: a rewrite of every file proved by taking the change out
again, a rule test written before the cleanup it holds, and readers moved before what they read. Skill 20,
extensibility: when a seam earns its keep, and when a strategy is a class. Skill 5, structured outputs: a
default where data someone else writes can mean one thing, and a refusal where it can mean several.

## The pictures a project needs, and a rule that reads the docs' own words (CL65.5, 2026-11-15)

**What it is.** Every project doc used to show the same four pictures, whatever the project was. You said the
project's type should decide: "the project type and requirements must drive which diagrams is required", and "if
diagrams is missing add them". Now each doc says what kind of project it is about in its first lines, its kind
sets the least it shows, and the doc names what it shows beyond that. A test holds every doc to exactly that, and
the 27 pictures the new rule found missing are drawn.

**A rule a person writes once, and a test that reads it.** The floor is a table in the guide: a library shows its
main path and a sequence, a host where it runs, a web project what its main page is made of. The same table is a
switch in the test project, `ProjectFloor`, and a test reads the guide's rows and holds them equal to the switch.
So the rule a person reads and the rule the test keeps can't drift apart, and changing one fails until the other
follows.

**The doc's first lines are the data.** A doc already opened with a `**Kind:**` line in free words: "class
library", "Blazor WebAssembly app", "library, ASP.NET Core minimal API". Its first words are now a kind from a list
of nine, read with the same parser every choice in the code goes through, and the free words move into brackets
after it. A second line names what the doc shows beyond its floor, by the type names the pictures carry. Held as an
equality, not a minimum, those two lines are a true list of the doc's pictures: a doc that shows more than it says
fails too.

**A ratchet, as CL65.6 had.** When the rule first ran, 27 pictures were missing: 14 main paths, 8 places where a
host runs, 5 web pages. The test listed them by doc, and a doc whose picture was drawn had to come off the list.
Four steps later the list was empty and the rule a plain one. A list that only shrinks lets a rule land before
the work it asks for is done, without a single day where the rule is off.

**Every picture is a claim about the code.** A deployment diagram says which container a host runs in and what port
it is reached on; I read the compose files, the Dockerfiles and the programs for each. An activity diagram says
what happens in which order; I drew only what the doc's own story and the code it names confirm. Where they didn't
settle an order, as in a web fetch, where I could not confirm whether robots.txt is asked before a replay is
read, I drew the order the doc does state and left the rest out.

**No section apart.** You also said "graphs must also be placed where it is used ort needed not a section". 40
docs kept their pictures in a *Diagrams* section. A test now fails on one, and each section's pictures joined the
story they explain, its few sentences that pointed from one section to the other said again for where they stand.

**What went wrong on the way.**

The first kind line I read stopped at the first comma, and two docs' old words had a comma before their path. The
parse was right and the line was wrong: the old words went into brackets after the kind. A file of secrets holds the
port the API is reached on, so I could not read it, and the doc and the compose file disagree. The picture says
`API_PORT` rather than guess a number. Moving a picture to the exact paragraph it explains, rather than to the end of
the story, needs a person to read each doc. Each picture now closes its doc's *How it works*; finer placing is left
for when a doc is next touched.

**What is open for you.**

The picks made unattended, in CL65.5's plan as of commit `a9a875cda`: nine kinds where the floor you saw named five,
the equality rather than a minimum, a web project's composite structure drawn as a nested picture, and the rule's
types in the test project. The `ticket` kind's name, CL66 in the TODO, with its three options in CL65's plan as of the
same commit.

**Where it sits.** Skill 18, testing and shipping: a rule stated once for people and held by a test that reads it, a
ratchet that lets a rule land before its work is done, and a doc's own lines used as data. Skill 17, observability
of a design: every picture is drawn from what the code and its compose files say.

## The look held against the design, and the flow walked three times (F134, 2026-11-15)

**What it is.** F134.2 to F134.5 were built without a stop after each step, and every one of them passed its tests
and its own live check. Then you looked at the local site and it was not the design. This is the pass that held
the site against the J and K mockups, side by side in a browser, and then walked the whole flow with the real
guide until it went right three times in a row.

**What the tests had not seen.** Every fault here had green tests over it. The guide's answer stood in a folded
box with "PROCESSING" in it, the site's chips stood on top of that, and both stood on the start's dock. Each part
was tested alone, and nothing tested the three together on one screen. The caption was as wide as the bar, with
the talk's big cards stacked in half of it. Its test asked that the choices were there, which they were.

Turning the caption over threw it back over the bar. The module marked the caption with a class, and the page
writes the caption's classes again when they change. A test of the module's names can't see that. Only the press
did. A choice to write in your own words did nothing on a demo's page. It was a label for the box by its id, and
that page has the business's own chat with a box of the same id. The test page had one box.

And on the start the guide offered "Ask it live", asked a business that has no window there, and told you to
answer in a window you could not see. Its instructions were written when the start was a page of covers, and no
test walks the guide.

**What that teaches.** A markup test proves a part exists, and a style test proves a rule is written. Neither
proves the page looks right or that a walk makes sense. The mockup is the test for the look, and it has to be
opened beside the built page, at more than one window size, before a child is called done. The walk is the test
for the flow, and it has to be made by pressing what a visitor presses, more than once, since the guide does not
answer the same way twice: in one round it met you at a demo's door with three questions and no walk, and its rule
had to say "exactly four choices, in this order" before it held.

**How the rounds were run.** A small script in the browser read the page's state after each answer as data: the
address, the orb, the chips, the caption's words and choices, where it stood, what was lit, whether the live
window was on screen. A screenshot showed what data can't. Three rounds each took one business from the start's
tour, through "Open this demo", a question at the door answered live in the business's window, the walk of the
demo's page, the caption turned over, the whole talk, an answer put away, a pressed note and "Stop here". The
parts that changed after a round were run three more times on their own.

**Sharing one browser.** You were pressing in one tab while I drove another, and twice I drove yours. A tab that
is not in front has no size, so every measure from it read nothing. A fresh tab of my own fixed that. A frame on a
scratch page stood in for a phone and for a short laptop screen, since a maximised window takes no new size.

**What it cost.** About US$0.28 for 545 requests on Haiku 5.5, by the usage ledger, from 05:23 to 08:08 UTC. That
is everything asked of the local site in that time, your own presses included.

**The same lesson once more, on the demo itself.** After that pass you looked at a demo's page and said the demos
themselves were never redesigned. They weren't. "Redesign each demo to fit the new fancy style" had been read as
the page round the window, and the website inside it stayed as it was: every size in fixed pixels, drawn for a small
window, and 660 high whatever the screen. On a 2560 screen that was a postcard in a 2,400-wide
frame, with 13 px words and photos capped at 200 px high. No test saw it, since each rule was written as it was
meant.

What fixed it was one size the mock names from its own width and height, with everything inside it in em. The
window takes the height the first screen leaves, and the chat stands beside the website as K drew it. Then you
said it must fit 1080p, so that was the size it was held to: a headless browser driven over its debugging port
gives an exact 1920×1080 where a maximised window gives whatever it is. The same script took 2220×1025, 1366×768
and 390×844, and the phone picture caught a fault of this very change, a window that had dropped to 460 high with
12 px words.

One thing stopped short. The websites were to gain sections, and a test refused: it holds each website's places
equal to the list its recording was sent, since the agent could only show what it was told was there. A new
section means recording the conversation again, and that is a run to ask for, not to slip in. The home page's
words were sent too, so the paragraphs first added there came out again, and the websites gained photos on the
cards they already had.

**The photos (F134.6).** You chose the three candidates from a page that set each beside the photo it would
replace. At that page's size they were clean. At the size a hero draws them, two were not: the stoep had a real
business's sign and a flag in it, and the study had a painting of people along its top. The showroom's rule for
photos (ADR 0115) is no recognisable person or brand, so both were fetched again, cut from the bottom, and the
stoep lost its string lights along with its sign. A photo is checked at the size it will be shown, not at the size
it was chosen. The photo host serves WebP itself, so nothing had to be converted. The three files are 247, 115 and
49 KB.

**Where it sits.** Skill 18, testing and shipping: what a test can prove and what only looking can, and a walk by
the real agent as the last check before a thing is called working. Skill 4, tool design: the guide's rule for the
start is a prompt rule, and a tool that refuses to ask a business with no window on the page would hold it harder.

## Every answer turns over to the ledger's own numbers (F134.5, 2026-11-15)

**What it is.** The guide's caption now has a back. "How I did that" turns it over to the answer's work: the tools
it called in order, each with what it was about, how long it thought, and its usage line, the tokens by kind and
the cost, with the exact figures in the tooltip. The front carries the cost beside the guide's name. The bar carries
what the whole talk has cost so far, which opens the drawer, and the drawer ends with the same total. On a demo's
page each "could also" note became a piece the visitor can press to ask about, and the bar shows the selected
piece's chip, as the corner always did.

**Nothing new was measured.** The research found every number already there: each turn's usage, which the API
priced from the usage ledger and sent with the answer, the chat's sum of them, and one class that writes them. So
this child adds a reading, `AnswerWork.Of(turn)`, and a drawing, and no number of its own. That is what made the
done-when a fair test: the back of a live answer said 4 requests, 10 input, 20,766 cache write, 59,574 cache read
and 1,177 output tokens, US$0.00378, and the ledger's four rows for that turn sum to exactly that. Showing a person
the framework's own record, rather than a second count of it, is what keeps the two from ever disagreeing.

**What the back honestly shows.** The first live answer listed `Offer` twice: once by its name alone, once with its
question. The pack had refused the first call and the model offered again. The back shows what happened, a refused
call included, because it reads the turn and nothing else. Whether a refused call should be marked as refused is a
small question left open.

**Reuse.** The chip in the bar is the corner's chip: the widget's four chip rules now reach into the bar, rather
than being copied into its styles. A note became a piece by doing what a tile does, an id from its title through
`PieceId`, the piece mark, a heading the scripts read, and the shared pressed look.

**What looking found.** The caption beside a place is 340 wide, and its foot squeezed two buttons beside the way
into the box, each wrapping onto two or three lines. The foot wraps now. Tests said the buttons were there; only the
screen said they were cramped.

**What it cost.** Two live walks of the real guide on Haiku 5.5: US$0.0074 for seven requests.

**Where it sits.** Skill 13 (cost and token economics): every answer's cost from the same ledger the owner is billed
from. Skill 17 (observability): the framework's own records of an answer, shown to the person who asked.

## The agent runs the room, and nothing moves that it didn't move (F134.4, 2026-11-15)

**What it is.** The guide at the foot of the showroom now runs the room from there. When it shows a place, its
answer stands as a caption beside that place on a wide screen, with the choices it offered drawn in it, the
suggested one marked. When it shows a demo on the start, that demo's window comes forward. While it talks, a
business's recorded demo holds its play, and goes on when the guide is done. And the whole talk opens as a drawer at
the side, or as a sheet on a phone.

**Nothing moves on its own.** F134's rule 2 says nothing on the page moves unless an agent's tool call caused it.
So the caption doesn't place itself where it likes: the guide's own call lit a place, the widget's script leaves a
mark on it, and the caption stands beside that mark. The window that comes forward is the same story. The guide
shows the demo's address, the script presses the place the address names, and that place is the dock's button.
That one was already true before the step started, found while the plan was researched, so its step is only a test.
A behaviour that already works is still worth pinning, and the test was seen failing once by taking the dock's mark
away.

**Reuse twice.** The caption's choices are the talk's own cards, so a choice looks and answers the same in both
places, a box choice too. And the shared state that tells a demo the guide is talking is the same Observer the
showroom already used twice, registered beside them. Neither needed a new idea.

**A race, caught by a flaky run.** The bar told the page "talking" straight from the runner's event. The runner
raises that event from its stream and from a redraw timer, on different threads. A late timer call could read
"streaming" just before the end and leave the guide talking for good, which would hold a demo forever. The test
passed alone and failed once in a batch, with the wrong two values. Telling the page on the bar's own dispatcher,
reading the runner as it is then, closed it, and the test passed three runs in a row.

**What looking found.** The tests said the caption was right; the screen said it was crowded. With a caption
standing, the site's suggestion chips stood over it too, two rows of shortcuts at once. On a phone, with the whole
talk open, the chips came back above the sheet and covered the header's button. Both are now held by tests: the
chips give way to a caption and to the open talk. The CSS also had a trap: the bar was centred with a transform,
and a transform makes the bar the frame for anything fixed inside it, so the drawer couldn't reach the screen's
edge. Centring by margins put the bar where it was and freed the drawer.

**What it cost.** Five live turns of the real guide on Haiku 5.5: the caption beside the restaurant's page head
(US$0.0042), the drawer (US$0.0034), and a walk of the start and one at 390 px (US$0.0049 together). About
US$0.0125 in all.

**What went wrong on the way.**

- The caption's first test streamed a turn through the client and failed inside the stream reader, on the fixture's
  shape, not on the caption. It now hands the bar a runner whose chat holds the turn.
- The held recorded chat's test timed out at bUnit's one-second wait, though the play was right: the whole recording
  plays once let go. It waits five seconds now.
- On the start the caption stands beside the dock's button the guide lit, so it covers the right edge of the window
  that came forward. Lighting the forward window instead would place it better; it is left for Louis to decide.

**Where it sits.** Skill 4 (tool design): every movement on the page is a tool call's effect. Skill 20
(extensibility): a player that can wait, told by a state any part can read.

## One engine, a second look, and an orb that only says what is true (F134.3, 2026-11-15)

**What it is.** In the showroom the guide was a button in a corner. Now it is a bar at the foot of the page, as the
J and K mockups drew it: an orb that shows what the guide is doing, a line of words saying so, a box that is always
there to type in, and the guide's suggestions as chips. You asked for it as "chatwidget layout type", and that is
what it is: the same widget with a second layout, not a second widget.

**One engine, two looks.** The widget already did everything a chat needs: its runner, the session, the page that
travels with a message, the guide's way of showing places. A bar that did those again would drift from the corner
the first time either changed. So the bar is only a view: it reads the widget's runner and hands a send back to the
widget. The test of that is simple: the corner's two golden files didn't change by a character.

**The orb reads, it never guesses.** An orb that pulses while the agent is idle would be a small lie on screen.
Its state is a rule over three facts the chat already knows: a place was just shown, the answer is streaming, the
answer's words have come. Whether the words have come is the turn's own knowledge now, `EndsInWords`, asked where
it lives. The rule is a plain class with its cases tested, and the component only draws what it is told.

**A limit that shaped the design.** The widget's code-behind sat at its 200-line limit, so every new parameter cost
a line somewhere else. The size's classes moved onto the size's own type, which was also the cure for a switch two
components had copied. And the suggestions came in the way a site names its other parts, through its areas
builder, so the widget didn't grow at all. A size limit is a design pressure, and here it pushed things to where
they belong.

**A live check, and what it found.** The real guide walked the start, and a typed question mid-walk was answered,
with its next choices after it: two turns, five requests on Haiku 5.5, US$0.0051. The look at 390 px found what the
tests didn't: the chips wrapped to three rows on a phone and stood over the end of every demo page. One sideways row
fixed it.

**What went wrong on the way.**

- The talk's test raced the stream's redraws and passed only sometimes. It waits for the orb to be idle now, and
  passed three runs in a row.
- The phone fix was written before its test, from what the look showed, and the commit says so.

**Where it sits.** Skill 4 (tool design) and skill 17 (observability): an orb that shows the agent's state read from
the chat itself. Skill 20 (extensibility): one engine with a second layout.

## A page restyled without moving the guide (F134.2, 2026-11-15)

**What it is.** After F134.1 the start was a stage and a demo's own page still wore the look before it, so walking
in from the start dropped a visitor into an older page. Now a demo's page stands in the same light, in the K
mockup's look: the title and the lede side by side, the player the stage's whole width, what to watch as a row of
chapters under it, and the rail a slim column of icons. The coding agent and the triage team, whose players had no
window, now stand in one.

**The places held by their ids.** The guide's tour stops at a demo page's places: the story, the demo, what to
watch, the notes and the offer. Each is a section with an id. A new look could easily move one, or wrap two in a
new box, and the tour would stop somewhere else. So the tests hold the order of those ids, and a redesign is free
to change everything else. When the cues moved from beside the player to under it, the change was one fewer box
round them, and the test of the order failed first on exactly that.

**When the plan's part was the wrong part.** The plan drew the rail with the start's dock. Looking closer, the dock
draws buttons that tell the page what was pressed, and the rail's demos are links: each is a page of its own that
an address, a search engine or the guide opens. Turning them into buttons would have cost every demo page its link.
So the rail kept its links and took the dock's look. A general part is for what it does, not for how it looks.

**What went wrong on the way.**

- The phone's rules were written with the parts they stack, so the phone step's test held them rather than driving
  them. It passed on its first run, and the commit says so.
- The window was feared to cut a long recording, since a framed window cuts what doesn't fit. It doesn't here: the
  frame fills the place it stands in, and a player's place has no fixed height. Measured on the triage page, the
  player and the window's body are the same height.

**Checked by eye.** Each demo page at full width on the local site, and the guesthouse, the coding agent and the
triage team at 390 px in a frame of a phone's width, none scrolling sideways.

**Where it sits.** Skill 18, testing and shipping: a page's places held by their ids, so a new look can't move the
guide's stops. Skill 20, extensibility: a general part used for what it does, not for how it looks.

## A new kind of picture, then one for every project that has parts (F134.7, 2026-11-15)

**What it is.** You asked that "all projects which has components should have an component diagram in their
documentation". So the diagrams gained a kind, a component and everything it holds, drawn as boxes inside a box
with the pins that carry what is handed down and said back up. Since CL65 it is called `nested`, and its UML type
is a composite structure. Every project that holds Razor components now shows one of them, and a test fails when
one doesn't.

**One picture is of one component.** The plan gave a project one picture. Of the guide's "its main component" you
asked "of it's main components? plural". A test can't tell which components are main, so the rule asks for at least
one, named by the picture's subtitle, which starts with the project's name. Which ones are main stays a person's
judgement.

**Every picture is read from the markup.** A box's pins are its real parameters and callbacks; a neighbour is a
service the code-behind injects or an address it calls. Before each picture I read the page's `.razor` and its
`.razor.cs`, and where the two disagreed with the plan, the code won: the dock takes a record a button, not a
function per fact, so its picture shows the record.

**A rule seen failing on exactly the right five.** The last pictures were set aside, and the rule ran: it named the
four packs' web views and the starter, and nothing else. Back in place, it passes. That is the smallest proof a rule
reads the right thing: it fails on the cases you expect, by name, and on no others.

**What went wrong on the way.**

- The starter has no design doc, so its picture had nowhere to stand. It stands in *Spawn a site*, beside the
  part that tells you what to make your own, since that is where a reader meets the starter.
- The first look had no key. Of the first drawing you asked "where is this?", and the key, the headings over the
  neighbours and the pins' samples were added after step 5.

**Where it sits.** Skill 18, testing and shipping: a rule whose first failure names exactly the cases it should.
Skill 17, observability of a design: a component's picture drawn from its parameters, not from memory.

## Parts any site can use, proved alone, then one page built from them (F134.1, 2026-11-15)

**What it is.** `/demos` was a feature card and a row of small flat covers, each with an emoji Windows drew black.
You picked "centre stage": one demo at a time as a window in the light, the others behind it in depth, a dock under
it. And you set the rule for how: "things that can be shared, must go to framework", "like cool components we
could reuse later in other projects". So the start is built from five general parts in the framework's web, each
with its own test, and only then put together.

**The parts know nothing of demos.** A line icon, a lit stage, a deck, a framed window with three bodies, and a
dock. Each was built and tested alone before anything used it: the deck's markup with five items and the first
forward, the dock's buttons and its divider, the window's address word. The deck's one real rule, how far an item
stands from the one forward, the short way round, is a plain class, `DeckPlace`, tested with no browser at all.
That is the pattern of a presentation rule kept out of the component: the part that can be wrong in a way a
person can't see is the part that has a test of its own.

**The page owns what it shows.** Which window is forward comes into the deck as a parameter and goes out as an
event, so the start holds it, reads it from the address, and writes it back when a visitor moves the deck. A
window's address, `/demos/restaurant-demo`, opens the start with that window forward. The frame's own script
couldn't do that here, as step 7 found, so the start reads its address and a small script of its own writes it.

**The site fills a slot by naming a type.** A demo's record names the component that fills its window, as a site
names its admin card. The framework draws the frame and the bar; the site draws a business's website and one
moment of its recorded talk. A test holds every word a window shows to the recording it comes from.

**What your picks changed on the way.** One icon set, the diagrams', not a second one. The dock takes a record a
button, not a function for each fact. On a phone the dock is its icons only, since with a name open it was wider
than the phone. The coding agent and the triage team came off the start, so it shows the three businesses, and the
page says which group it shows. A window's answer is one whole sentence of the recording, not a cut-off answer.

**What went wrong on the way.**

- The plan named a part's address word nowhere, and a window needs one. It is a field on the demo's record now.
- The page a search engine is given had no demos in it, on `main` too. It was fixed here, at your word, as FX155.
- The lean towards the cursor was in the design and in no step. It became its own step, built last.

**Checked by eye.** `/demos` at full width in Chrome, each of the three windows brought forward in turn, and each
at 390 px in a frame of a phone's width. Windows' display scaling makes Chrome's page 1538 wide when the window is
1920, so the wide check is at that width.

**Where it sits.** Skill 18, testing and shipping: a general part proved alone by a markup test before anything is
built from it, and a rule kept out of the component so it is tested with no browser. Skill 20, extensibility: a slot
a site fills by naming a type.

## One record, and a page that is its own editor (F142, 2026-11-13)

**What it is.** Your CV was a Word file. It is now one JSON file in the JSON Resume shape, read strictly into
records, and three things read that one record: the page at `/cv`, its editor in the admin, and (in F143) the guide.
The AI-engineering idea is small and worth keeping: **one source of truth that people and agents both read**. A page
an agent has to scrape drifts from what the agent says. A typed file both read can't.

**What was built, in order.** The records and the strict reader. The page. The CV kept as a file on the site's API,
so an edit shows without a deploy, with the built-in copy as the fallback. Then the editor, twice.

**What changed on the way, and why it was right to change.**

*It was the site's, not the framework's.* The plan put the CV's records in the framework's web because "any site could
have a CV". You stopped it at step 1: one site's feature stays in that site. What moved into the framework was only
what any edited page needs: a text field, a tag input, the inline pieces. *The editor was a form, then the page.* The
first cut was a form for each part beside a live preview, your pick of three. Having seen it you asked "can we not
just edit it inline?". A form beside a page is two copies of every field, which is the problem the single record was
meant to end. The forms and their tests were thrown away before they were committed, and the cost was an afternoon,
because the step was small enough to throw away. *No edit mode.* The page's components don't know they are being
edited. Each gets its part and hands a changed copy back up, and a part has a listener exactly when the whole has one
(`PartChanges`). Where nobody listens, the inline text draws plain words. The proof is cheap: the public page's golden
file didn't change.

**What the tests hold.** The strict read (a misspelt field is an error). What a public page mustn't hold, checked on
the file and again on every save. Every change a person can make on the page, one test each, through the real view.
The public page has nothing to click. An address under `/admin` is no public page, so the editor is never rendered
for a search engine or handed to the guide.

**What didn't work, or cost time.**

A namespace named like a type hides the type: `Resume`, then `Cv`, each bit once. Folders are `Resumes/` and the tests
name the content class by an alias. The site's Sass compiler only read scoped styles from `Pages/` and `Components/`,
so the new `Admin/` folder's styles were silently skipped until the folder was named in `sasscompiler.json`. Nothing
failed. It only looked wrong. A style file added to a component adds a scope mark to its markup, so a golden file
changed with no change to the words. Read the diff before believing it. Two of the shared pieces had their tests
written first but run only after the code. One was then broken on purpose to see its test catch it. Seeing a test fail
first is cheaper than proving it afterwards.

**Not measured.** No paid run was needed, and nothing here has a before and after in money or tokens. The numbers
are test counts: the site's tests went from 484 to 589 and the web's from 760 to 802 across the feature. Still to
do by eye: the page and the editor at phone width, and a real PDF from the print dialog.

## Words at a pace, and an answer that leaves (F154, 2026-11-17)

The guide's answer used to appear in whatever bursts the model streamed it in, arrive with a fixed CSS rise, and
vanish in one frame when it was put away. Louis picked a motion with three parts: the answer grows out of where it
comes from, its words come at a steady pace, and it fades before it goes.

The pace is the part worth studying. Streaming shows the model's words as they come, but the model sends them in
bursts, sometimes a word, sometimes a sentence at once. A steady pace is a buffer between how fast words arrive and how
fast a person reads, and a buffer needs a rule for falling behind, or it grows without end. `WordPace` is that rule,
one pure function from one frame to the next: 90 letters a second; when far behind, a pace of the gap and some more;
once the answer is done, a pace that finishes it soon. The first version used a pace of the gap alone, and its own
tests caught why that fails: a pace that is the gap over the time left slows as the gap closes, and never arrives. Each
faster pace now keeps a floor that does not shrink. Because the rule takes its times as arguments, its tests give it
frames and need no clock and no browser.

Two lessons came from the browser, not the tests. A script beside a component is served only under the component's
own name, so the first motion module was never loaded and the answer showed no words at all; the module tests had only
checked that each file was on disk, and missed every path `FrameworkScripts` builds from a constant. They now hold each
module to a place the library serves. And the caption tidied up its motion before it let go of the page, so when the
motion module had failed, the page stayed dimmed: letting go of the page now comes first, whatever fails after.

The leaving answer taught a Blazor lesson. A caption drawn in a separate spot as it leaves would be a new caption, and
would grow in again instead of fading. So the bar draws all its captions in one list keyed by their answer, and a
leaving caption is the same one, told it is leaving. Which answers are leaving is a small class, `CaptionHandOver`,
tested apart from the drawing, as the pace is.

What waits: Louis noted pacing further down as well, in the engine and in the API (F155, F156), so the words travel at a
steady pace before they reach the page.

## One press to be a first visitor again (F153, 2026-11-17)

Louis wanted to see his site the way a new visitor does, without opening a private window. That turns out to be a
lesson in where a conversation lives. An agent's conversation is in two places: the server keeps the session, and
the browser keeps its own copy, the list of chats with each session's id. Resetting means ending both, and the
order matters. The reset ends every session on the server first, while the browser still holds the ids, and only
then forgets the lists. Done the other way round, a reset that failed half way would lose the ids and leave sessions
on the server that nothing can name.

The work is one small class with one call, a Facade over the module, the chat store, the API and the navigation,
and inside it a Transaction Script, one procedure in order. That shape made the order testable without a browser or
a server: the test gives the class a stand-in for the browser's script and an API that records every request, and
checks the `DELETE`s, then the forgetting, then the reload. One session the server says it no longer has stops none
of the rest, and the test holds that too.

Two things the plan had wrong were caught by the build and by reading the keys. The plan split the reset and its
button into two steps, but Blazor refuses a component's script with no component beside it, and a rule test needs
every function the C# calls to be in a module that exists, so the two landed as one commit. And the plan said the
saved lists end in `.chats`, but the guide's is `widget-chats` and the showroom's `showroom-chats`, so the reset
takes every key that ends in `chats`. Its look was caught by eye: a panel lays its parts in a column, so the reset's
line and press stood centred until its rule said "row".

## Pacing in the engine, and where it sits (F155, 2026-11-17)

Streaming shows a model's words as they come, but a model sends them in bursts. F154 paced them on the page; F155
gives the engine the same, so Rider, the CLI and the API can have even text too. The research for it found one
pattern everywhere: put a queue between the model and the reader, and drain it at a pace of its own. That is a
producer and a consumer at different speeds. The reader keeps up with the model, so the turn is never held back, and
the writer keeps a reader's pace, catching up when it falls behind.

Where it sits mattered as much as how it works. Louis picked a pacer on the turn's stream on its way out, a
Decorator, over one inside the chat pipeline. Inside, every tool round would have waited for its text, and the
traces would have timed the pacing as if it were the model. Outside, the agent's work, its cost records and its
history run at the model's speed, and only what a host reads is spread out. He also settled that three layers of
pacing were too many: each pacer holds the text back to keep its rhythm, so pacing twice only adds lag. So there is
one rule, `WordPace`, shared by the engine and the page, and one switch on each, with only one on per path.

The research changed the rule too. A fixed speed either falls behind a fast model or shows its bursts again, so the
pace became continuous, the larger of a steady pace and what is waiting over a short window. And a server must cut
on whole words, or a word or a markdown mark breaks across two pieces.

The tests ran the pacer on a fake clock, so they could see how much went out by when without waiting. They found the
fault worth remembering. A model's last update carries only its finish, and the first pacer sent all the waiting
text at once before anything that was not text, so at the end of every reply the pacing did nothing. The step's own
tests had missed it, because their turn paused between updates and the text went out before the finish arrived. A
turn that sends everything at once showed it in one run. The lesson: a test of timing has to give the code the
worst arrival, not the kind one.

The step also brought a naming rule, ADR 0126. The first name was `PacedStream.Of(turn, clock)`, and `Of` in this
repo means a record built from another type, which this wasn't. It became `turn.Paced(clock)`, a stream operator as
LINQ names them, and the rule now says which word a method takes by what it does.

## A second app is a row in a table (F164.3, 2026-11-19)

framework.louis-agent.dev goes live beside louis-agent.dev as a second Azure Container App. The lesson is how little
that took, because the deploy script was already shaped for it. `deploy.ps1` keeps a table of apps, and each entry
holds everything one release needs: the image, the yaml, the secrets file, the identity, the share, the hostname, the
suffix on its own vault secrets. The framework's site was one more entry, a yaml that differs from louis-agent.dev's
only where it means to, and a second step in the workflow. Comparing the two yamls with their comments taken out
showed exactly those lines and no others, which is a better check than reading a new file on its own.

Nothing new was made in Azure but the app. It shares louis-agent.dev's identity and file share, each app's logs in a
folder of their own, after Microsoft's own pages said a user-assigned identity "can be used by multiple resources" and
several apps may mount one share. A second identity would have guarded against something neither app does, and
making it would have meant running a Bicep build that FX122 rules out.

Two faults were found before they could happen. The `domain` verb took any name but `www.` for an apex, so it would
have told Louis to point framework.louis-agent.dev's apex record, `@`, at the new app, which is louis-agent.dev's own
name. It now counts the name's parts. And the secrets file the framework's keys go in was not git-ignored: the old
rule named `.env.secrets` exactly, so `.env.secrets.framework` would have been committed. A test now holds the line.

One unknown was designed around rather than guessed at. Whether Container Apps mounts a share folder that doesn't
exist yet could not be confirmed, so a release now makes the folders an app names, with `az storage directory
create`, which leaves a folder there already alone. louis-agent.dev names none, so its release never reads the storage
key and stays as it was.

What didn't go well: the Azure docs had described the setup before FX122 for ten days, a VM and a database as
running, and a workflow that "tried each release even when the other failed", which GitHub doesn't do. They were
rewritten whole here, at Louis's pick, and their pictures redrawn. A doc that a deploy depends on goes stale exactly
when a fix is rushed, and FX122 was rushed: the next deploy fix should touch its docs in the same change.

Nothing was measured in money in this child's steps: the live release and its first paid answer come after the merge.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a second deploy target is data in a table, checked against
the first by a diff, with an idempotent step for the one thing that couldn't be confirmed.

## A framework's tests use no product (F166.1, 2026-11-20)

Before Puls can leave for a repository of its own, the framework has to stop leaning on it. Here, it leaned in two
places. The API's tests, about 950 of them, ran on Puls's identity, its packs and its eval files. And the script that
releases the framework's two sites lived in Puls's folder, with Puls's old infrastructure verbs in it. This child
moved both out, in eight steps, without a cent spent and with nothing deployed.

The API's tests now talk to an agent that doesn't exist. `TestAgent` is a made-up identity with the shape a real one
has: language voices over a routing voice, a specialist, a reader, minions on a shared base. Its prompts live in
memory, and its packs are fakes under the real packs' names. That's the test double pattern, Meszaros's name for a
stand-in with the real thing's shape. The tests that only checked what the shop-prices pack does with a Stats SA
sheet went back to that pack's own tests, which already held every one of those facts, and the API's upload and feed
routes now prove themselves on the framework's `FakeImport` and `FakeFeed`. A test proves one thing at its own layer.

The eval tests got data of their own. The integration tests start `LouisAgent.Api.TestRunner`, the framework's
runner with the general checks, which copies the framework's template eval beside it. The scenario and draft tests
needed more than the template has, a follow-up, a held-out split, an expected budget, so `SampleEvalWorkspace` writes
a small eval into a temporary folder for each test class. That's a Fresh Fixture: the values a test checks sit beside
it, not in a product's folder that can change or leave.

The rule tests did more of the work than the plan expected. `TestsNameNoProductTests`, written first, held each step.
When the tests moved under `src/`, `SiteBoundaryTests` refused them: under `src/` nothing but the site may reference a
pack (ADR 0076), so the test agent's real web and calculator packs became fakes too. The folder-size rule caught a
seventh file, the solution-item rule caught a doc listed at its old place, and the diagram tool's own test caught
that it never draws pictures under `src/site`. That last one is why the deploy docs became a guide and an
explanation in `src/docs`, where the docs rules put each kind, and not a README beside the script.

One real bug came out. The new upload test showed the API dropping every uploaded file's name: the admin page sends
`file_name` and the API reads `fileName`. Shop-prices reads files by slot, so nothing visible had broken. It's
queued as a fix of its own, on its own branch.

What didn't go well. Twice, `git add` stopped on a file `git mv` had already renamed, and the commit went in with only
the rename; both were caught at once and completed before anything was pushed. A path in the run-the-evals guide was
missed in the docs step and found only at the next merge. And one guard, the browser's toolset names against the web
pack's, would have been lost in the move to fakes; it now lives in the web pack's own tests, seen failing on a wrong
name before it was trusted.

Nothing was measured in money: no model call, no eval run and no deploy. The tests are the measure: 367 API tests,
553 studio tests and the integration tests passed on the test agent, and 77 deploy tests moved to the site's project
with the counts on both sides matching.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a framework's tests use fakes and data of their own, so a
product can leave without taking them, and rule tests hold the line while the code moves.

## Every project named for its namespace (F167, 2026-11-21)

Before the framework can be published as packages (F166.2), its names had to settle. A project was called
`louis-agent.core` on disk while its code said `LouisAgent.Core`, and a package would have taken the first. Louis chose
one name for all of it, so the folder, the project file, the DLL, the namespace and later the package all read
`LouisAgent.Core`. All 155 projects moved, with their docs and their pictures.

The new names were read, not invented. Each project already declared its namespace, so 146 names came straight from
it. Nine had to be listed by hand: seven that shared a namespace with a neighbour, and the coding catalog with its
tests, whose name says more than their namespace. Because the names were read, the code moved and no namespace in
it changed.

The rename was planned as a tool of our own with tests, and two steps of it were built: a name map that refuses a
clash before anything is touched, and a rewriter that changes a name only where the whole name stands, the longest
first, so `louis-agent.core.web` is never half-changed by `louis-agent.core`. Then Louis dropped it, "just as a once off
just too many to test", and the same rules ran as plain commands, kept in `src/tools/rename-projects` with their
reports so anyone can see what ran. Writing the rewriter first still paid off: measuring which endings follow a
project's name found `.styles.css`, the stylesheet .NET names after the assembly, which a site would have lost.

The run itself was one command and 6,245 names in 1,286 files. What took the time was everything that found a project
by its old spelling and not by its name: a test that knew a test project by `.tests`, a rule that read identities
from `louis-agent.identity.*`, a regular expression with the name escaped, diagram labels whose name followed a
`\n`, Puls's routing questions with their dataset hash. The suite found almost all of it, 35 failures across 11
projects, and each fix was small. Two were in the product: the log's service name, kept exactly what it was by turning
`SiteApi` back into `site-api`, and the eval studio's publish folder, which had pointed at a folder that no longer
existed while its own tests, built on the old names, stayed green. That one is the lesson: a test that makes its own
fixture can agree with a bug.

What didn't go well. Rider stayed open through the run and kept writing build folders under the new names while they
were being cleared, so the clean-up had to wait for the build servers to stop. The tool was half built before it was
dropped, and its two steps were taken back out of the branch, kept only on a local branch. Nothing was measured in
money: no model call and no eval run. The measure is the suite, green but for the five tests that fail on Windows only,
and the site answering on :5080 with its renamed stylesheet.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a change too wide for hands, made by rules that are
written down and kept, then proved by the build and the whole suite; and a name read from the code, not invented.

## The framework as packages (F166.2, 2026-11-21)

Puls is moving to a repository of its own, and from there it can't point at the framework's project files. So the
framework became something a product installs: 31 NuGet packages on GitHub Packages, the projects a product reaches,
each with a version of its own read by MinVer from a tag of its own name, `LouisAgent.Core-0.1.0`.

Almost nothing of it is code. A package is a project's settings: one shared file makes every project no package unless
it says so and gives every package the licence and Source Link, and a second, read after the project, adds MinVer only
where the project said yes. A rule test holds which 31 say it. The proof was written before anything was published:
an integration test packs the 31 into a folder and runs an agent from a project outside the repository that knows
only that folder.

Two questions the docs didn't settle were answered by trying. A project reference becomes "this version or higher" of
what it referenced. And a scratch repository of two projects showed what MinVer does across prefixes: with
`MinVerIgnoreHeight`, a package with no tag on the release commit keeps its last tag's version, so a package released
later depends on a published neighbour, not a pre-release nobody can restore. That made one tag per package safe, with
a guard before the push for the one thing left, a dependency whose tag was never pushed.

The release taught two things on the way. A tag runs the workflow its commit holds, so the release waited for the
merge and was tagged on `main`. And GitHub makes no event at all when more than three tags are pushed at once: 31 tags
arrived and nothing ran. The workflow already published every package tagged on its commit as one batch, since GitHub
starts tag runs in no set order, so deleting one tag and pushing it again made one run that published all 31.

What didn't go well. The local unit suite ran green while an older integration test, which packed a pack at .NET's
default 1.0.0, broke on CI after the merge (FX218); a change to how everything builds needs the integration tests
before it merges. A timing race in another integration test surfaced in the same runs and was fixed on its own (FX219).
Nothing was spent: no model call. The measure is the release run, 31 packages pushed, and a workflow that restores
them with only its own token and gets an agent's answer back.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a framework shipped as packages, proved from outside the
repository before and after it is published, with a guard where a mistake would be permanent.

## A look measured, then held by a test (F169.1, 2026-11-21)

The design review of the framework's site found flat's lines and quiet words too weak: in the dark a card's edge
all but vanished on the near black page, the faintest words were hard to read on a card, a word of code in a
sentence came out in Bootstrap's pink, and the keyboard's ring was drawn twice, the second time at 70 percent. Each
fix is a value or a line of Sass. What makes it a lesson is how each one was proved, because "it looks better" is
not a test.

The idea is to turn a look into a number. WCAG, the web's accessibility rules, says how readable a colour is on
another as a contrast ratio, from 1 to 21, worked out from each colour's relative luminance by the W3C's own sum
(technique G18). Words need 4.5 to 1, and a ring round a control 3 to 1. So the first step was a small type in the
web tests' shared library, `Contrast`, that takes two colours as a theme writes them and gives the ratio. It has
tests of its own against known pairs, black on white is 21, and it learnt to read `rgb()` with percentages when
glass's light page turned out to be written that way.

With that, a theme's sheet became something a test can measure. The themes' tests already read the compiled sheet
for each theme and style, so `FlatInksTests` asks for the three inks and the grounds they sit on and checks every
pair. It failed first, as a test should: flat's faint grey read 3.85 to 1 on a light card and 4.46 on a dark one.
The new greys read 4.69 and 5.53. A listing's comments, which used the same grey, failed the same way and were
moved with it. The purple test went over every theme, not only flat, and found something the review hadn't: glass's
light page is itself a violet wash, and the brand's middle violet on it read 2.9 to 1. That was a choice the plan
didn't settle, so it went to Louis, and glass light now writes its purple in the brand's deep violet, 5.43 on the
page and 9.52 on white.

A line is not text, so it has no 4.5 to hold it to, and here a test pins the design instead: in the dark every one
of flat's lines is the one firmer grey, `#404048`, which stands 1.90 to 1 off the page where the old one stood 1.39.
The rules that draw things once are held by reading the Sass: the ring is written in one sheet only, a chip reads
the chip's line, and a code word, on a page or in the agent's answer, goes through one mixin that reads three names
every theme now sets. Those are plain string tests on the source, cheap and exact, and they fail the day someone
draws the ring again in a later sheet.

Numbers are not the whole story, so the built pages were measured too. A Playwright script opened nine pages of the
two sites in every theme and style, walked every visible piece of text, worked out its real colour and the colour
behind it, and listed everything under 4.5. One item was left: the brand's dot in the pictures, a logo, which WCAG
exempts and a later child of the same feature replaces. The ring was measured on the page as well, at 4.75 on flat
light and 3.84 on flat dark.

What didn't go well. The diagram engine keeps its own copy of flat's palette, so changing the line in the theme
broke a golden file in a different test project, the drawn card's, and it was found only when that project's tests
ran; a change to a shared colour needs a search for every copy of the value first. A folder grew past six files
when a new test went in, and a few lines went over 120, both caught by the rule tests a step later rather than
before the commit. Nothing was spent: no model call.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a look turned into numbers a test can hold, the rule
measured in the sheet and again on the built page, and what a number can't hold, a line or a ring drawn once, held
by reading the source.

## Puls in its own repository (F166.3, 2026-11-21)

Puls left. It now lives at [github.com/bosbevk/puls](https://github.com/bosbevk/puls), with its three packs, under
names of its own (`Puls.Identity`, `Puls.Packs.Recipes`) in the usual `src/` and `tests/`, and it reaches the framework
only through the packages F166.2 published. Its history stays here, at the tag `puls-moved-out`, and the move was made
by commands run once and kept in that repository as the record, the way F167's rename was: a table of 26 names, a
script that reads Puls's files from a commit of louis-agent and writes them in the new layout, and a second that
mended the doc links. A rerun from a later commit gave the same tree again, which is what let the move be redone each
time the framework changed under it.

The lesson is in the first run. Packed from `main` and built outside, Puls compiled, and 199 of its tests failed. Not
one was a fault in Puls. Each was a place where the framework had quietly assumed it was in its own repository: the web
packages carried compiled CSS but not the Sass a site forwards, four test helpers looked under `demos/` and read
recordings and partials from louis-agent's source, the runner's package left out its stub eval's transcripts, and one
page reader a pack needed was internal, which was why the pack kept a copy. Every one had been invisible here, because
here every path exists. A product outside the repository is the only test that can find them, and the packages'
integration tests now hold each one, so the next product doesn't find them again.

Each fix was its own small step in this repository, tested first, released by a tag, and picked by Louis from three
options. The same pick came four times, on the same principle: a package carries what it needs to work outside, rather
than the product copying it. Copying was the cheaper option every time, and every time it would have left two versions
to drift. The packaging itself taught one thing the docs were quiet on: NuGet adds an item's own folders to its
`PackagePath`, so naming them as well doubles them. A test that opened the package caught it on the first try.

What didn't go well. Twice an option was put to Louis before its premise was checked: dropping the pack's copies of the
readers assumed all three were public, and one wasn't, so the choice had to be put again. And a full suite caught what
the touched tests hadn't, a repeated name written as a string, more than once. Nothing was spent: no model call. The
measure is the first CI run in `puls`: the framework restored from the feed with that repository's own token, 1,665
unit tests and 22 integration tests on a throwaway MongoDB, all green, and the coming-soon page published from there.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a framework's first outside user as its truest test, with
every assumption it finds fixed in the framework and held by a test there.

## Puls gone from louis-agent, and F166 closed (F166.4, 2026-11-21)

The last child took Puls out. `demos/puls` and the recipes, shop-prices and where-to-buy packs left, with their 26
projects and the 17 solution folders around them, and the three packs' ADRs went home to `puls` beside the one Puls
already had. That closes F166: louis-agent is the framework and its own demos now, and Puls is the first product
built on it from outside, in a repository of its own, on the packages.

Deleting was the small part. Most of the work was what a deletion leaves behind, and the order mattered. The ADRs
landed in `puls` first, so nothing here ever pointed at a file that wasn't there yet. The framework's pages changed
next, while the folders still existed, so their tests could be seen failing for the right reason. Puls stays on those
pages as the site's best evidence, a product on the packages in its own repository. A small record, `ExampleHome`,
says whether an example's identity is a folder here or a path in a repository of ours, and ADR 0100 gained one narrow
exception: a claim about a product built elsewhere is held by its link, since no test here can read another repository.

The lesson is what left with Puls without anyone deciding it. Puls's composition root had been the one project that
referenced every pack, so the framework's own rules about packs lived in its tests: every pack convertible to an MCP
server, and every prompt with its header. They moved to `puls` with it, and then guarded only Puls's three packs. The
docs sweep found it, in a doc that described those tests. They came back as `LouisAgent.Packs.Rules.Tests`, the one
project that sees every pack here, with a test that its list is every pack so the next one can't slip past. Holding
all twelve at once caught at its first run what no single catalogue ever had: DevOps's and Paymo's waiting tools take
no cancellation token, and the site pack's example addresses read as naming the framework's identity. The first went
on a list that only shrinks, under FX83, which already named that breach. The second was a false alarm, fixed in the
shared check: a name between slashes is a page's address, not the agent.

Then the docs. About a hundred lines in 60-odd docs described Puls as if it were here. Links were mended by one small
rule: to the file in `puls` when it moved there, otherwise to louis-agent at the tag `puls-moved-out`, where history is
true. Words were mended by hand. The records of what happened kept their history, and the docs that describe today were
made true. Two guides needed more than a word swap. *Build a pack* and *Build an identity* had taught registering
through Puls's composition root and its `PACKS` and `IDENTITIES` settings, which nothing reads any more. They teach a
program's catalogue now, with the calculator, the docs pack and the triage team as their examples. Where only Puls had
the example, a feed, a judge or a pack's checks, they point at its own repository.

What didn't go well. A few small edits went through a shell substitution where the rule is to edit, never script, and
were checked after. A commit went ahead before it was shown, at Louis's "just get it done", and the next was stopped
for his explicit word. Nothing was spent: no model call. The measure is a repository with no file under `demos/puls`,
every pack held to the rules, and the full suite green.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a rule held by the one project that sees everything goes
wherever that project goes, so splitting a repository means asking which guards leave with each part.

## The sites' Azure, made from code (F170, 2026-11-22)

The two sites ran in an environment built once from Puls's Bicep file, and that file left with Puls. So the releases
worked every merge, but nothing in the repository could make their environment again, and the trust page said so.
F170 wrote the framework's own file, `src/site/deploy/azure/main.bicep`, for everything the apps stand on: the logs
and Application Insights, the network, the environment, the registry, the vault and its identity, the storage and
its share, and the rights between them. Every name is a parameter, and our own names sit in `main.bicepparam`, so the
same file builds another business's sites.

The interesting part was describing something that already runs. A Bicep file is a desired state, and deploying it
over what stands changes whatever differs, so the bar for each step was Azure's `what-if` showing nothing to create
and nothing to delete. Microsoft's verified modules were the base, and their defaults are safe for a new resource and
dangerous for ours. Read from the modules' own code at their pinned versions, a deploy with the defaults would have
turned on the vault's purge protection, which can never be undone; denied every address at the registry and the
storage, cutting the environment off from its images and its share; turned off the workspace's key sign-in, which
the environment's logs use; stopped masking visitors' IP addresses in Insights; and taken both sites off the internet
through the environment's public access. Each was set back to what stands, and the ones that would hurt most are
held by a test that reads the file.

Two modules couldn't say what stands at all. The environment's has no setting for its telemetry agent, so a deploy
through it would have stopped the logs and traces reaching Insights, and the network's names a subnet's delegation
after its service, which would rename the live one under a running environment. Louis picked plain Bicep for those
two. Bicep's own types even lagged the API there: reading the environment back at three stable versions proved the
settings exist, so those two lines silence the type warning with the reason beside them. Reading every rights list
also turned up four leftovers of Puls, a subnet and rights given to identities that no longer exist, and each went
as a fix of its own first (FX221, FX223).

Then the deploy, by a person, since the file gives rights the release identity can't. Azure recorded the four outputs
under `louis-agent-infra`, each equal to the old build's, and both apps were released from them. That release met
FX224: Windows PowerShell writes a byte-order mark where PowerShell 7 on the runner doesn't, and the yaml reader
took the mark's line for a key. The trust page's *Rebuilt from code* is in place now, held to the file.

What it cost: nothing but a few minutes of the two apps' revisions. The throwaway rebuild into an empty group, which
would prove *from nothing* rather than *over what stands*, wasn't run.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): infrastructure as code is a desired state, so the first
file written for running infrastructure is proved by the difference it would make, and a module's safe defaults are
read before they are trusted.

## A page of words, and a layout run before it was trusted (F177, 2026-11-24)

Louis takes freelance work integrating AI into a client's website or business, and he wanted something to show a
client: how a job goes, from the first call to the handover, and how it is paid. That became two things. One is a
deck to hand over. The other is the same story as a page of the site, *Work with us*, in the menu before Contact.

The page itself was the easy part, because the framework already had every piece. Nine bands, a few rows of tiles, the
numbered steps and two buttons at the end, all configured framework controls, with no HTML of the site's own. Its
words are six small lists keyed by an enum of their parts, each with a test that every part has its words. Two tiles
that both said "50%" got the same id, which the site's test that every place on a page has its own address caught at
once. Giving each half a title fixed it.

The lesson was in the layout. Louis picked the framework's *slides* layout, where every section is one screen and a
long one pages sideways. It had been built for an earlier feature, and no site used it. Reading its code, it looked
ready, and that is what he was told. Running it showed otherwise. At 1920 by 1080, a long section showed only its
small topic line on page one and its heading on the next (FX246). On a 1366 by 768 laptop every slide paged, even the
last one, because the guide's bar keeps 234 pixels free at the foot of the screen and a slide had about 200 left
(FX247). Louis chose to fix the layout first, then picked the guide as a corner button on a page of slides. After
both fixes, at 1920 by 1080 eight of the nine slides fit on one screen, and on a laptop every slide's heading is on
its first page.

What didn't work: the claim from reading. The code said "a section is a screen", and that was true, but what a
visitor sees depends on what else stands on the screen. A layout no site uses has never met the rest of a site.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a layout is proved by running it at the screen sizes
people really have, beside everything else that shares the screen, before anyone is told it works.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plans](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL15-engine-identities-packs.md), [CL19](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL19-config-by-place.md) · [Deploying to Azure](../guides/deploy-the-sites.md) ·
[Write and run tests](../guides/write-tests.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) · [ADR 0066](../../../demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md) · [The site, now the app's](../../../demos/louis-agent/docs/projects/LouisAgent.CodeReview.md)
