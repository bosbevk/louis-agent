# louis-agent.core.web — design

> **Kind:** Class library (`net10.0`) · **Path:** `src/engine/louis-agent.core.web/` · **References:** louis-agent.core,
> louis-agent.composition, louis-agent.choices, PuppeteerSharp · **Used by:** every host that gives its agent the web:
> the API, the CLI, the ACP and MCP hosts, the demos' programs; the eval runner through the API's settings

![Web access, and its parts: the robots policy, the browser, search and rendering, and the recorders](diagrams/louis-agent.core.web.svg)

## Purpose

Web access on core (CL33, [ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md)): the
implementations of the composition's web contracts, which core's engine takes through its `IWebAccess` seam. A
host that wants its agent to read pages hands `AgentHost.Build` a `HostServices` with `WebAccess.Create`; a host
that doesn't ships no browser binary, and its web tools say web access isn't set up (`NoWebAccess`, core's Null
Object).

## How it works

`WebAccess.Create(options, replay)` builds, once per process, what `AgentEngineFactory` built itself before:

- **One robots.txt policy** (`Robots/RobotsPolicy`, F35, [ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md)),
  reading the file through a fetcher of its own that has no policy; every fetcher and the browser obey it.
- **One guarded browser** (`Browsing/`, F33): `ChromeWebBrowser` on headless Chrome at `BROWSER_URL`, with the user
  agent the settings say ([ADR 0038](../adr/0038-the-browser-says-what-it-is.md)), wrapped by the recorder where a
  replay store is kept and by `RobotsWebBrowser` always; none without a usable address, and a toolset that needs one
  is then unavailable. `RenderPolicy` decides what a visit may do; `LostContexts` names the errors Chrome's library
  loses by design, which the crash handler is told to expect.
- **The search provider** (`Search/`): the one the settings name through `WebSearchProviderFactory` (DuckDuckGo,
  Google, SearXNG, Tavily, each a setup that is "not set up" without its keys), wrapped by `RecordedSearchProvider`
  where a store is kept; a host's own provider takes its place through `Create(options, replay, search)`.
- **The page renderer** (`Rendering/`): the one the settings pick through `PageRendererFactory` (Chrome through the
  browser, or Tavily), made for each ask as before.
- **A fetcher per call** (`Fetching/PageFetcher`), under the policy and the store, with the SSRF guard core keeps.
- **The recorders** (`Replay/`): `RecordedFetchHandler`, `RecordedSearchProvider` and `RecordedWebBrowser` write
  what the web answered into core's replay store and read it back in a replay, so an eval's run costs nothing to
  repeat (F18); the replay clock stayed in core beside the store.

What a pack gets is unchanged: its `IPackContext` (`ToolsetContext`) answers `WebSearch`, `Browser`, `Renderer` and
`NewFetcher()` from the seam.

## Diagrams

![Web access: from a host's program to IWebAccess and the parts behind it](diagrams/louis-agent.core.web-diagrams.svg)

![Fetching a page: a pack's tool gets a fetcher through the seam, and the fetcher asks the robots policy before it reads](diagrams/louis-agent.core.web-diagrams-2.svg)

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

The namespaces are `LouisAgent.Core.Web.*`, the folder path under the library's root namespace, so a type kept its
name in the move.

## Configuration

What the settings names: `BROWSER_URL` and `BROWSER_USER_AGENT` (the browser), `WEB_SEARCH_PROVIDER` with each
provider's keys, `PAGE_RENDERER`, `REPLAY_DIRECTORY` (the store); the list is each demo's `config/.env.example`.

## Tests

`src/tests/engine/louis-agent.core.web.tests`: the browser's policy and lost contexts, the fetcher and the robots file
fetch, the robots rules and signals on real files, rendering and search setups and providers on the stub handler,
the recorders on the shared replay fixtures (`louis-agent.testing/Replay`), and `WebAccessTests` (the browser
BROWSER_URL names under the policy, none without an address, a host's own search provider recorded only where a
store is kept). No network.

## Limits and plans

The hand-built shop readers and recipe sources stay in their packs; a new provider or renderer is one setup class
here. The replay clock is core's; the recorders are here. CL35 (the TODO) removes `RedirectBodies` once
PuppeteerSharp awaits its own redirect bodies.

## Related docs

[louis-agent.core](louis-agent.core.md) · [louis-agent.core.telemetry](louis-agent.core.telemetry.md) ·
[ADR 0091](../adr/0091-web-access-and-telemetry-are-libraries-on-core.md) · [ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md) ·
[ADR 0038](../adr/0038-the-browser-says-what-it-is.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
