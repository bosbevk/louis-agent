# louis-agent.identity.expert — design

> **Explanation.** Who the framework's guide is, the expert in the site's corner, and how it answers. **Elsewhere:**
> the pack it reads with in [louis-agent.pack.docs](../../../../packs/docs/projects/louis-agent.pack.docs.md); the
> widget that shows it in [louis-agent.web](../../../../src/docs/projects/louis-agent.web.md); the decisions in
> [ADR 0099](../../../../src/docs/adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md) and
> [ADR 0100](../../../../src/docs/adr/0100-the-site-tells-the-whole-architecture.md).

> **Kind:** Class library (`net10.0`) · **Path:** `demos/louis-agent/identity/louis-agent.identity.expert/` ·
> **References:** `louis-agent.composition` only · **Used by:** `louis-agent.coding-catalog`, beside the coding identity

## Purpose

The louis-agent site has a chat in its lower-right corner (F109.3). Whoever answers there has to know the framework
the way its docs tell it, and nothing else: no files, no git, no shell. That is this identity, `louis-agent-expert`:
one voice, one pack. The coding agent's demo runs it beside the coding identity in one catalogue, and a session says
which of the two it opens as (a session names its identity, F109.3 step 1).

## How it works

- `ExpertIdentity`: the name, the voice `louis-agent-expert/voice` as the default, and the docs pack as its one pack.
- `Prompts/agent/louis-agent-expert/voice.md`: who it is (the guide, a solo project from South Africa; since F110,
  fun, bubbly and excited, it teaches one thing with every answer and sells by showing, with one nudge at the end,
  honest always; since FX72 a salesperson that reads the person: plain words for someone who has never written code,
  no code or file names, and the full technical answer once it knows it talks to a software developer), how it answers (reads before it speaks, names the doc, says what the docs don't say, numbers only
  from a doc, plain words, only what works today), what stays Louis's (the roadmap's money, numbers and risks and
  the TODO's notes are never quoted; a price question goes to the contact page) and where things are in the docs
  root, so the first tool call goes to the right folder; and, since F109.4, the site's map: each page, what it says
  and the docs it was written from, so a question about a page's claim goes to its source, and `/chat` named as not
  the guide's to run.
- Its knowledge is the `docs` toolset over `DOCS_ROOT` (the image carries the docs; the PC mounts the repo's), so an
  answer can name the doc it read. Louis's picks: an identity, not a role (A2); a tool, not an index or a digest (B1).

## Tests

`CodingCatalogTests` in `louis-agent.coding-catalog.tests` holds the catalogue to two identities with the expert on the
docs pack; the model-request golden pins its voice as the model reads it; `GuideKnowsTheSiteTests` in the
code-review app's tests holds the voice to every page of the site.

---
[Docs index](../../../../src/docs/README.md) · Related: [the coding agent's web app](louis-agent.code-review.md)
