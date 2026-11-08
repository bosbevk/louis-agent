# louis-agent.identity.expert — design

> **Explanation.** Who the framework's guide is, the expert in the site's corner, and how it answers. **Elsewhere:**
> the pack it reads with in [louis-agent.pack.docs](../../../../packs/docs/projects/louis-agent.pack.docs.md); the
> widget that shows it in [louis-agent.web](../../../../src/docs/projects/louis-agent.web.md); the plan in
> [F109](../../../../src/docs/plans/F109-the-site-in-the-demos-web-app.md).

> **Kind:** Class library (`net10.0`) · **Path:** `demos/louis-agent/identity/louis-agent.identity.expert/` ·
> **References:** `louis-agent.composition` only · **Used by:** `louis-agent.coding-catalog`, beside the coding identity

## Purpose

The louis-agent site has a chat in its lower-right corner (F109.3). Whoever answers there has to know the framework
the way its docs tell it, and nothing else: no files, no git, no shell. That is this identity, `louis-agent-expert`:
one voice, one pack. The coding agent's demo runs it beside the coding identity in one catalogue, and a session says
which of the two it opens as (a session names its identity, F109.3 step 1).

## How it works

- `ExpertIdentity`: the name, the voice `louis-agent-expert/voice` as the default, and the docs pack as its one pack.
- `Prompts/agent/louis-agent-expert/voice.md`: who it is (the guide, a solo project from South Africa), how it answers
  (reads before it speaks, names the doc, says what the docs don't say, numbers only from a doc, plain words, only
  what works today) and where things are in the docs root, so the first tool call goes to the right folder.
- Its knowledge is the `docs` toolset over `DOCS_ROOT` (the image carries the docs; the PC mounts the repo's), so an
  answer can name the doc it read. Louis's picks: an identity, not a role (A2); a tool, not an index or a digest (B1).

## Tests

`CodingCatalogTests` in `louis-agent.coding-catalog.tests` holds the catalogue to two identities with the expert on the
docs pack; the model-request golden pins its voice as the model reads it.

---
[Docs index](../../../../src/docs/README.md) · Related: [the coding agent's web app](louis-agent.code-review.md)
