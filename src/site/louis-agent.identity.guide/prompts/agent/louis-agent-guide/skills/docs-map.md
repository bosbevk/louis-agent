---
name: docs-map
description: Where each kind of doc sits in the docs root (the index, the architecture, the ADRs, the project docs, the guides, the demos' and packs' docs). Read it before you search the docs, so you know where an answer lives.
metadata:
  level: agent
  load: on-demand
---
# Where things are

The docs root is laid out as the repository is, so a link in one doc leads to the next.

## Never

1. **Never answer from a doc you haven't opened.** A path from this map is where to look, not the answer.

## Steps

1. Say what kind of question it is. A question about *why* goes to an ADR, about *how it works* to a project doc or
   the architecture, about *how to* to a guide.
2. Open that folder's index, find the one or two docs that matter, and read them.
3. Answer from what they say.

## The map

- `README.md` at the top: what the framework is and what runs on it; `ROADMAP.md`: where it is going; `TODO.md`:
  what is still open, one row per item.
- `src/docs/`: the framework's docs. `src/docs/README.md` is the index of everything; `CLAUDE.md` there says how
  the project is worked on.
- `src/docs/architecture/`: what the system is, part by part (the building block view is the map of every project).
- `src/docs/adr/`: every design decision with its reason, one file each, numbered; its `README.md` lists them all.
- `src/docs/projects/`: one design doc per project: what it holds and how it works; its `README.md` lists them.
- `src/docs/guides/`: how to do a thing: build a pack, build an identity, spawn a site, run the tests.
- `src/docs/learning/`: the write-ups of what each skill or feature taught; `src/docs/brand/`: the logo and voice.
- `demos/louis-agent/docs/` and `demos/puls/docs/`: each demo's own docs the same way; `packs/docs/`: the packs'.
- `src/docs/projects/louis-agent.site.md`, *What the site sells, and how to reach Louis*: the
  hosted site with an agent, its price, and Louis's address, so a question about buying or writing to him is
  answered from that doc.

## An example

Someone asks why the agent never runs a shell command. That is a why, so it goes to an ADR. You open
`src/docs/adr/README.md`, find the decision on arguments as data, and read that one file before you answer.

## References

- [`src/docs/README.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/README.md), the index of every doc
- [`src/docs/adr/README.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/README.md) and [`src/docs/projects/README.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/projects/README.md), the two lists this map leans on most
