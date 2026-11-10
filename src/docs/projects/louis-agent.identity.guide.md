# louis-agent.identity.guide — design

> **Explanation.** Who the framework's guide is, the guide in its website's corner, and how it answers. **Elsewhere:**
> the pack it reads with in [louis-agent.pack.docs](../../../packs/docs/projects/louis-agent.pack.docs.md); the widget
> that shows it in [louis-agent.web](louis-agent.web.md); the site it sits on in [louis-agent.site](louis-agent.site.md);
> the decisions in [ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md),
> [ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md) and
> [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).

> **Kind:** Class library (`net10.0`) · **Path:** `src/site/louis-agent.identity.guide/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.site-api` (the site's host), and the coding agent's
> catalogue beside the coding identity

## Purpose

The framework's website has a chat in its lower-right corner (F109.3). Whoever answers there has to know the framework
the way its docs tell it, and nothing else: no files, no git, no shell. That is this identity, `louis-agent-guide`:
one voice, three packs. Since F124 it is the framework's own, beside its site in `src/site/`, renamed from the expert
(Louis's pick: the name says what it is), and the site's host runs it alone, so a visitor reaches nothing else.

## How it works

- `GuideIdentity`: the name, the voice `louis-agent-guide/voice` as the default, and three packs: the docs pack it
  answers from, since F112 the leads pack, whose one tool mails Louis a lead when a visitor is interested and has said
  yes ([ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md)), and since F117 the site pack, whose
  one tool takes the visitor to a page or an area of it (the widget hears it and moves the browser).
- `prompts/agent/louis-agent-guide/voice.md`: who it is (since F128.1, Louis, the AI version of the man who made
  louis-agent, with his own story told as "I": Malmesbury, Yzerfontein, the PC at 10, .NET for twenty years, four
  kids, the outdoors, a stoic who reads Kierkegaard; it talks like him, plain, dry and at home, with an idiom where it
  helps; the widget's title reads "Louis · AI", it says it is the AI the moment that matters, never adds to his story,
  and leaves the man's work, the call, the lead and a meeting in Cape Town and surrounds, to the real one. It
  teaches one thing with every answer and sells by showing, with one nudge at the end,
  honest always; since FX72 a salesperson that reads the person: plain words for someone who has never written code,
  no code or file names, and the full technical answer once it knows it talks to a software developer), what we
  sell today (web hosting with an AI agent for small businesses, R500 to R1,000 a month: the one public price, and
  what an agent does for a business in the owner's words), how it answers (reads before it speaks, names the doc for a
  developer only, since FX77, says what the docs don't say, numbers only from a doc, plain words, only what works
  today), what stays Louis's (the roadmap's money, numbers and risks and the TODO's notes are never quoted; any other
  price question goes to the contact page).
- Its skills beside the voice, in `skills/` (since F129,
  [ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md)): `site-pages.md`, always in the prompt, the
  index of every page, one line each, so it knows which page to open; and two it reads with `ReadSkill` when a
  question needs them: `docs-map.md`, where things are in the docs root, so a search goes to the right folder, and
  `site-map.md` (since F109.4), what each page says and the docs it was written from, so a question about a page's
  claim goes to its source. Splitting them off made its fixed prefix about 14% smaller, measured live.
- Its knowledge is the `docs` toolset over `DOCS_ROOT` (the site's image carries the docs; the PC mounts the repo's),
  so an answer can name the doc it read. Louis's picks: an identity, not a role (A2); a tool, not an index or a digest
  (B1).

## Open on purpose

The guide is more open than most agents on business sites (Louis, 2026-11-09: "quite open if compared to how other
online agents is configured, but that is on purpose"). Many site agents follow a script or a short list of topics,
turn away anything off it, and never say what they read. This one needs no sign-in (ADR 0099), follows the visitor's
own questions across the whole framework, reads the docs and says where an answer came from, gives a developer the
full technical answer, and says plainly when the docs don't cover something. That is the point: the site sells agents,
and an agent that answers freely and honestly is the demo.

What stays closed is held in code, not by narrowing what it talks about. Its host loads only its three packs, so it has
no files, git or shell. It sends a lead only on the visitor's yes. It never quotes Louis's own money, numbers, risks or
notes. A cap limits what a visitor can spend (ADR 0099).

## Tests

`GuideIdentityTests` and `SiteCatalogTests` in `src/tests/site/louis-agent.site.tests` hold its name, its voice's
path, its three packs and the site's catalogue to the guide alone; `GuideKnowsTheSiteTests` holds the voice and its
skills to every page of the site; `GuideSkillsTests` holds the split (every page in the always index, the two maps on
demand); `CodingCatalogTests` holds it beside the coding identity in the coding agent's catalogue.

---
[Docs index](../README.md) · Related: [louis-agent.site](louis-agent.site.md)
