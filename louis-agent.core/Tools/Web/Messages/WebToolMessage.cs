
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Web.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the web toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum WebToolMessage
{
    [Description("Error: query cannot be empty.")]
    SearchQueryEmpty,

    [Description("Error: freshness must be day, week, month or year.")]
    SearchFreshnessInvalid,

    [Description("Error: web search via {Provider} failed ({Reason}). Check the internet connection and try again.")]
    SearchFailed,

    [Description("Error: url must be an absolute http:// or https:// URL.")]
    FetchUrlInvalid,

    [Description("Error: {Url} returned HTTP {Status} {Reason}.")]
    FetchHttpError,

    [Description("Error: {Url} is '{MediaType}', which cannot be read as text.")]
    FetchNotText,

    [Description("Error: startIndex {StartIndex} is past the end of the content ({Length} characters).")]
    FetchStartPastEnd,

    [Description("Error: the response is larger than {Megabytes} MB.")]
    FetchTooLarge,

    [Description("Error: could not fetch {Url} ({Reason}).")]
    FetchFailed,

    [Description("more than {Count} redirects")]
    FetchTooManyRedirects,

    [Description("redirect to unsupported scheme '{Scheme}'.")]
    FetchRedirectScheme,

    [Description(
        "DuckDuckGo refused the automated search. For reliable search set TAVILY_API_KEY (a licensed search API), or, for development and benchmarks, WEB_SEARCH_PROVIDER=searxng with SEARXNG_URL (the SearXNG we run).")]
    DuckDuckGoRefused,

    [Description(
        "WEB_SEARCH_PROVIDER=tavily needs TAVILY_API_KEY in config/.env.secrets (a key from https://app.tavily.com).")]
    TavilyNotConfigured,

    [Description("Tavily rejected the request ({Detail}). Check TAVILY_API_KEY.")]
    TavilyRejected,

    [Description(
        "Tavily's limit was reached ({Detail}): the plan's searches for the month, or too many at once. Try again later, or raise the plan at https://app.tavily.com.")]
    TavilyLimit,

    [Description("Tavily search failed: HTTP {Status} {Detail}")]
    TavilyFailed,

    [Description(
        "ReadRenderedPage needs a renderer: BROWSER_URL (the headless Chrome we run) or TAVILY_API_KEY (Tavily Extract). Without one, FetchUrl is the only way to read a page.")]
    RenderNotConfigured,

    [Description(
        "couldn't render {Url}: {Reason}. If FetchUrl read it as empty too, the site blocks readers: mark what it holds as unknown and move on.")]
    RenderFailed,

    [Description("the renderer returned no content for it")]
    RenderNoResult,

    [Description("no answer from the browser within {Seconds} s; the page was closed")]
    RenderTimedOut,

    [Description("Unknown WEB_SEARCH_PROVIDER '{Provider}'. Supported: {Supported}.")]
    UnknownSearchProvider,

    [Description(
        "WEB_SEARCH_PROVIDER=searxng needs SEARXNG_URL: the address of the SearXNG we run (the compose file names it).")]
    SearxngNotConfigured,

    [Description(
        "SearXNG at {Url} refused a JSON search (HTTP 403): its settings.yml needs json under search.formats.")]
    SearxngJsonOff,

    [Description("No search engine behind SearXNG answered: {Reasons}. Try again later.")]
    SearxngNoEngineAnswered,

    [Description("SearXNG search failed: HTTP {Status} {Detail}")]
    SearxngFailed,

    [Description(
        "PAGE_RENDERER=chrome needs BROWSER_URL: the headless Chrome's DevTools address (the compose file names it).")]
    BrowserNotConfigured,

    [Description("The browser at {Url} didn't answer ({Reason}). Is the browser container running?")]
    BrowserUnreachable,

    [Description("The page at {Url} couldn't run the browser's script: {Reason}.")]
    BrowserScriptFailed,

    [Description(
        "{Url} was not read in the recorded run, so this replay can't open it: replays read only recorded pages. Treat what it holds as unknown.")]
    ReplayMiss,

    [Description(
        "The search '{Query}' was not run in the recorded run, so this replay has no results for it: replays answer only recorded searches. Treat what it would find as unknown.")]
    ReplaySearchMiss,

    [Description(
        "The recorded run never read the clock, so this replay can't tell the time: treat today's date and the time as unknown.")]
    ReplayClockMiss,

    [Description("Unknown PAGE_RENDERER '{Renderer}'. Supported: {Supported}.")]
    UnknownPageRenderer,

    [Description("it went on to {Address}, which isn't a public address")]
    RenderBlockedAddress,

    [Description("Google search quota exceeded ({Detail}). Try again later or raise the quota in Google Cloud.")]
    GoogleQuotaExceeded,

    [Description(
        "Google rejected the request ({Detail}). Check GOOGLE_SEARCH_API_KEY, GOOGLE_SEARCH_ENGINE_ID and that the Custom Search API is enabled.")]
    GoogleRejected,

    [Description("Google search failed: HTTP {Status} {Detail}")]
    GoogleFailed,

    [Description("No results for {Query} (via {Provider}). Try broader or different keywords.")]
    NoSearchResults,

    [Description("Search results for {Query} (via {Provider}):\n")]
    SearchResults,

    [Description("(no readable text)")] NoReadableText,

    [Description("\n[Showing characters {Start}-{End} of {Length}. Call {Tool} with startIndex={End} to continue.]")]
    PageContinues,

    [Description(
        "WEB_SEARCH_PROVIDER=google needs both GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID in config/.env.secrets.")]
    GoogleNotConfigured,

    [Description("Error: give the exact words to look for: the quote is empty.")]
    QuoteEmpty,

    [Description("Found: the quote is on {Url}.")]
    QuoteFound,

    [Description(
        "Not found: the quote isn't on {Url}. Treat the value as unconfirmed: ask once more with stronger: true, or say it couldn't be confirmed.")]
    QuoteNotFound,

    [Description(
        "Error: '{Quote}' is only a price, and a price alone matches any product at that price on the page. Call again with the product argument: the product's name as the page shows it.")]
    QuoteOnlyAPrice,

    [Description(
        "Not found with the product: {Url} has the quote, but not right after '{Product}' (within {Window} characters, no other price between), so it may be another product's. Treat the value as unconfirmed.")]
    QuoteNotWithProduct,

    [Description(
        "Page empty: {Url} has no readable text (a page drawn with JavaScript that couldn't be rendered), so the quote can't be checked. Treat the value as unconfirmed.")]
    QuotePageEmpty,
}
