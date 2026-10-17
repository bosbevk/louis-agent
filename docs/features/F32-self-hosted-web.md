# F32 · Self-hosted web: search through SearXNG, pages rendered by a headless Chrome

> **Status:** built (2026-10-17), on `feature/F32-self-hosted-web` (PR #56); open: the cook check's baseline run and a
> Puls chat that searched and rendered with `TAVILY_API_KEY` unset (both paid, Louis's go) · **Milestone:** — · **Depends on:** F25 (the
> renderer seam, `ReadRenderedPage`), FX16 (`VerifyQuote`'s fallback to the renderer), F31 (the cook check that
> measures it)
>
> **In the TODO:** **F32 Self-hosted web** under *Tooling and repositories* ([TODO](../../TODO.md)) · **Plan:**
> [plans/F32](../plans/F32-self-hosted-web.md)

Puls's web tools lean on Tavily twice: `WebSearch` and `ReadRenderedPage` (Tavily Extract, F25 step 7), which also backs
`VerifyQuote` when a shop draws its price with JavaScript. On 2026-10-17 Tavily's free credits ran out (they come back
on the 1st of the month) and both stopped: F31's first paid run measured a cook that couldn't search. DuckDuckGo, the
keyless fallback, let one search through and refused the rest; Google's Custom Search is closed to new customers and
shuts on 2027-01-01. Louis's question: can Puls search and read any page without a key or credits?

**SearXNG is for development, tests and benchmarks, not the sold endpoint** (Louis, 2026-10-17, "we sell our
endpoint"): it scrapes Google, Bing and others, whose terms forbid automated querying, more so in a paid product. The
sold Puls keeps a search API that licenses its results (Tavily, or Brave as a second provider); with a Tavily key set,
Tavily is still picked first. The headless Chrome renderer serves both.

## User stories

**F32-S1 — Search without a key, for development.** As an *operator*, I want `WebSearch` to work through a search
engine we run ourselves, so a provider's limit never stops development, the tests or a benchmark.
- *Given* `WEB_SEARCH_PROVIDER=searxng` and `SEARXNG_URL` *when* the model searches *then* the results come from the
  engines SearXNG asks (Bing, Brave, DuckDuckGo, Google, Mojeek, Startpage, Wikipedia), with `site` and `freshness`
  applied.
- *Given* some engines refuse *then* the results come from the others and the log names the silent ones; *given*
  every engine refuses *then* the search fails with each engine's reason, so the explorer's tools ring says why.
- *Given* no provider set *then* the order is Tavily (key), Google (key and engine), SearXNG (URL), DuckDuckGo.

**F32-S2 — Any page read as a browser shows it.** As a *user*, I want a shop's product page read with its price, from
whichever search found it, so a price comes with its source.
- *Given* `BROWSER_URL` *when* `ReadRenderedPage` or `VerifyQuote` needs a page drawn with JavaScript *then* a headless
  Chrome renders it and the text comes back the way `FetchUrl` gives it.
- *Given* a page that redirects to, or loads anything from, a private or internal address *then* that request is
  refused, as `FetchUrl` refuses it.
- *Given* `PAGE_RENDERER=tavily` *then* Tavily Extract renders, as today.

**F32-S3 — Measured.** As *Louis*, I want the cook check run on the self-hosted web, so its numbers are a baseline:
priced with sources, and what it cost.

## Design notes

- **Behind the seams we have.** `SearxngSearchProvider` is one more `IWebSearchProvider`; `ChromePageRenderer` one more
  `IPageRenderer`. The model's tools, their descriptions and the guards stay ours; only what's behind them changes.
- **The address rule moves into the browser.** A browser follows redirects and loads subresources on its own, so the
  renderer checks every request the page makes with `SsrfGuard`, not only the first URL.
- **Two containers in the Puls stack,** `puls-searxng` and `puls-browser`, on their own network with `puls` and
  `puls-agent`, never with `puls-mongo`; their image tags pinned so benchmark runs compare.
- **Licences and terms:** SearXNG is AGPL-3.0, run unchanged as a separate service over HTTP and never linked, and
  not used by the sold endpoint, since the engines it queries don't license their results for that
  ([ADR 0023](../adr/0023-copyleft-service-beside-us.md)); PuppeteerSharp is MIT; Chromium's headless shell is
  BSD-style.
- **MCP, considered:** Tavily's MCP server spends the same credits; a SearXNG or browser MCP server adds a container and,
  for the browser, loses the per-request check. MCP servers for the model are their own feature (a TODO line).

## Done when

- A Puls chat searches through SearXNG and reads a shop's JavaScript product page through the headless Chrome, with
  `TAVILY_API_KEY` unset.
- The provider and the renderer are tested with stubs; the address rule inside the browser is tested on its own; the
  live tests run against the containers and skip without them.
- The texts the model reads say no more than is true (no "Tavily", no "a credit"); both golden files reviewed.
- The cook check run from the portal on the self-hosted web, its numbers here: the baseline F31's run couldn't be.

---
[Features](README.md) · [Plan](../plans/F32-self-hosted-web.md) · Previous: [F31 Benchmarks in the admin portal](F31-benchmarks-portal.md)

## What the build changed

- **Step 1 (2026-10-17): SearXNG behind `IWebSearchProvider`.** One GET with `format=json`; the engines it couldn't
  reach come back named with their reason, so a search where none answered fails saying which and why. Unset, it comes
  after Tavily and Google and before DuckDuckGo, so a Tavily key keeps the sold endpoint on the licensed API.
- **Step 1b: choices are enums, and the web toolset by role** (Louis). `WEB_SEARCH_PROVIDER` and the search's freshness
  became enums parsed once with `Config/SettingChoice`; the rule joined *Rules that always apply* with ADR 0024. The
  web toolset moved into `Search/` and `Rendering/`, each with its implementations one level down.
- **Step 2: the headless Chrome renderer.** On Pick n Pay's search page `FetchUrl` read **0 characters**; the renderer
  read **4,345, with 37 rand prices**. Every request the page makes goes through `RenderPolicy` (the same `SsrfGuard` as
  `FetchUrl`): a redirect to `169.254.169.254` is refused inside the browser, the error naming it. **One gap the
  interception can't close:** Chrome's request interception doesn't see WebSocket connections, so the browser's own
  network (step 4) is the layer that keeps a page from opening one to an internal address. Chrome answers its
  DevTools address only by IP, so the renderer resolves `puls-browser` first.
- **Step 3: the texts the model reads say no more than is true.** `ReadRenderedPage`'s description names no renderer
  and no credit (it says it's slower than `FetchUrl`, and that the address rule holds for every request the page
  makes); DuckDuckGo's refusal and the web guide advise a licensed API (Tavily) or, for development, SearXNG, not
  Google; a missing renderer names both settings. **Left as it is, to measure:** the cook's prompt says Pick n Pay's and
  Woolworths' pages give nothing even rendered, so it prices them from search results. That was Tavily Extract's
  result; the Chrome renderer read 37 prices off Pick n Pay's search page. Whether to let the cook render them is a
  change to how it works, for the baseline run to inform, not a wording fix.
- **Step 4: the containers, and a gap the interception left.** `puls-searxng` and `puls-browser` run in the Puls stack
  on a `web` network with `puls` and `puls-agent`, never with `puls-mongo`, both pinned by digest; SearXNG reads only
  its own secret (`config/.env.searxng`, the pattern of the review of PR #55). **WebSockets:** request interception
  doesn't see them, and neither does Chrome's URL blocking (`Network.setBlockedURLs`, tried first: the live test still
  opened a socket); so before a page's own scripts, in every frame, `WebSocket` refuses to be made, and the live test
  now reads `socket-blocked`. What's left is a Web Worker's socket: a blind GET to `puls` or `puls-agent`, whose GET
  endpoints change nothing, and no route to the database. **Engines:** SearXNG's defaults switch Bing, Google and Qwant
  off, so the settings switch them on; Mojeek and Startpage, planned, are inactive in this version (they ask for a
  proof-of-work CAPTCHA). From Louis's IP on the day, **only Bing answered** (Google and Qwant *access denied*, Brave
  *too many requests*, DuckDuckGo *CAPTCHA*), each named with its reason: enough for 9 results with Pick n Pay's pages,
  but one engine is thin, so a benchmark on it says which provider it searched with.
- **Review of PR #56.** *Must:* a host that doesn't exist (a dead tracker on a shop's page) made the real lookup throw,
  and the exception escaped the browser's `async void` request handler, which would end the process and every open
  chat with it; the tests' stub resolver returned `[]`, which the real one never does. `RenderPolicy` now refuses when
  the lookup fails for any reason, and the handler catches everything (answering a request can throw too, when the
  page closes under it), both tested first with a resolver that throws. *Should:* a cancelled turn held one of the four
  browser pages for up to 40 s, since PuppeteerSharp's waits take no token: the turn's token now reaches the renderer
  (`ReadRenderedPage` and `VerifyQuote` take it; Microsoft.Extensions.AI fills it and keeps it out of the schema, so the
  golden files didn't move), and cancelling closes the page, which ends the wait (live test: under 5 s, from 12). A
  frame a script makes (`about:blank`) was suspected of escaping the WebSocket block; a live test shows it doesn't.
  **Still open, by design for now:** *DNS rebinding.* `RenderPolicy` resolves a name and decides; Chrome then resolves
  it again on its own, so a hostile name could answer public to the check and internal to Chrome, and reach `puls` or
  `puls-agent` on the `web` network (no key unless `AGENT_API_KEY` is set: set it where the stack is reachable by
  anyone else). `FetchUrl` doesn't have the gap: it checks the address it connects to. With the Worker's socket, this is
  the second thing one layer would close: an egress proxy for the browser that resolves once, refuses internal
  addresses and connects to the address it checked (a TODO line).
