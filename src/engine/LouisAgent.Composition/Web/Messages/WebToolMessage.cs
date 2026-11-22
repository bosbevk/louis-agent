using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Composition.Web.Messages.Holes.Fetch;
using LouisAgent.Composition.Web.Messages.Holes.Google;
using LouisAgent.Composition.Web.Messages.Holes.Quotes;
using LouisAgent.Composition.Web.Messages.Holes.Redirects;
using LouisAgent.Composition.Web.Messages.Holes.Rendering;
using LouisAgent.Composition.Web.Messages.Holes.Replay;
using LouisAgent.Composition.Web.Messages.Holes.Robots;
using LouisAgent.Composition.Web.Messages.Holes.Search;
using LouisAgent.Composition.Web.Messages.Holes.Searxng;
using LouisAgent.Composition.Web.Messages.Holes.Tavily;

namespace LouisAgent.Composition.Web.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the web toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum WebToolMessage
{
    [Description("Error: query cannot be empty.")]
    SearchQueryEmpty,

    [Description("Error: freshness must be day, week, month or year.")]
    SearchFreshnessInvalid,

    [Description("Error: web search via {Provider} failed ({Reason}). Check the internet connection and try again.")]
    [Holes(typeof(SearchFailedHoles))]
    SearchFailed,

    [Description("Error: url must be an absolute http:// or https:// URL.")]
    FetchUrlInvalid,

    [Description("Error: {Url} returned HTTP {Status} {Reason}.")]
    [Holes(typeof(FetchHttpErrorHoles))]
    FetchHttpError,

    [Description("Error: {Url} is '{MediaType}', which cannot be read as text.")]
    [Holes(typeof(FetchNotTextHoles))]
    FetchNotText,

    [Description("Error: startIndex {StartIndex} is past the end of the content ({Length} characters).")]
    [Holes(typeof(FetchStartPastEndHoles))]
    FetchStartPastEnd,

    [Description("Error: the response is larger than {Megabytes} MB.")]
    [Holes(typeof(FetchTooLargeHoles))]
    FetchTooLarge,

    [Description("Error: could not fetch {Url} ({Reason}).")]
    [Holes(typeof(FetchFailedHoles))]
    FetchFailed,

    [Description("more than {Count} redirects")]
    [Holes(typeof(FetchTooManyRedirectsHoles))]
    FetchTooManyRedirects,

    [Description("redirect to unsupported scheme '{Scheme}'.")]
    [Holes(typeof(FetchRedirectSchemeHoles))]
    FetchRedirectScheme,

    [Description(
        "DuckDuckGo refused the automated search. For reliable search set TAVILY_API_KEY (a licensed search API), " +
        "or, for development and benchmarks, WEB_SEARCH_PROVIDER=searxng with SEARXNG_URL (the SearXNG we run).")]
    DuckDuckGoRefused,

    [Description(
        "WEB_SEARCH_PROVIDER=tavily needs TAVILY_API_KEY in config/.env.secrets (a key from https://app.tavily.com).")]
    TavilyNotConfigured,

    [Description("Tavily rejected the request ({Detail}). Check TAVILY_API_KEY.")]
    [Holes(typeof(TavilyRejectedHoles))]
    TavilyRejected,

    [Description(
        "Tavily's limit was reached ({Detail}): the plan's searches for the month, or too many at once. Try again " +
        "later, or raise the plan at https://app.tavily.com.")]
    [Holes(typeof(TavilyLimitHoles))]
    TavilyLimit,

    [Description("Tavily search failed: HTTP {Status} {Detail}")]
    [Holes(typeof(TavilyFailedHoles))]
    TavilyFailed,

    [Description(
        "ReadRenderedPage needs a renderer: BROWSER_URL (the headless Chrome we run) or TAVILY_API_KEY (Tavily " +
        "Extract). Without one, FetchUrl is the only way to read a page.")]
    RenderNotConfigured,

    [Description(
        "couldn't render {Url}: {Reason}. If FetchUrl read it as empty too, the site blocks readers: mark what it " +
        "holds as unknown and move on.")]
    [Holes(typeof(RenderFailedHoles))]
    RenderFailed,

    [Description("the renderer returned no content for it")]
    RenderNoResult,

    [Description("no answer from the browser within {Seconds} s; the page was closed")]
    [Holes(typeof(RenderTimedOutHoles))]
    RenderTimedOut,

    [Description("Unknown WEB_SEARCH_PROVIDER '{Provider}'. Supported: {Supported}.")]
    [Holes(typeof(UnknownSearchProviderHoles))]
    UnknownSearchProvider,

    [Description(
        "WEB_SEARCH_PROVIDER=searxng needs SEARXNG_URL: the address of the SearXNG we run (the compose file names " +
        "it).")]
    SearxngNotConfigured,

    [Description(
        "SearXNG at {Url} refused a JSON search (HTTP 403): its settings.yml needs json under search.formats.")]
    [Holes(typeof(SearxngJsonOffHoles))]
    SearxngJsonOff,

    [Description("No search engine behind SearXNG answered: {Reasons}. Try again later.")]
    [Holes(typeof(SearxngNoEngineAnsweredHoles))]
    SearxngNoEngineAnswered,

    [Description("SearXNG search failed: HTTP {Status} {Detail}")]
    [Holes(typeof(SearxngFailedHoles))]
    SearxngFailed,

    [Description(
        "PAGE_RENDERER=chrome needs BROWSER_URL: the headless Chrome's DevTools address (the compose file names it).")]
    BrowserNotConfigured,

    [Description("The browser at {Url} didn't answer ({Reason}). Is the browser container running?")]
    [Holes(typeof(BrowserUnreachableHoles))]
    BrowserUnreachable,

    [Description("The page at {Url} couldn't run the browser's script: {Reason}.")]
    [Holes(typeof(BrowserScriptFailedHoles))]
    BrowserScriptFailed,

    [Description(
        "{Url} was not read in the recorded run, so this replay can't open it: replays read only recorded pages. " +
        "Treat what it holds as unknown.")]
    [Holes(typeof(ReplayMissHoles))]
    ReplayMiss,

    [Description(
        "The search '{Query}' was not run in the recorded run, so this replay has no results for it: replays answer " +
        "only recorded searches. Treat what it would find as unknown.")]
    [Holes(typeof(ReplaySearchMissHoles))]
    ReplaySearchMiss,

    [Description(
        "The recorded run never read the clock, so this replay can't tell the time: treat today's date and the time " +
        "as unknown.")]
    ReplayClockMiss,

    [Description("Unknown PAGE_RENDERER '{Renderer}'. Supported: {Supported}.")]
    [Holes(typeof(UnknownPageRendererHoles))]
    UnknownPageRenderer,

    [Description("it went on to {Address}, which isn't a public address")]
    [Holes(typeof(RenderBlockedAddressHoles))]
    RenderBlockedAddress,

    [Description("Google search quota exceeded ({Detail}). Try again later or raise the quota in Google Cloud.")]
    [Holes(typeof(GoogleQuotaExceededHoles))]
    GoogleQuotaExceeded,

    [Description(
        "Google rejected the request ({Detail}). Check GOOGLE_SEARCH_API_KEY, GOOGLE_SEARCH_ENGINE_ID and that the " +
        "Custom Search API is enabled.")]
    [Holes(typeof(GoogleRejectedHoles))]
    GoogleRejected,

    [Description("Google search failed: HTTP {Status} {Detail}")]
    [Holes(typeof(GoogleFailedHoles))]
    GoogleFailed,

    [Description("No results for {Query} (via {Provider}). Try broader or different keywords.")]
    [Holes(typeof(NoSearchResultsHoles))]
    NoSearchResults,

    [Description("Search results for {Query} (via {Provider}):\n")]
    [Holes(typeof(SearchResultsHoles))]
    SearchResults,

    [Description("(no readable text)")] NoReadableText,

    [Description("\n[Showing characters {Start}-{End} of {Length}. Call {Tool} with startIndex={End} to continue.]")]
    [Holes(typeof(PageContinuesHoles))]
    PageContinues,

    [Description(
        "WEB_SEARCH_PROVIDER=google needs both GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID in " +
        "config/.env.secrets.")]
    GoogleNotConfigured,

    [Description("Error: give the exact words to look for: the quote is empty.")]
    QuoteEmpty,

    [Description("Found: the quote is on {Url}.")]
    [Holes(typeof(QuoteFoundHoles))]
    QuoteFound,

    [Description(
        "Not found: the quote isn't on {Url}. Treat the value as unconfirmed: ask once more with stronger: true, or " +
        "say it couldn't be confirmed.")]
    [Holes(typeof(QuoteNotFoundHoles))]
    QuoteNotFound,

    [Description(
        "Error: '{Quote}' is only a price, and a price alone matches any product at that price on the page. Call " +
        "again with the product argument: the product's name as the page shows it.")]
    [Holes(typeof(QuoteOnlyAPriceHoles))]
    QuoteOnlyAPrice,

    [Description(
        "Not found with the product: {Url} has the quote, but not right after '{Product}' (within {Window} " +
        "characters, no other price between), so it may be another product's. Treat the value as unconfirmed.")]
    [Holes(typeof(QuoteNotWithProductHoles))]
    QuoteNotWithProduct,

    [Description(
        "Page empty: {Url} has no readable text (a page drawn with JavaScript that couldn't be rendered), so the " +
        "quote can't be checked. Treat the value as unconfirmed.")]
    [Holes(typeof(QuotePageEmptyHoles))]
    QuotePageEmpty,

    [Description("{Host}'s robots.txt disallows reading {Path}; not read.")]
    [Holes(typeof(RobotsDisallowsHoles))]
    RobotsDisallows,

    [Description("{Host}'s robots.txt couldn't be read ({Reason}); its pages are not read for an hour.")]
    [Holes(typeof(RobotsUnreachableHoles))]
    RobotsUnreachable,

    [Description("{Host} says no to AI readers ({Signal}); not read.")]
    [Holes(typeof(PageRefusesAiReadersHoles))]
    PageRefusesAiReaders,

    [Description(
        "Error: web access isn't set up on this host: it runs core alone, without the web library. Nothing on the " +
        "web can be searched, fetched or read here.")]
    WebAccessNotSetUp,
}
