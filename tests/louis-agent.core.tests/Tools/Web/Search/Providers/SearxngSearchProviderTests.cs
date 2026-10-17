using System.Net;
using System.Text;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Search.Providers;

// F32 step 1: WebSearch through SearXNG, the search engine we run ourselves (for development, tests and benchmarks;
// the sold endpoint keeps a licensed API, ADR 0023). One GET with format=json; the engines SearXNG asked and couldn't
// reach come back as unresponsive_engines, so a search where none answered says why, engine by engine.
public class SearxngSearchProviderTests
{
    private static readonly Uri Base = new("http://puls-searxng:8080");

    private const string AnswerJson = """
        {"query":"beef mince","number_of_results":0,"results":[
          {"url":"https://www.checkers.co.za/product/beef-mince-500g","title":"Beef Mince 500g | Checkers","content":"Beef Mince 500g <b>R64.99</b>","engine":"bing","engines":["bing","brave"]},
          {"url":"https://www.pnp.co.za/beef-mince","title":"PnP Beef Mince","content":"","engine":"brave"},
          {"title":"No url","content":"x"},
          {"url":"https://example.org/third","title":"Third","content":"three"}
        ],"answers":[],"suggestions":[],"unresponsive_engines":[["google","CAPTCHA"],["duckduckgo","timeout"]]}
        """;

    private static HttpClient Http(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(new StubHandler(respond));

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    // --- The request ---

    [Test]
    public void TheRequest_IsOneGet_WithTheSiteInTheQuery_JsonAndTheTimeRange()
    {
        var uri = SearxngSearchProvider.RequestUri(Base, new WebSearchQuery("beef mince", 5, "checkers.co.za", SearchRecency.Month));

        Assert.That(uri.GetLeftPart(UriPartial.Path), Is.EqualTo("http://puls-searxng:8080/search"));
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.That(query["q"], Is.EqualTo("beef mince site:checkers.co.za"));
        Assert.That(query["format"], Is.EqualTo("json"));
        Assert.That(query["time_range"], Is.EqualTo("month"));
    }

    [Test]
    public void WithoutSiteOrRecency_TheQueryIsTheText_AndNoTimeRange()
    {
        var uri = SearxngSearchProvider.RequestUri(Base, new WebSearchQuery("prime lending rate", 5));

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.That(query["q"], Is.EqualTo("prime lending rate"));
        Assert.That(query["time_range"], Is.Null);
    }

    [Test]
    public void ASearxngUnderAPath_IsSearchedThere() =>
        Assert.That(SearxngSearchProvider.RequestUri(new Uri("http://host:8888/searxng/"), new WebSearchQuery("x", 1)).GetLeftPart(UriPartial.Path),
            Is.EqualTo("http://host:8888/searxng/search"));

    // --- The answer ---

    [Test]
    public void TheResults_ComeBackCleaned_UpToMaxResults_WithoutTheOnesWithNoUrl()
    {
        var answer = SearxngSearchProvider.Parse(AnswerJson, 2);

        Assert.That(answer.Results.Select(r => r.Url), Is.EqualTo(new[]
        {
            "https://www.checkers.co.za/product/beef-mince-500g", "https://www.pnp.co.za/beef-mince",
        }));
        Assert.That(answer.Results[0], Is.EqualTo(new WebSearchResult("Beef Mince 500g | Checkers",
            "https://www.checkers.co.za/product/beef-mince-500g", "Beef Mince 500g R64.99")));
    }

    [Test]
    public void TheEnginesThatDidntAnswer_AreNamed_WithTheirReason() =>
        Assert.That(SearxngSearchProvider.Parse(AnswerJson, 5).SilentEngines, Is.EqualTo(new[] { "google: CAPTCHA", "duckduckgo: timeout" }));

    [Test]
    public void WhenNoEngineAnswered_TheSearchFails_NamingEachEngineAndWhy()
    {
        const string none = """{"query":"x","results":[],"unresponsive_engines":[["google","CAPTCHA"],["bing","Suspended: too many requests"]]}""";

        Assert.That(() => SearxngSearchProvider.Parse(none, 5), Throws.TypeOf<WebSearchException>()
            .With.Message.Contains("google: CAPTCHA").And.Message.Contains("bing: Suspended: too many requests"));
    }

    [Test]
    public void NoResults_WithEveryEngineAnswering_IsAnEmptySearch_NotAFailure()
    {
        var answer = SearxngSearchProvider.Parse("""{"query":"x","results":[],"unresponsive_engines":[]}""", 5);

        Assert.That((answer.Results, answer.SilentEngines), Is.EqualTo((Array.Empty<WebSearchResult>(), Array.Empty<string>())));
    }

    // --- Over HTTP ---

    [Test]
    public async Task ASearch_GetsTheRequest_AndReturnsTheResults()
    {
        var handler = new StubHandler(_ => Json(AnswerJson));
        var searxng = new SearxngSearchProvider(Base, new HttpClient(handler));

        var results = await searxng.SearchAsync(new WebSearchQuery("beef mince", 3, "checkers.co.za"));

        Assert.That(searxng.Name, Is.EqualTo("SearXNG"));
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(handler.Requests.Single().Url, Does.StartWith("http://puls-searxng:8080/search?"));
    }

    [Test]
    public void A403_SaysJsonIsOffInSearxngsSettings()
    {
        // SearXNG answers 403 to format=json unless its settings.yml lists json under search.formats.
        var searxng = new SearxngSearchProvider(Base, Http(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));

        Assert.That(() => searxng.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("settings.yml").And.Message.Contains("json"));
    }

    [Test]
    public void AnotherHttpError_SaysItsStatus()
    {
        var searxng = new SearxngSearchProvider(Base, Http(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { ReasonPhrase = "Bad Gateway" }));

        Assert.That(() => searxng.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("502"));
    }

    // --- Against the SearXNG we run (F32 step 4): SEARXNG_URL set, as the Puls stack's puls-searxng on 127.0.0.1:8888 ---

    [Test, Category("Live")]
    public async Task TheSearxngWeRun_FindsAShopsProduct()
    {
        string? url = Environment.GetEnvironmentVariable("SEARXNG_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "SEARXNG_URL isn't set: no SearXNG to search (the Puls stack's puls-searxng).");

        var results = await new SearxngSearchProvider(new Uri(url!)).SearchAsync(new WebSearchQuery("beef mince price", 5, "pnp.co.za"));

        Assert.That(results, Is.Not.Empty, "settings.yml lists json, and at least one engine answered");
        Assert.That(results.Select(r => r.Url), Has.Some.Contains("pnp.co.za"));
    }

    // --- Chosen by configuration ---

    [TestCase("searxng", "http://puls-searxng:8080", null, typeof(SearxngSearchProvider))]
    [TestCase("SearXNG", "http://puls-searxng:8080", "tvly-key", typeof(SearxngSearchProvider))]
    [TestCase("searxng", null, null, typeof(UnavailableSearchProvider))]
    [TestCase(null, "http://puls-searxng:8080", null, typeof(SearxngSearchProvider))]
    [TestCase(null, "http://puls-searxng:8080", "tvly-key", typeof(TavilySearchProvider))]
    [TestCase(null, null, null, typeof(DuckDuckGoSearchProvider))]
    public void TheFactory_PicksSearxng_NamedOrWhenItsTheFirstConfigured(string? provider, string? url, string? tavilyKey, Type expected)
    {
        // Unset: Tavily (key), then Google (key and cx), then SearXNG (its URL), then DuckDuckGo. A Tavily key keeps
        // the sold endpoint on the licensed API even where SearXNG runs too.
        var options = new AgentOptions { WebSearchProvider = provider, SearxngUrl = url, TavilyApiKey = tavilyKey };

        Assert.That(WebSearchProviderFactory.Create(options), Is.TypeOf(expected));
    }

    [Test]
    public void Google_StillComesBeforeSearxng_WhenItsKeysAreSet() =>
        Assert.That(WebSearchProviderFactory.Create(new AgentOptions
        {
            SearxngUrl = "http://puls-searxng:8080", GoogleSearchApiKey = "key", GoogleSearchEngineId = "cx",
        }), Is.TypeOf<GoogleSearchProvider>());

    [Test]
    public void NamedWithoutItsUrl_TheFirstSearchSaysWhatToSet()
    {
        var missing = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "searxng" });

        Assert.That(() => missing.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("SEARXNG_URL"));
        Assert.That(WebSearchProviderFactory.SupportedProviders, Does.Contain("searxng"));
    }
}
