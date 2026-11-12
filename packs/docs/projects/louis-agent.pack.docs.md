# louis-agent.pack.docs

> **Explanation.** What the docs pack does and why it's built this way. **Elsewhere:** the rule behind it in
> [ADR 0080](../../../src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md); building a pack in
> [Build a pack](../../../src/docs/guides/build-a-pack.md); who uses it, the guide in the site's corner, in the
> framework's site's [guide](../../../src/docs/projects/louis-agent.identity.guide.md).

![The docs pack's three tools: ListDocs, SearchDocs and ReadDoc](diagrams/louis-agent.pack.docs.svg)

A guide that answers about a project from memory invents; one that reads the project's docs first answers from what
was written and can say where. This pack gives a model three read-only tools over one folder of markdown: the index,
a search, a doc. It never writes and never leaves the folder. Louis's pick for the expert's knowledge (F109.3, B1):
a tool over the docs, not an index built at build time and not a digest in the prompt, because the docs are small
enough to read and an answer can then name its doc.

## The tools

| Tool | Does |
|---|---|
| `ListDocs(folder)` | The folders and the docs directly in one folder, folders first, as paths from the root; empty for the top, where `README.md` is the index |
| `ReadDoc(path)` | One doc whole, markdown or plain text, up to 1 MB |
| `SearchDocs(query, maxResults)` | Every line holding the text, case ignored, as `path:line: text`, up to 100 |

A path that reaches outside the root, a file that isn't a doc (a picture, a film), a doc over 1 MB and a search
with no text are each refused with their reason, as `DocsToolMessage` says it. The same `WorkspaceBoundary` the
files pack uses keeps every path under the root.

## The setting

`DOCS_ROOT`: the folder the docs are under. Unset, the toolset reports it missing and stays off, so an instance
without docs runs as before. On the PC the demo's compose points it at the repo's docs; in the image it is the copy
the Dockerfile carries (`/app/docs`: `src/docs`, each demo's `docs/`, `packs/docs`, films left out).

## Tests

`packs/tests/louis-agent.pack.docs.tests`: the pack's name, guide and setting; the three tools over a small docs folder
made per test; every tool's shape as MCP needs it; the texts pinned to a golden.

---
[Packs' docs](../README.md) · Related: [general packs](general-packs.md)
