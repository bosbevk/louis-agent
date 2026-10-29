# Changelog

> **Reference.** What was finished, newest first, one entry per feature, fix or cleanup. **Elsewhere:** what's still
> open in the [TODO](TODO.md); what each built lives on in its project's design doc ([projects](docs/projects/README.md)); the
> feature, fix and plan docs an entry links are at the tag `pre-cl24`, since CL24 folded them.

- 2026-10-29 — **FX28 A replay is marked, and can be started, on the Benchmarks page**: a replay's row says *replay
  of …*, the card's *Last run* line gets a *replay* chip, the Run button's estimate leaves replays out as it leaves
  re-scores out (the cook's free replay `c0362ff73151` had become "≈ R0 for 1 repeat"), and a recorded run's row has
  a *Replay* button; `StoredRunRow.Recorded` from the run's `web` setting (`EvalSettings.Recorded`), beside CL26's
  `ReplayOf`
- 2026-10-29 — **CL26 The eval card, reworked, and the stylesheets split**: the Evals page's card leads with its
  latest run as a ring per mode, the runs before it down a timeline with a bar per mode and a tag that says
  recorded, replay or re-score (a run row says what it replayed, `ReplayOf`), and what it checks in a fold
  (`EvalCardView`, a Presentation Model); both web apps' `app.css` became an `@import` list of area files (Puls 18,
  the code-review app 8), byte-identical when joined, and a `.css` file holds at most 200 lines of rules
  ([0053](docs/adr/0053-stylesheets-by-concern.md); the byte-checked move is
  [0054](docs/adr/0054-a-move-may-copy-line-ranges.md))
- 2026-10-29 — **CL25 Every CLAUDE.md in 200 lines**: `docs/CLAUDE.md` from 558 lines to under 200 and every
  `CLAUDE.md` and `.claude/rules/` file held to 200 by `ClaudeMdSizeTests`; the why of each rule in its ADR (new:
  [0050](docs/adr/0050-summaries-like-a-person.md) summaries like a person, [0051](docs/adr/0051-flat-conditionals-and-named-endpoints.md)
  flat conditionals and named endpoints, [0052](docs/adr/0052-edit-never-script.md) edit, never a script), the how in
  path-scoped rules files (`csharp.md`, `razor.md`, `docs.md`), the how-tos in [guides](docs/guides/README.md)
  (get started, finish a step, add a skill, debug and logs, common errors); one rule, *Rider's tools first*, in
  place of the list of Rider's tools
- 2026-10-29 — **FX27 A sub-session's answer carries no usage**: the footer under a specialist's or minion's answer
  names the child, never its tokens or cost, which are zero on a replay and made the parent's next request miss its
  recording; the usage stays in the ledger and the parent's turn, and a fresh cook recording replays with no miss
  ([fix doc as it was](https://github.com/bosbevk/louis-agent/blob/f54d0e7ed2c93bd177dd300f63b5d2df5fd98931/docs/fixes/FX27-sub-session-footer-cost.md))
- 2026-10-29 — **FX26 A replay miss is marked `replay-miss`**: the replay client throws `ModelReplayMissException`, the
  API streams it as `error.type` `replay_miss_error` (`StreamErrorKind` in session-stream), and the runner sets the
  turn's failure, so a replayed trial with no recording counts as infrastructure, not as the agent's failed answer
  ([fix doc as it was](https://github.com/bosbevk/louis-agent/blob/deb14797f4be3e6b8fa0b66eab9900aedb5f87c9/docs/fixes/FX26-replay-miss-kind.md))
- 2026-10-29 — **CL24 Done docs folded**: the docs of built features and done fixes folded into the project design docs
  and the write-ups, then deleted; the plans of built work deleted; every link to them at the tag `pre-cl24`. Each
  fix's row keeps its symptom and cause; the lessons are in [Lessons from the fixes](docs/learning/fixes-lessons.md);
  [platform.md](docs/plans/platform.md) is the living roadmap with Phases 0 to 3 done; the rule *a feature holds at most
  about ten steps* and its sub-features ([docs/CLAUDE.md](docs/CLAUDE.md), rules 2 and 3) are TODO lines
  ([plan](https://github.com/bosbevk/louis-agent/blob/113501480e9df53e94a84a447f4c3abc5c93e081/docs/plans/CL24-done-docs-folded.md))
- 2026-10-29 — **F18 Evaluation**: every check is a C# eval on one harness (`evals/louis-agent.evals/`): `IEval` and
  `IEvalSet`, datasets in Inspect's Sample shape with a held-out split and a hash, code graders first, a model judge
  calibrated against people's labels (90% of each class), human review in the portal, pass@k and pass^k, a roll-up by
  category, re-scores and replays for $0; the runner and the API share one event contract, protocol v2 (`evals/
  louis-agent.evals.protocol`, [ADR 0047](docs/adr/0047-protocol-v2-one-event-contract.md)); the store goes through
  typed records ([ADR 0048](docs/adr/0048-mongo-through-records.md)) and keeps nothing older than its build ([ADR
  0049](docs/adr/0049-nothing-is-compatible-before-a-sale.md)); the portal reads only the contract's records. First
  v2 baseline: `puls-cook` 23 of 24 in its cook mode, `puls-routing` 72 of 72, $0.37 together; the local judge 31 of
  36, below the bar ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F18-evals.md), write-ups
  [12](docs/learning/12-evaluation.md) and [13](docs/learning/13-cost-and-token-economics.md))
- 2026-10-27 — **FX25 The eval runner sees only what it reads**: a run's process got the API's whole environment; `RunnerEnvironment`
  keeps an allowlist (the runtime's, the runner's settings, and an Anthropic judge's key under the judge's name only), so the Mongo
  login, the admin key and the provider keys never reach an eval or a script ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX25-runner-environment.md))
- 2026-10-25 — **FX22 The recipe's image renders**: an image in an answer renders when its exact URL came back in a tool
  result of the same turn, the cook's sub-session included (`ImageSources`, `Block.Images`, `Markdown.ToHtml(markdown, shown)`),
  not only from a shop's host; a delegation call's own result proves nothing; per turn ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX22-images-from-tool-results.md))
- 2026-10-25 — **CL20 API standards**: every route of the agent API under `/v1` but `/health`, mapped in one place
  (`ApiMap`); resources as nouns (the git routes by resource: `commits`, `commits/{sha}`, `branches/{name}` with its
  slashes, a merged branch deleted with `DELETE`), and both creates answer `201` with `Location`; every `/v1` route
  needs the key unless the code marks it open (the prices and the rates), so `GET /llm-profiles`, open by omission,
  is closed, and an unknown `/v1` path is a JSON 404, not the web app's page; every client names the whole route
  (`v1/sessions`), its setting only the host; CL14's `GET /personalities` and `personality` field gone; the git
  endpoints split by resource (`Endpoints/Git/`); the route table pinned by a golden file, ADR 0039's rule checked on
  it by tests ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL20-api-standards.md), [ADR 0039](docs/adr/0039-api-routes-under-v1.md), write-up
  [11](docs/learning/11-protocols.md))
- 2026-10-24 — **FX21 The browser says what it is**: `BROWSER_USER_AGENT`, what the headless browser tells every site it
  reads for a user; the Puls stack, its launch profile and the Azure app set `Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/docs/SITE_OWNERS.md;
  reads a page on a user's behalf)`, unset sends Chrome's own, the automation marker is never touched, and never a
  person's name or place ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX21-the-browser-says-what-it-is.md))
- 2026-10-24 — **CL19 Config and Docker by place**: a folder is one kind of thing for one place. `config/` is what the
  app reads at run time (unchanged, so no code moved; `rider/` for the IDE templates); `docker/` is this PC (`images/`
  for every Dockerfile, Ollama's from `services/`; two stacks, one Puls container that peers with itself); `deploy/azure/`
  is the cloud: `puls-app` on a `ts-forwarder` sidecar that carries both tunnel ports from one tailnet node, its yaml
  and SearXNG's tracked with `secretRef`s and no secret value, `deploy.ps1` (build, secrets, update, searxng-settings,
  logs) and the how-to; `tailscale serve` on the PC is the home end. A README in each of the three folders
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL19-config-by-place.md))
- 2026-10-20 — **CL15 Engine, identities and packs** (the platform plan's Phase 2): core is the engine and names no
  product, shop or country; Puls is an identity (`identities/louis-agent.identity.puls`: its voice, modes, roles and
  the packs it names) and the shops a pack (`packs/louis-agent.pack.shops`: the toolset, its guide, settings and
  texts); one `AgentCatalog`, built by the composition root (`src/louis-agent.composition`) from `IDENTITIES` and
  `PACKS`, checked at start-up; `HOST_TIME_ZONE` replaces the South African default; every pack convertible to an MCP
  server, held by `PackConvertibilityTests`; the fakes the test projects share in `tests/louis-agent.testing`. During the
  build Louis added two rules, held by ratchet tests: a `.razor` holds markup only (`RazorHasNoCodeTests`) and a file
  holds at most 200 lines, a test file 400 (`FileSizeTests`), with core, the API, ACP and the web apps sorted and split
  by concern ([ADR 0034](docs/adr/0034-code-a-person-can-maintain.md)); the ACP host pinned by a transcript test before
  it moved. Proved
  by three goldens and one paid cook run on the finished branch, `b4a6ea22b178`, read against the two runs on main in
  write-up [07](docs/learning/07-context-engineering.md) ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL15-engine-identities-packs.md),
  [ADR 0027](docs/adr/0027-engine-identities-and-packs.md), [ADR 0028](docs/adr/0028-packs-stay-convertible-to-mcp.md),
  PR #71, write-ups [04](docs/learning/04-tool-design.md) and [18](docs/learning/18-testing-and-shipping.md))
- 2026-10-19 — **FX20 Skill files wait for approval** (the platform plan's Phase 1, its last door): a skill file the
  agent writes goes to `Skills/agent/functions/pending/`, active only in the engine that wrote it, until the user's
  `/approve <name>` moves it up for every later session; `/reject` deletes it and `/tools` lists it, as agent-built
  tools already did ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX20-skill-files-wait-for-approval.md))
- 2026-10-19 — **FX19 Git arguments as data** (the platform plan's Phase 1): every git tool runs from a list of
  arguments, the model's values after `--end-of-options` and paths after `--`, a value that starts with `-` refused
  before git runs, and `SetConfig` limited to `user.name` and `user.email`; `ArchitectureTests` keeps a command-line
  string out of core ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX19-git-arguments-as-data.md), [ADR 0004](docs/adr/0004-arguments-as-data.md) amended)
- 2026-10-19 — **FX18 Auth fails closed** (the platform plan's Phase 1): the API, and the MCP server over HTTP, refuse
  to start without a key unless `AGENT_AUTH=off` says so on purpose; a key that is set is always checked. One pure
  rule in core, `Guardrails/HostAuth`, replaces the two hosts' own checks ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md))
- 2026-10-19 — **F29 Validated specialists**, closed by [the platform plan](docs/plans/platform.md) (Phase 0): its
  four steps were in git (`d79dbb1`, `ae257c5`, `d7afa23`, `28c4901`) though its doc said *planned*. `ROLES_ENABLED`
  (`PERSONALITIES_ENABLED` before CL14) gates the roles an instance may use; Puls opens with `specialists` (then
  `agents`) and `web`, answers the web itself and hands only cooking to the cook, until each other role has a report
  shape, a relay rule and a check ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F29-validated-specialists.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F29-validated-specialists.md))
- 2026-10-19 — **F26 Specialist follow-up**, built 2026-10-15, closed by the platform plan: a sub-session stays open
  for its chat and Puls continues it by handle (`ContinueSubSession`), so a follow-up on the cook's meal goes to the
  same cook with its context; the firmer follow-up rule measured 4 of 4 continued, and the Werk panel names the role.
  Its open item, a continued specialist's ledger rows, is a fix on Puls's track
  ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F26-specialist-follow-up.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F26-specialist-follow-up.md))
- 2026-10-19 — **F25 Local cook**, built 2026-10-15, closed by the platform plan: Puls's first local specialist, a
  South African meal with its recipe and a shopping list priced in rand at the shops near the user, the routing and
  the sources measured; Tavily searches without refusals. Its open item, the cook's unconfirmed prices (*geskat* or
  *unknown*), is on Puls's track ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F25-local-cook.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F25-local-cook.md))
- 2026-10-19 — **F22 Role tools**, built 2026-10-13, closed by the platform plan: the tools Puls's specialists need,
  `GetToday`, attachments by name, documents (PdfPig, OpenXml, ExcelDataReader), `Blame`, `FileHistory` and
  `SearchCommits` with arguments as a list, `ReadTable`, `ConvertCurrency`, and the user's place. Its open item, binary
  attachments sent as files, is a fix on Puls's track ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F22-role-tools.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F22-role-tools.md))
- 2026-10-19 — **F21 Personalities**, built 2026-10-13, closed by the platform plan: each session (and sub-session)
  names its personality, now its role; sub-sessions get no router, and all 24 delegations in the three-run delegation
  check went to the role the router names. Its open item, Louis's corrections to the Afrikaans, is on Puls's track
  ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F21-personalities.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F21-personalities.md))
- 2026-10-18 — **FX17 The cook's labels and counts**: the recipe line and the tally in the task's language (*Dien* /
  *Serves*, *Geprys* / *Priced*), and a count of packs after the name and the price, so the photo stays first; and a
  follow-up's card names the cook while it works, from the role each session's `message_start` now carries
  (`session_role`) ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX17-cook-labels-and-counts.md))
- 2026-10-18 — **F34 Shop deals**: Checkers searched in the store that serves the user's town (its own address call,
  the one approved direct call, ADR 0023), found once per town and kept a day; `SearchSpecials` (multi-buys) and
  `SearchPromotions` with the shop's wording; the cook prices only shops it has a tool for, none: the list with `-` and
  a no-catalogue line, deals first, packs the recipe needs, every product with its photo, and Puls asks how many people.
  A round's calls run at once for read-only toolsets (ADR 0026), two searches at once per shop with a pause after a
  refusal. The cook check compares Puls with the cook's tools against Puls with its cook: run `d94597a51d4d` priced 31 of
  31 with the cook (Prince Albert rightly without prices), 13 of 18 alone ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F34-shop-deals.md))
- 2026-10-17 — **CL14 The public word: role**: what callers see says role: `POST /sessions` takes `role` and the session
  JSON returns it, `GET /roles`, `ROLES_ENABLED`, the model's `StartSubSession(…, role, …)` and the prompts that teach
  it, the voices' headings, the web apps and the benchmarks. Expand, then contract: the old words are still read
  (`personality`, `GET /personalities`, `PERSONALITIES_ENABLED` with a warning) until a TODO line removes them, and
  recorded data (stored runs, old chats, the stream fixture) is read either way for good. Checked by hand (free): the
  language menu, a cook opened with either word, and a run stored before CL14 in the portal
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL14-public-word-role.md))
- 2026-10-17 — **CL13 Skills by role**: `Skills/` in the seams' pattern, the contract at the top, `Providers/`
  (`MarkdownSkillProvider`, `CompositeSkillProvider`) and `Roles/` (`RoleCatalog`, `RoleLevel`, `RoleException`,
  `RoleHeader`); "personality" becomes "role" in the code, `RoleNamingTests` holding it; the unused
  `PersonalitySkillsProvider` deleted, and `MarkdownSkillProvider`'s unused default-routing flag; inside `Skills/` the
  top uses nothing below it (`ArchitectureTests`). The golden files never changed, and a cook session on the rebuilt
  `puls-agent` still opens with `web, shops` (checked by hand, free). The public word (`personality` on the API and
  `StartSubSession`) is CL14's ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL13-skills-by-role.md))
- 2026-10-17 — **F33 Shop catalogues**: a shop's products and exact prices read from its own catalogue through the
  guarded browser (`Tools/Web/Browsing`, extracted from F32's renderer), Checkers first (`Tools/Shops`,
  `Providers/CheckersCatalogue`); a `shops` toolset with `FindProducts`, off unless `SHOP_CATALOGUES` names the shop.
  The cook loads it itself, from its role's header (`toolsets: shops?`), optional toolsets (`?`) loading only where an
  instance has them. Its run priced 19 of 23 ingredients with sources (3 more at their price per kg, which the scorer
  now counts apart), F32's 0 of 32. Plans now carry a namespace map ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F33-shop-catalogues.md))
- 2026-10-17 — **F32 Self-hosted web**: `WebSearch` through SearXNG in the Puls stack (development and benchmarks
  only, ADR 0023: the sold endpoint keeps Tavily) and pages drawn with JavaScript rendered by a headless Chrome
  (PuppeteerSharp), every request a page makes held to the address rule and no WebSockets; both containers pinned by
  digest, off the database's network. Choices are enums (ADR 0024); the web toolset is laid out by role (`Search/`,
  `Rendering/`, each with its `Providers/`). Its run ($0.057) searched and rendered with no key but priced nothing:
  F33 follows ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F32-self-hosted-web.md))
- 2026-10-17 — **F31 Benchmarks in the admin portal**: the checks (`benchmarks/`: a shared Python harness, the cook
  check, the routing check and a free stub) run only from the portal, and the API records each run from the
  script's stdout line protocol into MongoDB (`puls-mongo`, `puls_benchmarks`), costed from the usage ledger; the
  explorer reads a run at every level (nine charts per mode, the conversations side by side, each turn replayed, each
  request) and a trend per check, every chart explaining itself; a price counts only with its source. The first paid
  run (cook check, $0.045, the ledger's figure exactly) found Tavily out of credits. The portal is in English at
  `/admin/benchmarks`. Closes F27 ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F31-benchmarks-portal.md))
- 2026-10-17 — **FX16 A price is checked against its product**: `VerifyQuote` refuses a quote that is only a price
  (`R22.99`) unless it names the product, and then finds it only right after the product's name, no other price
  between; it was *Found* for any product at that price ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX16-short-quote-match.md))
- 2026-10-17 — **CL10 Clear the decks**: `samples/` goes (both checks, 18 recorded runs, the order-service demo and
  its compose file) and `docs/BENCHMARKS.md`; they're kept at the tag `pre-cl10`, where the history docs now link. The
  admin portal stops reading and writing run folders: no checks until they're rewritten, with scripts in `benchmarks/`
  and results in SQL Server's `puls_benchmarks` (the next feature, before F30's minions are measured). Roles are
  named, not derived: `roles.json`, `RoleFor` and `puls-specialist` go; the other specialists stay, off by
  `PERSONALITIES_ENABLED` ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL10-clear-the-decks.md))
- 2026-10-17 — **CL9 Modes per product**: a level-0 session is a mode, one file of voice and routing in a folder per
  product (`agent/louis-agent/voice.md`, the default for the CLI, Rider and code review; `voice-orchestrator.md`), or a
  language (`agent/puls/voice-af.md`, `voice-en.md`) that loads Puls's shared routing (`puls/voice.md`); Puls's *Taal*
  menu picks the language for new chats. Every agent loads the same principles first (never lie, cheat, steal or
  murder). Each product routes only what it does; `AGENT_FUNCTION` and the placeholder code-review skill go; the
  `agents` toolset is `specialists`. Measured from the start-up logs: the coding agent's fixed prefix 29,464 → 28,663
  tokens, the cook's 4,518 → 4,773 (the principles), Puls's chat 6,703 (Afrikaans) and 6,694 (English); the paid
  checks were skipped until they're rewritten after F30 ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL9-modes-per-product.md),
  [ADR 0019](docs/adr/0019-modes-one-file-per-product.md), [ADR 0020](docs/adr/0020-principles-for-every-agent.md))
- 2026-10-16 — **CL8 Skills by level**: the built-in prompts move into `louis-agent.core/Prompts/`, one folder per
  level a session runs at (`agent/` level 0, `specialists/` level 1, `minions/` level 2 for F30) beside the shared
  `toolsets/`, and are read only from the assembly (`Microsoft.Extensions.FileProviders.Embedded`): a role opens only at
  its own level, a copy on disk no longer overrides one, and a wording change is a rebuild. The repo's `Skills/` keeps
  what the agent writes; the placeholder time-logging skill goes; `SkillsLayoutTests` holds the layout
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL8-skills-by-level.md), [ADR 0017](docs/adr/0017-skills-by-level.md),
  [ADR 0018](docs/adr/0018-built-in-prompts-embedded.md))
- 2026-10-15 — **Licence: all rights reserved** (was AGPL-3.0): public to read, but no use, copying, changes or hosting
  without written permission; earlier versions keep their licence ([ADR 0016](docs/adr/0016-licence-all-rights-reserved.md))
- 2026-10-15 — **Licence: AGPL-3.0** (was MIT): still free to use, change and share; whoever changes it and runs it as a
  service must publish the changes, so a hosted Puls's improvements stay open ([ADR 0015](docs/adr/0015-licence-agpl.md))
- 2026-10-15 — **F24 Landing motion**: Puls's landing page as a moving page on the brand's idea (a puls is a disturbance
  that travels from its source): dark water behind it, the app's three panels playing a scripted turn in the page, a
  pinned *In die app* tour, a Kenmerke bento over two marquees, Syfers counting up the measured numbers, an accordion
  FAQ, every link landing its section centred; the app gets the water behind an empty chat, a delegation card that
  opens with a round plus, costs that count up, Blazor's real loading count, and an *Animasies* switch that overrides
  the system's reduced-motion setting in this browser ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F24-landing-motion.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F24-landing-motion.md))
- 2026-10-14 — **FX15 A test needed `config/.env`**: the display-currency test names the tracked rates and prices
  files; it had relied on `config/` being found next to a `.env`, which a fresh clone lacks, so the first CI run found
  it ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX15-test-needs-env-file.md))
- 2026-10-14 — **CI**: `.github/workflows/ci.yml` builds and tests the whole solution on Ubuntu on every push and pull
  request (the test results kept as an artifact); a regression is caught before a review, not only locally
- 2026-10-14 — **F5 follow-up, sub-session usage rolled up**: a delegated answer's usage line, the chat total, `/usage`
  and `GET /sessions/{id}` include the specialist's requests, tokens and cost, as one `sub_session` record per reply
  (with `requests`, the model requests it stands for) that is also a row in the parent's ledger; ledger reports list
  those rows without totalling them, since the specialist's own rows are in the ledger it ran on
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F05-usage-rollup.md))
- 2026-10-14 — **FX9 Cancelled requests in the ledger**: a non-streaming request that times out or is cancelled (the
  orchestrator's triage) is written to the ledger as `cancelled` with unknown tokens, as a cancelled stream already
  was; provider errors stay unrecorded ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX09-cancelled-request-usage.md))
- 2026-10-14 — **CL7 Docs tidy**: the top of `docs/` keeps the eight docs most readers need. The session picture moves
  to `docs/specs/images/` (the 43 KB SVG; the 1.1 MB PNG of it goes), How a turn works joins arc42's runtime view, and
  the runbooks move beside the orchestrator's project doc; every link repointed ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL7-docs-tidy.md))
- 2026-10-14 — **Test first**: a step that changes behaviour starts with its tests, run and seen failing, then the
  code; one commit with both, the red output in the step's explanation ([ADR 0014](docs/adr/0014-test-first.md))
- 2026-10-14 — **CL6 arc42, ADRs and C4**: the architecture in arc42's twelve sections in `docs/architecture/`
  (ARCHITECTURE.md's text moved unchanged, plus goals, constraints, C4 context, container and component diagrams,
  quality scenarios and a glossary), and 13 decisions as ADRs in `docs/adr/`, each written from where it was
  recorded and linking its sources; the guide's rules link their ADRs, and a plan that makes a lasting decision adds
  one ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL6-arc42.md))
- 2026-10-14 — **CL5 Guides by folder**: the development guide, which loads into every Claude Code session, keeps what
  every task needs (~11.4k → ~6.2k tokens); core's how-tos move to `louis-agent.core/CLAUDE.md` (~4.8k), the tests' to
  `tests/CLAUDE.md`, the hosts' to `src/CLAUDE.md`, which Claude Code loads only when working there (checked with
  `/context` in a fresh session). None in `Skills/`, which core embeds and reads as personalities
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL5-guides-by-folder.md))
- 2026-10-14 — **CL4 One type per doc**: every doc is one Diátaxis type (tutorial, how-to, reference, explanation;
  plans a fifth kind), named in the docs index and in a quoted opening line. Six mixed docs split, moving facts, not
  dropping them: the README 230 → 152 lines, the TODO's Done to this file and its ticked lines removed, the benchmark
  runs to `docs/BENCHMARKS.md`, Architecture's settings to SETUP and MODELS, How a turn works' how-tos to the guide.
  Five out-of-date facts found on the way were fixed in their own commits (the default model, tool registration since
  F5, F5's status, deleting `personality.md`, "no shell"); a link check ran after every step
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL4-doc-types.md))
- 2026-10-14 — **F5 Toolset profiles**: a session names its toolsets when it opens and keeps them; an engine per
  toolset list with only those tools and their guides; sub-sessions on a peer (`StartSubSession`), which Puls's
  delegation runs on. Short benchmark: prefix −50% (~48.5k → ~24.4k tokens), run −23%, 4/4 fixes, the cache still hits
  ([numbers](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#f5-benchmark-short-run-2026-10-13)); the full 10-fix run waived by Louis
  ([F5](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F05-toolset-profiles.md))
- 2026-10-14 — **F20 Puls look**: a landing page and a dark three-panel dashboard in Puls's colours, with the Werk panel showing each delegation live (specialist, steps, sources, cost); checked in a real browser with a live delegation ([F20](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F20-puls-look.md)).
- 2026-10-14 — **FX14 Sub-session prompt budget**: `SUB_SESSION_PROMPT_BUDGET` (80k, under the 100k price step) stops a specialist's prompt growing into long-prompt prices; it answers from what it has ([FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX14-sub-session-prompt-budget.md)).
- 2026-10-14 — **FX13 Counting to the analyst**: Puls's router sends counting to `files` + a script, and the reader hands counting back instead of reading every file ([FX13](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX13-counting-to-the-analyst.md)).
- 2026-10-14 — **FX12 Env templates readable**: `.env.example`, `.sample` and `.template` files are no longer treated as secrets ([FX12](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX12-env-templates-readable.md)).
- 2026-10-14 — **FX11 Empty sub-session answer**: a specialist that ends with no text (its 10 rounds used) is asked once more in the same session instead of Puls starting over ([FX11](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX11-empty-sub-session-answer.md)).
- 2026-10-14 — **FX10 Optional peer name**: `StartSubSession` without `agent` uses the only peer; Puls's calls without it were refused ([FX10](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX10-optional-peer-name.md)).
- 2026-10-12 — **Ollama on the GPU and local triage**: `ollama-nvidia` and `ollama-amd` services sized for a 4 GB card
  (32k context, flash attention, 8-bit KV cache); Ollama's client waits 10 minutes, not 100 seconds. The short demo's
  triage on `qwen3:4b-instruct-2507` (profile `.env.ollama-triage`): 4 fixed, 1 escalated, as on Claude, triage $0, the
  run $0.042 against $0.049, 2.4× slower; llama3.1 and qwen3's thinking variant failed
  ([Local triage](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#local-triage-2026-10-12),
  `samples/sample-output-short-local-triage/`)
- 2026-10-12 — **CL3 Messages and logging**: every exception, log line, tool error and user text is a message enum
  member with a `[Description]` template (486: 313 in core, the rest in each host's own `Messages/`), proved to render
  as before; `ToolMessageSnapshot.json` pins the 190 texts tools return to the model. Logging is `ILogger` with
  Serilog: stderr only, the run file and the JSON-lines records as sinks, holes kept as fields; `AgentLog` is gone.
  The orchestrator logs to stderr too, and `decisions.jsonl` is snake_case like its other records
- 2026-10-12 — **CL2 Split AgentEngine**: 1,078 lines become the engine (the turn loop, 360) and `ToolRegistry`,
  `ToolCallGuards`, `SkillTools`, `AgentBuiltTools` in `Agent/`; pure checks in `Guardrails/` (`WorkspaceBoundary`,
  `SensitiveFiles`, `SsrfGuard`); the console chat in the CLI. A golden file pins what the model is sent; it found that
  the prompt and two descriptions took the checkout's line endings, now always LF
- 2026-10-11 — **CL1 Project layout**: core in folders by concern (`Agent`, `Llm/{Name}Provider`, `Tools/{Toolset}`,
  `Skills`, `Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`, `Config`, `Messages`, `Logging`), the namespace the folder
  path, one type per file; PascalCase namespaces in every project; the tests mirror the source; `ArchitectureTests`
  fails on a namespace off its folder, two types in a file or a dependency against the layers. Behaviour unchanged:
  the same 779 tests pass. Moved with `git mv`, so history follows
- 2026-10-11 — **F4 Prompt caching**: two cache markers on Anthropic (the system prompt with `LLM_CACHE_TTL`, the
  conversation by automatic caching, 5 minutes), as a provider strategy; tools sorted; writes priced at their lifetime
  (`cache_write_1h`); `usage-probe --cache-gaps` recommends the lifetime from the ledger. Full demo $0.13 against M1's
  $0.66 (−80%), louis-agent's input cost −87%, 10/10 fixes; samples in `samples/sample-output-cached/`
- 2026-10-11 — **F14 Display currency**: costs in the PC's currency in every host (rands from `HOST_CULTURE=en-ZA`,
  written by `usage-probe --host-culture`; `R0.034`, `.` decimals), at a dated rate (Frankfurter mapped into our
  `rates.json`, `config/rates.json` as the fallback, served at `GET /rates`), with the rate and the USD charged beside
  converted totals; a currency picker in the web app. The ledger stays in USD. Both demo samples rerun in rands

- 2026-10-11 — [FX8](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX08-fix-result-quotes.md): a FIX-RESULT whose summary quotes without escaping
  ("Cher") is still read, instead of asking louis-agent to continue three times; found by F14's full demo rerun

- 2026-10-11 — **F3 Show usage** (M1 done): every answer says what it used and cost — a line on the CLI's stderr,
  under each answer in the web app (with the chat's total), in Rider with `USAGE_IN_CHAT=true` — `/usage` in every
  host, `usage` on the API's `message_delta` and `GET /sessions/{id}`, and cost per reply, per error and per run in the
  orchestrator's comms log. The short demo's comms log total equals its ledger's exactly ($0.2526, 57 requests)
- 2026-10-11 — [FX7](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX07-timeout-ends-run.md): a timed-out request fails that error, not the
  orchestrator's whole run; found with the new `run-demo.ps1 -OrchestratorProfile`

- 2026-10-11 — [FX6](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX06-usage-on-failed-turns.md): a failed louis-agent turn still reports its usage (on
  the API's `error` event), so the web app's chat total and the orchestrator's comms log match the ledger; built in F3

- 2026-10-11 — [FX5](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX05-null-conditional-ledger.md): a `?.` in F3's first step skipped the ledger write for
  requests outside a usage scope; F1's tests caught it before the commit

- 2026-10-10 — **F2 Prices and cost**: every ledger record priced from a tracked `config/prices.json` (Haiku 5.5's
  long-prompt tier included), or from the API's `GET /prices` with `PRICES_URL`; `usage-probe -- --ledger` totals a
  ledger; M1 baseline $0.6586 for the full demo ($0.0625 a fix), both sample runs rerun with priced ledgers
- 2026-10-10 — Full demo on Claude Haiku 5.5 with the usage ledger: 10/10 fixes confirmed, 2 ignored, 1 escalated, ≈ $0.66
  (`samples/sample-output/`, now with `usage-2026-10.jsonl`); F2 prices that ledger for the M1 baseline
- 2026-10-10 — [FX1](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX01-round-limit-tools.md): the engine keeps the tools on the request after the
  10-round limit (`tool_choice: none`), which Haiku 5.5 otherwise rejects; the short demo went from 2/4 to 4/4 fixes
- 2026-10-10 — **F1 Usage ledger**: every model request of every host in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, with
  host, session, turn, purpose and the orchestrator's fix and run; `tools/louis-agent.usage-probe`; a short demo
  (`run-demo.ps1 -Short`) saved with its ledger in `samples/sample-output-short/` (4/4 fixes, ≈ $0.27 on Haiku 5.5)
- 2026-10-10 — Default Anthropic model is Claude Haiku 5.5 (about 7.5× cheaper than Haiku 4.5; usage, thinking and
  thinking text checked with `tools/louis-agent.usage-probe`); LLM profiles `config/.env.anthropic` / `.env.ollama`
- 2026-10-07 — Specs and feature plan for usage, budgets and cheaper responses (`docs/specs/`, `docs/features/`); the
  session-architecture and orchestrator designs moved from this list into `docs/specs/`
- 2026-10-07 — Docs: [how the agents and endpoints talk](docs/AGENT_COMMUNICATION.md), [runbooks](docs/projects/orchestrator-runbooks.md), a
  docs index in reading order
- 2026-10-07 — Web app Branches tab: merges and deletes are queued instead of silently dropped while another runs
- 2026-10-07 — Comms log keeps louis-agent's text blocks apart
- 2026-10-06 — Orchestrator demo in Docker: ten bugs → ten fix branches, Markdown comms log, sample output with a
  recording of the merges
- 2026-10-06 — Web app Branches tab and history endpoints: all commits, branch and commit diffs, merge, delete merged
- 2026-10-06 — Root `.dockerignore`: builds no longer send `config/.env.secrets` or `.git` into the build context
- 2026-10-06 — Orchestrator POC: runbook per API method, fixes delegated to louis-agent.api and verified; `AgentEngine`
  `toolsets:` option
- 2026-10-06 — MCP server: Streamable HTTP transport (`MCP_TRANSPORT=http`) with its own `MCP_API_KEY`
- Earlier — Skill files standardised (titles, tool tables, Python fences); DevOps blockers query fixed (tag, not type);
  sensitive-file checks added to `DeleteDirectory` and `CopyFile`; all 108 tool descriptions rewritten
