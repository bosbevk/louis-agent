---
name: docs-map
description: Where each kind of doc sits in the docs root, the index, the architecture, the ADRs, the project docs, the guides, the demos' and packs' docs. Read it before you search the docs, so you know where an answer lives.
metadata:
  level: agent
  load: on-demand
---
# Where the docs are

The docs root is laid out as the repository is, so a path in one doc leads to the next.

## Never

1. **Never give a path you haven't read.** A doc you didn't open is a guess.

## The map

- `README.md` at the top: what the framework is and what runs on it; `ROADMAP.md`: where it is going; `TODO.md`:
  what is still open, one row per item.
- `src/docs/README.md`: the index of every doc.
- `src/docs/architecture/`: what the system is, part by part; the building block view is the map of every project.
- `src/docs/adr/`: every design decision with its reason, one file each, numbered; its `README.md` lists them all.
- `src/docs/projects/`: one design doc per project, what it holds and how it works; its `README.md` lists them.
- `src/docs/guides/`: how to do a thing, such as build a pack, build an identity, spawn a site or run the tests.
- `src/docs/learning/`: the write-ups of what each feature taught.
- `demos/louis-agent/docs/`, `demos/puls/docs/` and `packs/docs/`: each demo's own docs, and the packs', laid out
  the same way.

A question about why goes to an ADR, about how it works to a project doc or the architecture, about how to do
something to a guide.

## An example

A developer asks why the engine runs its own tool loop. That is a why, so you search `src/docs/adr/`, read the ADR
on the Messages API tier, and answer from it with its path.

## References

- [`src/docs/README.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/README.md), the index of every doc
