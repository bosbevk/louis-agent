# 18 · Testing and shipping: how the cut was proved

> **Skill:** #18 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for the goldens · **Stage:** the platform
> plan's Phase 2, CL15 *Engine, identities and packs* (2026-10-19), CL19 *Config and Docker by place*
> (2026-10-24), and F54 *The engine's website* (PR #133, 2026-11-03) · **Written by** Claude, for Louis to read (2026-10-19, 2026-10-24, 2026-11-04) · **Next:** simulations in C# as the release gate (F18, Phase 3); the web app's first tests (F40,
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

## Git stays the truth (F49.5)

Skill #18 again, with #12. When F49 finished (2026-11-03), the admin portal could make a whole eval, and the
question was where it should end up. Several hosted eval tools kept the evals in their own store, and when the tool
died the evals died with it. So the studio's evals live where the code lives: as files in git, changed by a pull
request ([ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)).

- **Publish is a commit, not a push.** The eval's folder is committed beside its identity on a branch of its own,
  `eval/{identity}-{name}`. You push it and open the pull request. Then a person reviews it like any other change,
  and CI runs it. The API has no GitHub token, so it has no secret to leak and can't merge anything by itself.
- **It never touches your checkout.** The commit is made with git's plumbing (the low-level commands under `git add`
  and `git commit`) over a temporary index: the files are written into git's object store, a tree is built from
  them, and the branch is pointed at a new commit off HEAD. Your current branch, your working tree and anything you
  have staged stay exactly as they were. The git endpoints would have switched branches under you.
- **The gate before the commit is the gate CI runs.** Only a folder the runner's `--check` passed is published, and
  that check is the conformance test every eval folder meets in CI. A pull request from the studio shouldn't go red
  for a reason the studio could have told you about.
- **Where there's no checkout, a zip.** The hosted Puls has no git workspace, so a publish there hands you the folder
  as a zip to add in a pull request. It's more work, but it still ends in git.

## A host that is only content (F54)

The engine's website was the first host with no engine in it (until F109.1 folded it into the coding agent's web app): `louis-agent.site` referenced nothing of ours and
runs no model. It is ASP.NET Core with **static server rendering**: a Razor component is rendered once on the server
into plain HTML and sent, no WebAssembly, no interactivity, no script. A page is its markup and one stylesheet, and
the two films of how the engine works are CSS keyframes over CSS 3D transforms. What that gave the tests:

- **A page is a string.** `WebApplicationFactory<Program>` starts the site in memory and the test asks each route and
  reads the HTML: the title, the film's class, no `blazor.web.js`. No browser in the test, no port, a second for all
  five pages. `/health` answers the container's check the same way.
- **The behaviour is in the stylesheet, so the stylesheet is tested.** The films' logic (where a part stands, when
  the camera flies in, when a caption shows) is CSS. `SiteStylesheetTests` reads `app.css`'s import list against the
  files on disk, holds tokens first, and refuses a keyframe name the two films could share (every name ends in `-s`
  or `-d`). A rule a file could break silently is a test, whatever language the file is in.
- **A golden per film**, as for the prompts: bUnit renders the markup and `Snapshots.Match` pins it, so a change to a
  film is read in a diff. The detailed film's six slabs inside core are checked against the chat pipeline's order as
  a plain array in the test, which is the one honest way to couple a project that must not reference core.
- **Headless eyes for a CSS animation.** A film can't be looked at by waiting for it. Playwright pauses every
  animation and sets its time (`document.getAnimations()`, `currentTime`), so a scene's midpoint is one screenshot
  and the whole film is 1,800 frames ffmpeg joins into the MP4; the stylesheet stays the one source.
- **Numbers in parallel branches collide.** Two branches opened the same day both took ADR 0065; the clash was caught
  by a colleague's note before the push, not by a test. Next free is read from `main` *and* the open PRs, and a test
  that refuses two ADR files with one number on merge is cheap.
- **Measured: $0.** The site calls no model; the only spend was the time to render frames.

## Everything in Azure, as one file (F61.4)

The CL19 deploy reached the PC for its browser and its store through a Tailscale sidecar, because a Container App can't
hold Mongo (its disk is a file share, and Mongo's engine fails on it) and the browser's DevTools port must never be
public. F61.4 turned that round: the services moved to where the app is, on a small VM in a private network that the
apps' subnet alone can reach, and the sidecar, the ingress lock and the open key went with it, since the web app now
signs in and the gate takes a signed-in person. Three things to keep from building it:

- **A build is a file, not a sequence of commands.** Louis asked for every `az` write to be recorded in config as a
  build step, then for scripts that check for a resource and create or update it; a Bicep deployment is exactly
  that for every resource it declares, so the template became the record, and the script's verbs are only what a
  template can't do: copying files to the VM, building images, the app's revisions, the domain's certificate once
  the DNS records exist. A verb reads what it needs (the VM's address) back from the deployment's outputs, never
  from a value someone copied.
- **A secret never travels in a definition.** The SQL password and the SSH key reach the template as parameters read
  from environment variables the verb sets from the git-ignored files; the VM's env files go up through run-command
  after the first boot, because cloud-init is readable on the VM for good.
- **The old stays until the new answers.** Nothing of the CL19 deploy or the F58 tunnels is deleted before both
  domains serve from Azure; the READMEs describe both until then, and the retirement is one listed step.

## The site moves into its app (F109.1)

The static site of F54 had one job, content, and one rule, no script. Six steps later it is the landing of the
coding agent's web app, and nothing about the content changed. What made the move cheap is worth keeping:

- **A frame with slots, in the framework.** The site's header, menu, footer and a corner for a chat widget are one
  component, `SiteShell`, with the same shape as the admin's frame: the demo's layout fills six slots in twenty lines.
  Louis's rule was the reason ("that custom layout can be reused, it should be in the framework"); ADR 0084 was already
  the rule, and a test keeps a site's layout under forty lines.
- **Goldens prove a move.** The films' markup was pinned before the move; copying the components across projects and
  seeing the same goldens pass is the whole proof that no pixel of markup changed. The films' export hash does the
  same for the rendered WebP: it fails on the moved files until the films are re-exported from the app.
- **A look stays where it belongs with one selector.** The site's element rules hang off `.site`, the frame's root,
  so the chat and the admin keep the framework's. The alternative, ordering a list so the right rule wins, works until
  the next partial is added in the wrong place.
- **Tests pin behaviour, not transport.** The site's tests fetched HTML from a server; the app is WebAssembly, so the
  same assertions became bUnit renders. What went: the two tests about the transport itself (plain HTML, `/health`).

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

- **A table beats a copy.** One script with `-App` and a table of what is each app's (image, config, yaml, identity,
  share, domain, the suffix on its own secrets) is twenty lines; a second script would have been three hundred that
  drift. The shared parts (the VM, the environment, the database with a tenant per app) are shared on purpose, to
  keep the month's bill at one VM, and ADR 0098 says how they split when they must.
- **Infrastructure as a file is a diff you can read.** The Bicep what-if named four creates and the same six
  reference-only modifies as the first deploy; that is the whole review of a change to production, before anything
  is written.
- **Keep a verb honest about what it can't finish.** The domain verb bound Puls's apex in one run; here the
  certificate's token was where Azure no longer returns it, and a bind on a pending certificate fails. The verb now
  reads the token from the one API version that still carries it, stops with a message while the certificate is
  pending, and does nothing twice on a second run. A deploy script's job is to be re-runnable.
- **DNS has a memory and so does the browser.** Cloudflare imports the old host's records when a zone is added, so
  the first thing to do on a domain is delete what you did not add; and Chrome remembered the old proxy's HTTP/3,
  which a reload cleared.

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

## Docs a test holds to the code

The site went stale without anything failing: CL29 changed the structure, and the films kept saying the old one. Prose can't be
compiled, so the fix was to make the parts that mirror code checkable (F57):

- **Say it once, and test the copy.** The program on *Build your own* is shown as text, so a test reads the real `Program.cs` and
  demands every line of it on the page. Change the program and the page fails.
- **Test the list against the source.** Every folder under `packs/` must be on the *Packs* page. A new pack can't be forgotten.
- **A generated file records what made it.** A rendered film takes minutes and a test can't read its pixels, so the export stores a
  hash of the film's source files, and comparing two hashes is instant. It's the same idea as a lock file.

Looking still found what tests couldn't. Frame by frame, one scene's card hid the very part the scene was about, and it had been
that way since the film was built. A test can hold words to the code; it takes eyes to see a picture is wrong.

What it measures: size decided the format. The same film was 22 MB as a GIF and under 3 MB as WebP at the same frame rate, which
settled a choice that opinion wouldn't have. Concept: *18 Testing and shipping* in the [goal](../GOAL.md).

## A site held to the solution (F109.4)

F57 held three things on the site to the code; F109.4 made the whole site that way, because a site that explains the
whole architecture drifts the moment the architecture moves. The step that mattered was the first: a test that reads
the solution file and asks that every project that ships is named on some page. Writing the Architecture page was then
filling a list the test already held, and the test says the day a project is added without a sentence about it.

- **Hold a claim to the thing it claims about, not to a copy.** The rand on the Measured page are computed by the
  test from the rates file the app itself falls back on; the packages the Built on it page names are checked against
  the project files; the planned features against the TODO's open rows. Each test reads the source the page was
  written from, so a wrong number can't be pinned in a golden.
- **What's planned is a page, not a line, when a test keeps it honest.** ADR 0066 kept the plan to one line because
  nothing could stop a planned feature reading as shipped. With every item held to an open row and labelled, the
  plan can be shown in full: what ships fails the test and leaves the page.
- **The guide is a page too.** The expert's voice names every page and the docs it came from, held by a test over
  the routes, so the site and the guide can't tell different stories.
- **A film's layout needs eyes once more.** The export's poster frame showed a slab under the caption strip in the
  wide shot; the tests hold the words, not the picture. Concept: *18 Testing and shipping* in the [goal](../GOAL.md).

## A site a crawler can read (F121, 2026-11-09)

A Blazor WebAssembly site is a program the browser runs. Before F121 every address on every site answered the same
file, the landing's title and an empty `<div id="app">`, and the page only appeared once the app had downloaded and
started. A person never noticed. A crawler that runs no script, a link preview on WhatsApp or LinkedIn, an AI crawler,
saw every page as the landing with no words. The sitemap of F119 pointed them at 71 places that all said nothing.

**What changed.** The API now renders each page itself, once, and serves it as HTML: the page's own title,
description, link card, canonical link and words inside `#app`. The app still starts and replaces that body, so a
person sees no difference. Measured with `curl` on the review copy: `/how-it-works` went from the landing's title and
an empty body to its own title, a canonical link to itself and all four sections' words.

- **Prerendering, in one call.** *Prerendering* means producing a page's HTML on the server before any script runs.
  ASP.NET Core ships the tool, `HtmlRenderer`, which renders a component outside a browser. Beside the page it renders
  Blazor's `HeadOutlet`, so whatever the page puts in the head (its `PageTitle`, its `PageDescription`) comes out too.
  A host's program says `MapLouisAgentSite(...)` where it said `MapFallbackToFile("index.html")`, and that is all a
  site does.
- **Render once, keep it.** A page is rendered the first time it is asked for and kept until the process restarts.
  A deploy restarts it, so the cache can't go stale: there is nothing to invalidate. Louis chose this over a publish
  step that writes files, which would need its own step in the build, and over a test that writes them into git,
  which would put generated HTML in every diff.
- **Opt in by saying what the page is about.** Only a page with a `PageDescription` is prerendered. The chat, the
  admin screens and the account pages have nothing to rank, and some can't render without their live services. So
  "is this page for search?" is answered by the page itself, in one line it needs anyway, not by a list in the host.
- **Fail to the old behaviour, loudly.** A page that throws while rendering, because it injects something its site
  didn't register, answers the plain shell, as before F121, and one warning goes to the log. A crawler gets less, and
  nobody gets an error page. The chat page does exactly this on the review copy, and that is expected.
- **Test against the real file.** `PageShellTests` fill the code-review site's actual `index.html`, not a sample, so
  a change to that file that breaks the filling fails a test rather than a search ranking months later.

**What didn't work at first.** `MapFallback` was given a method that took an `HttpContext` and returned a task. ASP.NET
binds that shape as a plain request delegate, so the method's result was thrown away and every page answered an empty
200. Taking the `HttpRequest` instead makes it a real endpoint whose result is written. A test through the test server
caught it, where a unit test of the method would have passed. And the standard HTML encoder escaped the site's "·"
into `&#183;`, valid but noisy in a title, so the shell escapes only the four characters HTML needs.

**Left as it is.** Once the app starts, the head holds the description twice, the shell's and the one Blazor's head
outlet adds. Both say the same thing, and a crawler that runs no script sees one. Concept: *18 Testing and shipping*
in the [goal](../GOAL.md), with [ADR 0109](../adr/0109-a-page-is-served-as-html.md).

## Facts a search engine reads (F123, 2026-11-09)

F121 let a crawler read every page. F123 tells it what it is reading. Louis's goal was page one for "c# agent
framework", and nobody can promise that: Google ranks mostly by links from other sites. So the work was the part code
can do, and a how-to for the part only a person can.

- **Structured data.** A search engine reads facts about a page from *JSON-LD*: a script in the head, in the words of
  schema.org, that says "this is an organisation, its logo is here, its LinkedIn is there" or "this page sits under
  this site". It is the same idea as an agent's tool returning a typed result instead of loose text: a machine reads
  facts best when they come as facts. The framework's records write them, the landing says who runs the site and what
  the software is, and the API adds a breadcrumb to every other page by itself.
- **The bug a curl found.** Three facts on the landing, each from its own head component, and the landing was served
  as the empty shell again. Blazor keeps only the *last* `HeadContent` a page renders, so the facts pushed the
  description out, and with no description the API serves the shell (F121's opt-in rule). The unit tests passed,
  because each rendered one component alone. Only `curl` of the real page, then a test with two heads, showed it. Fixed
  by Louis's pick: one `PageHead` writes the whole head, renamed by Rider on every page of the three sites.
- **A fair comparison is a rule, not a tone.** The page for the search compares louis-agent with Microsoft Agent
  Framework and Semantic Kernel. Every claim about theirs is taken from their own pages, read on a date the page shows,
  and linked. What their pages don't say (Microsoft.Extensions.AI, cost tracking) isn't claimed either way. Every
  claim about ours is held to the repo by a test. It leads with Louis's selling point: it runs on your own hardware,
  with no cloud agent service in between.
- **The part no code does.** Search Console, the sitemap submitted, the important pages asked for first, and links
  from real places. The [how-to](../guides/get-a-site-found.md) lists them in order.

Concept: *18 Testing and shipping* in the [goal](../GOAL.md): a test of the parts is not a test of the page, and a
`curl` of what ships is part of the check. With [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md).

## One record, and a page that is its own editor (F142, 2026-11-13)

**What it is.** Your CV was a Word file. It is now one JSON file in the JSON Resume shape, read strictly into
records, and three things read that one record: the page at `/cv`, its editor in the admin, and (in F143) the guide.
The AI-engineering idea is small and worth keeping: **one source of truth that people and agents both read**. A page
an agent has to scrape drifts from what the agent says. A typed file both read can't.

**What was built, in order.** The records and the strict reader. The page. The CV kept as a file on the site's API,
so an edit shows without a deploy, with the built-in copy as the fallback. Then the editor, twice.

**What changed on the way, and why it was right to change.**

- *It was the site's, not the framework's.* The plan put the CV's records in the framework's web because "any site
  could have a CV". You stopped it at step 1: one site's feature stays in that site. What moved into the framework
  was only what any edited page needs: a text field, a tag input, the inline pieces.
- *The editor was a form, then the page.* The first cut was a form for each part beside a live preview, your pick
  of three. Having seen it you asked "can we not just edit it inline?". A form beside a page is two copies of every
  field, which is the problem the single record was meant to end. The forms and their tests were thrown away before
  they were committed, and the cost was an afternoon, because the step was small enough to throw away.
- *No edit mode.* The page's components don't know they are being edited. Each gets its part and hands a changed
  copy back up, and a part has a listener exactly when the whole has one (`PartChanges`). Where nobody listens, the
  inline text draws plain words. The proof is cheap: the public page's golden file didn't change.

**What the tests hold.** The strict read (a misspelt field is an error). What a public page mustn't hold, checked on
the file and again on every save. Every change a person can make on the page, one test each, through the real view.
The public page has nothing to click. An address under `/admin` is no public page, so the editor is never rendered
for a search engine or handed to the guide.

**What didn't work, or cost time.**

- A namespace named like a type hides the type: `Resume`, then `Cv`, each bit once. Folders are `Resumes/` and the
  tests name the content class by an alias.
- The site's Sass compiler only read scoped styles from `Pages/` and `Components/`, so the new `Admin/` folder's
  styles were silently skipped until the folder was named in `sasscompiler.json`. Nothing failed. It only looked wrong.
- A style file added to a component adds a scope mark to its markup, so a golden file changed with no change to the
  words. Read the diff before believing it.
- Two of the shared pieces had their tests written first but run only after the code. One was then broken on
  purpose to see its test catch it. Seeing a test fail first is cheaper than proving it afterwards.

**Not measured.** No paid run was needed, and nothing here has a before and after in money or tokens. The numbers
are test counts: the site's tests went from 484 to 589 and the web's from 760 to 802 across the feature. Still to
do by eye: the page and the editor at phone width, and a real PDF from the print dialog.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md) · [Plans](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL15-engine-identities-packs.md), [CL19](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL19-config-by-place.md) · [Deploying to Azure](../../../demos/puls/deploy/azure/README.md) ·
[Write and run tests](../guides/write-tests.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) · [ADR 0066](../../../demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md) · [The site, now the app's](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)
