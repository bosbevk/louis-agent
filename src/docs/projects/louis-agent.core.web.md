# louis-agent.core.web — design

> **Kind:** library (`net10.0`) · **Path:** `src/engine/louis-agent.core.web/` · **References:** louis-agent.core,
> louis-agent.composition, louis-agent.choices, PuppeteerSharp · **Used by:** every host that gives its agent the web:
> the API, the CLI, the ACP and MCP hosts, the demos' programs; the eval runner through the API's settings
> **Also shows:** `component`

![Web access, and its parts: the robots policy, the browser, search and rendering, and the recorders](diagrams/louis-agent.core.web.svg)

## Purpose

This project is how the agent reads the web. It holds the real implementations of the composition's web contracts,
and core's engine takes them through its `IWebAccess` seam.

A host that wants its agent to read pages hands `AgentHost.Build` a `HostServices` with `WebAccess.Create`. A host
that doesn't ships no browser at all. Its web tools then just say web access isn't set up, because core falls back
to `NoWebAccess`, its Null Object (a stand-in that answers "not here" instead of failing).

*Decided in:* CL33, [ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md).

## How it works

Here's one fetch, from start to finish.

![One fetch: a recorded answer in a replay; live, robots.txt first, the SSRF guard, the fetch or visit, and the answer written down](diagrams/louis-agent.core.web-flow.svg)

`WebAccess.Create(options, replay)` builds everything once per process. There are six parts, and each one builds on
the ones before it.

### 1. The robots.txt policy

Every site can say what it doesn't want read, in its `robots.txt` file. There's one policy for the whole process,
`Robots/RobotsPolicy`, and every fetcher and the browser obey it. It reads each site's file through a fetcher of its
own, one that has no policy, so it doesn't ask itself for permission.

*Decided in:* F35, [ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md).

### 2. The guarded browser

Some pages only show up once JavaScript has run, so there's one real browser: `ChromeWebBrowser`, a headless Chrome
at `BROWSER_URL`, in `Browsing/`. It uses the user agent the settings say, so it's honest about what it is.

It's always wrapped by `RobotsWebBrowser`, so it obeys the policy too. Where a replay store is kept, it's also
wrapped by the recorder. If there's no usable address, there's no browser, and a toolset that needs one is then
unavailable.

`RenderPolicy` decides what a visit is allowed to do. `LostContexts` names the errors Chrome's library loses by
design, so the crash handler is told to expect them.

*Decided in:* F33, [ADR 0038](../adr/0038-the-browser-says-what-it-is.md).

### 3. Search

The search provider, in `Search/`, is whichever one the settings name, picked by `WebSearchProviderFactory`:
DuckDuckGo, Google, SearXNG or Tavily. Each comes with a setup that says "not set up" when its keys are missing.
Where a store is kept, it's wrapped by `RecordedSearchProvider`. And a host can bring its own provider instead,
through `Create(options, replay, search)`.

### 4. Rendering a page

The page renderer, in `Rendering/`, is the one the settings pick through `PageRendererFactory`: Chrome, through the
browser, or Tavily. A new one is made for each ask.

### 5. A fetcher per call

Each call gets its own fetcher, `Fetching/PageFetcher`. It works under the robots policy and the replay store, and
behind the SSRF guard that core keeps (the check that stops the agent reaching private addresses).

### 6. Recording and replaying

The recorders, in `Replay/`, are `RecordedFetchHandler`, `RecordedSearchProvider` and `RecordedWebBrowser`. They
write what the web answered into core's replay store, and in a replay they read it back instead. That way an eval's
run costs nothing to repeat. The replay clock stays in core, beside the store.

*Decided in:* F18.

### What a pack sees

None of this changes what a pack gets. Its `IPackContext` (that's `ToolsetContext`) answers `WebSearch`, `Browser`,
`Renderer` and `NewFetcher()`, all from the seam.

![Web access: from a host's program to IWebAccess and the parts behind it](diagrams/louis-agent.core.web-diagrams.svg)

![Fetching a page: a pack's tool gets a fetcher through the seam, and the fetcher asks the robots policy before it reads](diagrams/louis-agent.core.web-diagrams-2.svg)

These are the types involved:

![The types: WebAccess is core's web seam, holding a search, a renderer and a browser that factories pick, each behind its interface](diagrams/louis-agent.core.web-interface.svg)

## Structure

```
src/engine/louis-agent.core.web/
├── WebAccess.cs          # Create: the host's entry, core's IWebAccess made real
├── Browsing/             # ChromeWebBrowser, RobotsWebBrowser, WebBrowserFactory, LostContexts, RedirectBodies; Policy/ (RenderPolicy, RenderVerdict)
├── Fetching/             # PageFetcher, RobotsFileFetch
├── Messages/             # WebLogMessage (ADR 0009)
├── Rendering/            # PageRendererFactory, PageRendererKind; Providers/ (Chrome, Tavily), Setups/ (one per renderer)
├── Replay/               # RecordedFetchHandler, RecordedSearchProvider, RecordedWebBrowser; Answers/ (ReplayPage, ReplayResponse)
├── Robots/               # RobotsPolicy, RobotsFile, RobotsVerdict; Rules/ (the robots.txt reader), Signals/ (a page's no-AI signals)
└── Search/               # WebSearchProviderFactory, WebSearchProviderKind; Providers/ (DuckDuckGo, Google, SearXNG, Tavily), Setups/
src/tests/engine/louis-agent.core.web.tests/   # the same folders, plus WebAccessTests; the robots fixtures under Robots/Fixtures/
```

The namespaces are `LouisAgent.Core.Web.*`: the folder path under the library's root namespace.

## Configuration

The browser takes `BROWSER_URL` and `BROWSER_USER_AGENT`. Search takes `WEB_SEARCH_PROVIDER`, plus each
provider's keys. The renderer takes `PAGE_RENDERER`, and the replay store lives in `REPLAY_DIRECTORY`. The full list
is in each demo's `config/.env.example`.

## Tests

The tests are in `src/tests/engine/louis-agent.core.web.tests`, and they need no network. They cover the browser's
policy and lost contexts, the fetcher and the robots file fetch, and the robots rules and signals, on real files.
The rendering and search setups and providers run on the stub handler. The recorders run on the shared replay
fixtures in `louis-agent.testing/Replay`.

`WebAccessTests` checks the whole: the browser `BROWSER_URL` names comes under the policy, there's no browser
without an address, and a host's own search provider is recorded only where a store is kept.

## Limits and plans

The hand-built shop readers and recipe sources stay in their packs, not here. A new search provider or renderer is
one setup class here. The replay clock is core's, and the recorders are here.

`RedirectBodies` is a workaround. CL35, on the TODO, removes it once PuppeteerSharp awaits its own redirect bodies.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.telemetry](louis-agent.core.telemetry.md) ·
[ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md) · [ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md) ·
[ADR 0038](../adr/0038-the-browser-says-what-it-is.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
