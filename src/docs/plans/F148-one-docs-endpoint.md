# F148: One docs endpoint, and every reader goes through it

**Status:** draft, written 2026-11-13 (thread "Plan a docs API endpoint", Louis: "can we plan a docs api end point
all docs must be served from that endpoint. this is next"). **Branch:** `feature/F148-one-docs-endpoint`.
Three choices are open and go to Louis one at a time, three options each (*Open choices*); the steps, the four
diagrams and the namespace map are settled after his picks, before step 1.

## Context

**What happened.** On louis-agent.dev a visitor asked the guide for the README, then "is this the github readme".
The guide answered: "I can't say for certain that it's the one on GitHub. It's the README.md at the root of the docs
I can read."

**Why it couldn't say.** It *was* the GitHub README: the site's image copies `README.md`, `ROADMAP.md`, `TODO.md`,
`src/docs/`, both demos' docs and `packs/docs/` to `/app/docs` when it is built (`src/site/docker/Dockerfile`), and
the docs pack reads that folder (`DOCS_ROOT`). But nothing tells the guide where the folder came from or which commit
it is, so the honest answer was "I don't know". The copy is also only as fresh as the last deploy.

**What exists.**

- `louis-agent.pack.docs` (F109.3): `ListDocs`, `ReadDoc`, `SearchDocs`, straight on the file system under
  `DOCS_ROOT`. A doc is a bare path and its text: no address, no version.
- No HTTP route serves a doc. The API serves what packs publish as *feeds* (`IDataFeed`, `/v1/feeds/{name}`, open,
  ADR 0070) and the MCP host serves those as `feed://` resources (F56, ADR 0072).
- F125.1 (approved, not built) replaces the three tools with `docs_search` and `docs_fetch` and adds `doc://`
  resources; F125.2 gives the docs a public MCP address. Neither says where the docs come from.

**What F148 adds.** One place that holds the docs (a *library*), one HTTP endpoint that serves all of them, and
every other reader (the guide's tools, the MCP resources, later the website's own pages) reading the same library.
Every doc it hands out carries its **path, its commit and its address on GitHub**, so "is this the GitHub README"
has an answer: "yes, `README.md` at commit `527ab4e`, here is the link".

## Design

**The shape, in plain words** (written for the recommended options; it changes with Louis's picks).

- **A docs library.** One class that knows every doc: its path, title, text, the commit it was read at and its
  GitHub address. The docs pack owns it; today's three tools read through it instead of the file system.
- **The endpoint.** `GET /v1/docs` lists every doc; `GET /v1/docs/{path}` returns one (its text plus where it is
  from); `GET /v1/docs/search?q=` finds lines. Open, like the feeds: the docs are public on GitHub already.
  Under `/v1`, nouns, typed answers (ADR 0039, 0045).
- **The version stamp.** The image's build writes the commit beside the docs it copies; the library reads it once.
  On the PC, where the docs are the working folder, the stamp says "local, not committed".
- **The guide says where a doc is from.** Each tool result ends with the doc's source line, and the docs toolset's
  guide tells the model to name it when asked.
- **MCP.** Each doc is also a `doc://{path}` resource from the same library, the way F56 serves feeds. This is the
  resource half of F125.1, moved here; F125.1 keeps the search (`docs_search`, BM25) and builds on the library.

**The patterns:** *Single source of truth*; *Repository* (Fowler, PoEAA: one object that stands for the collection
of docs, whatever holds them); *Plugin* (Fowler: the contract names the shape, the pack plugs it in, so the API never
references the pack, ADR 0027), as `IDataFeed` already does.

### Open choices (asked one at a time)

1. **Where the docs come from.**
   - **A. The image's copy, stamped with its commit** (*Immutable artifact*: build once, the artifact says what it
     is). What exists today plus one small file. Offline, fast, the docs always match the code that is running; as
     old as the last deploy, and says so. *Recommended.*
   - **B. GitHub, read live and cached** (*Gateway* with *Cache-Aside*, Fowler and Microsoft's cloud patterns).
     Always what GitHub shows; needs the network, a token or the anonymous rate limit (60 an hour), a cache and its
     expiry, and the docs can describe code newer than what is deployed.
   - **C. The image's copy, refreshed from GitHub in the background.** Both, and both costs: two sources to keep
     equal, the most code.
2. **How the guide's own tools reach the docs.** "All docs served from that endpoint", read three ways:
   - **A. One library, many doors** (*Repository*): the endpoint, the tools and the MCP resources all read the same
     library in process. One source, no network hop inside one program. *Recommended.*
   - **B. The tools call `/v1/docs` over HTTP** (*Remote Proxy*): literally everything through the endpoint; the
     guide could then read another instance's docs. A hop, a timeout and a failure mode for a call to itself;
     F125.8's `TOOL_TRANSPORT` gives this later for every tool at once.
   - **C. The tools read `doc://` over MCP.** Waits for F125.8's client.
3. **The endpoint's shape.**
   - **A. A `docs` feed**: plug an `IDataFeed` into the pack; `/v1/feeds/docs` and `feed://docs` come for free.
     Smallest. But a feed is one JSON blob with flat ids: no path with slashes without bending the contract, no
     search, no plain markdown.
   - **B. Its own routes, `/v1/docs`, behind a small contract** (`IDocLibrary` beside `IDataFeed`): list, one doc by
     path, search; JSON by default, the raw markdown on `Accept: text/markdown`; an `ETag` from the commit. A new
     contract and a route file. *Recommended.*
   - **C. Its own docs host** (a separate small program at its own address, the one F125.2 would make public). The
     cleanest outside door; a second container to deploy and pay for now, before anything needs it.

### Steps (proposed, for the recommended options; settled after the picks)

1. **The library.** `DocLibrary` in the docs pack: every doc with path and title; the three tools read through it.
   *Test:* the tools' answers are unchanged (golden files hold).
2. **The stamp.** `DocSource` (commit, repository address); the Dockerfile writes it; a doc's GitHub address is built
   from it. *Test:* a stamped root gives `…/blob/{commit}/README.md`; no stamp gives "local".
3. **The contract and the endpoint.** `IDocLibrary` in composition, the pack plugs it in, `/v1/docs` and
   `/v1/docs/{path}` open. *Test:* the list holds every doc; one doc carries its source; a path outside the root is
   404; an instance without the pack answers an empty list.
4. **Search on the endpoint.** `/v1/docs/search?q=`. *Test:* the same hits as `SearchDocs`.
5. **The guide names its source.** The source line on each tool result, the toolset guide's rule. *Test:* the result
   ends with the line; golden diff reviewed.
6. **`doc://` resources.** `DocResources` in the MCP host, beside `FeedResources`. *Test:* a resource per doc, read
   through both transports' one registration (`McpPrimitives`).
7. **Docs and done.** ADR 0123 (one library, every door reads it), the pack's and the API's design docs, the
   write-up, F125's plan amended (its `doc://` half is here), the TODO row gone, this plan deleted.

## Verification

- The docs pack's, the API's and the MCP host's tests per step; the golden files at steps 1 and 5; the full suite
  once after step 6.
- Free manual check: locally, `curl /v1/docs/README.md`, then ask the guide "is this the github readme" and get the
  commit and the link.
- **Paid:** the guide's eval before and after step 5 (a few cents), only on Louis's go.

## Concepts

- **Grounding and provenance** (GOAL.md skill 7, Context engineering): what goes into the context carries where it
  is from, so the model can cite it instead of guessing. The first stone of retrieval (skill 8).
- **Tool design** (skill 4): a tool result that answers the follow-up question ("from where?") before it is asked.
- **Protocols** (skill 11): one source behind three doors, HTTP, tools and MCP resources.

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Related: [F125](F125-an-agentic-site.md) · [ADR 0070](../adr/0070-packs-publish-data-feeds.md) · [ADR 0072](../adr/0072-feeds-are-mcp-resources.md) · [ADR 0039](../adr/0039-api-routes-under-v1.md)
