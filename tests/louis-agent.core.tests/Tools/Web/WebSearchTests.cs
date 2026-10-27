using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Pricing.Rates;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Search.Providers;

using static LouisAgent.Core.Tests.Tools.Web.FakeWeb;

namespace LouisAgent.Core.Tests.Tools.Web;

// WebSearch and the engines behind it (F5, F25, F32): an injected provider, the one picked from configuration, Tavily, Google and DuckDuckGo as stubs.
public class WebSearchTests
{
    // --- WebTools with an injected provider ---
    [Test]
    public async Task WebSearch_UsesInjectedProviderAndFormatsResults()
    {
        var fake = new FakeSearch(new("First", "https://a.example/1", "one"), new("Second", "https://a.example/2", ""), new("Third", "https://a.example/3", "three"));

        var result = await WebToolsOn(_ => Html(""), fake).WebSearch("dotnet 10", maxResults: 2, site: "learn.microsoft.com", freshness: "w");

        Assert.That(fake.Last, Is.EqualTo(new WebSearchQuery("dotnet 10", 2, "learn.microsoft.com", SearchRecency.Week)));
        Assert.That(result, Does.StartWith("Search results for 'dotnet 10' on learn.microsoft.com (via FakeSearch):"));
        Assert.That(result, Does.Contain("1. First\n   https://a.example/1\n   one"));
        Assert.That(result, Does.Contain("2. Second\n   https://a.example/2"));
        Assert.That(result, Does.Not.Contain("Third"), "never more than maxResults, even if the provider returns more");
    }

    [Test]
    public async Task WebSearch_ReportsNoResultsAndProviderErrors()
    {
        Assert.That(await WebToolsOn(_ => Html("")).WebSearch("x"), Does.Contain("No results for 'x' (via FakeSearch)"));
        Assert.That(await WebToolsOn(_ => Html(""), new UnavailableSearchProvider("Google", "needs a key")).WebSearch("x"), Is.EqualTo("Error: needs a key"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task WebSearch_RejectsEmptyQuery(string query) =>
        Assert.That(await WebToolsOn(_ => Html("")).WebSearch(query), Does.Contain("cannot be empty"));

    [Test]
    public async Task WebSearch_RejectsUnknownFreshness() =>
        Assert.That(await WebToolsOn(_ => Html("")).WebSearch("x", freshness: "decade"), Does.Contain("day, week, month or year"));

    [Test]
    public async Task WebToolset_UsesInjectedSearchProvider()
    {
        var fake = new FakeSearch(new WebSearchResult("Injected", "https://injected.example", ""));
        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(), TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [CoreToolset.Web.Name()], fake));
        var webSearch = engine.Tools.OfType<AIFunction>().Single(t => t.Name == "WebSearch");

        var result = await webSearch.InvokeAsync(new AIFunctionArguments { ["query"] = "anything" });

        Assert.That(result?.ToString(), Does.Contain("Injected").And.Contain("via FakeSearch"));
    }

    // --- Provider selection from configuration ---
    [TestCase(null, "key", "cx", typeof(GoogleSearchProvider))]
    [TestCase(null, null, null, typeof(DuckDuckGoSearchProvider))]
    [TestCase(null, "key", null, typeof(DuckDuckGoSearchProvider))]
    [TestCase("duckduckgo", "key", "cx", typeof(DuckDuckGoSearchProvider))]
    [TestCase("DDG", null, null, typeof(DuckDuckGoSearchProvider))]
    [TestCase("Google", "key", "cx", typeof(GoogleSearchProvider))]
    [TestCase("google", "key", null, typeof(UnavailableSearchProvider))]
    [TestCase("bing", null, null, typeof(UnavailableSearchProvider))]
    public void Factory_PicksProviderFromConfiguration(string? provider, string? key, string? cx, Type expected)
    {
        var options = new AgentOptions { WebSearchProvider = provider, GoogleSearchApiKey = key, GoogleSearchEngineId = cx };
        Assert.That(WebSearchProviderFactory.Create(options), Is.TypeOf(expected));
    }

    // F25 step 6 (Louis: "use tavily"): a Tavily key alone picks Tavily; with Google's keys too, Tavily wins (the
    // Custom Search API ends on 2027-01-01); an explicit choice still wins over any key.
    [TestCase(null, "tvly-key", null, typeof(TavilySearchProvider))]
    [TestCase(null, "tvly-key", "key", typeof(TavilySearchProvider))]
    [TestCase("tavily", "tvly-key", null, typeof(TavilySearchProvider))]
    [TestCase("Tavily", null, null, typeof(UnavailableSearchProvider))]
    [TestCase("google", "tvly-key", "key", typeof(GoogleSearchProvider))]
    [TestCase("duckduckgo", "tvly-key", null, typeof(DuckDuckGoSearchProvider))]
    public void Factory_PicksTavily(string? provider, string? tavilyKey, string? googleKey, Type expected)
    {
        var options = new AgentOptions
        {
            WebSearchProvider = provider, TavilyApiKey = tavilyKey,
            GoogleSearchApiKey = googleKey, GoogleSearchEngineId = googleKey is null ? null : "cx",
        };
        Assert.That(WebSearchProviderFactory.Create(options), Is.TypeOf(expected));
    }

    [Test]
    public void Factory_ExplainsAMissingTavilyKey()
    {
        var missingKey = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "tavily" });

        Assert.That(() => missingKey.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("TAVILY_API_KEY"));
        Assert.That(WebSearchProviderFactory.SupportedProviders, Does.Contain("tavily"));
    }

    [Test]
    public async Task Tavily_PostsTheQueryWithTheKey_Domains_AndTimeRange()
    {
        HttpRequestMessage? sent = null;
        string? body = null;
        var tavily = new TavilySearchProvider("tvly-key", Http(r =>
        {
            sent = r;
            body = r.Content!.ReadAsStringAsync().Result;
            return Html(TavilyJson, "application/json");
        }));

        var results = await tavily.SearchAsync(new WebSearchQuery("beef mince", 2, "checkers.co.za", SearchRecency.Week));

        Assert.That(sent!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(sent.RequestUri!.ToString(), Is.EqualTo("https://api.tavily.com/search"));
        Assert.That(sent.Headers.Authorization!.ToString(), Is.EqualTo("Bearer tvly-key"));
        using var json = JsonDocument.Parse(body!);
        Assert.That(json.RootElement.GetProperty("query").GetString(), Is.EqualTo("beef mince"));
        Assert.That(json.RootElement.GetProperty("max_results").GetInt32(), Is.EqualTo(2));
        Assert.That(json.RootElement.GetProperty("include_domains")[0].GetString(), Is.EqualTo("checkers.co.za"));
        Assert.That(json.RootElement.GetProperty("time_range").GetString(), Is.EqualTo("week"));
        Assert.That(body, Does.Not.Contain("tvly-key"), "the key travels in the header, never in the body");

        Assert.That(results.Select(r => r.Url), Is.EqualTo(new[] { "https://www.checkers.co.za/product/beef-mince-500g", "https://example.org/b" }), "a result without a url is dropped; max_results holds");
        Assert.That(results[0].Title, Is.EqualTo("Beef Mince 500g | Checkers"));
        Assert.That(results[0].Snippet, Is.EqualTo("Beef Mince 500g R64.99"));
    }

    [Test]
    public async Task Tavily_WithoutSiteOrRecency_SendsNeither()
    {
        string? body = null;
        var tavily = new TavilySearchProvider("k", Http(r => { body = r.Content!.ReadAsStringAsync().Result; return Html("""{"results":[]}""", "application/json"); }));

        Assert.That(await tavily.SearchAsync(new WebSearchQuery("x", 5)), Is.Empty);
        Assert.That(body, Does.Not.Contain("include_domains").And.Not.Contain("time_range"));
    }

    [TestCase(HttpStatusCode.Unauthorized, "Check TAVILY_API_KEY")]
    [TestCase(HttpStatusCode.Forbidden, "Check TAVILY_API_KEY")]
    [TestCase(HttpStatusCode.TooManyRequests, "limit")]
    [TestCase((HttpStatusCode)432, "limit")]
    [TestCase(HttpStatusCode.BadGateway, "HTTP 502")]
    public void Tavily_ExplainsApiErrorsWithTavilysMessage(HttpStatusCode status, string expected)
    {
        var tavily = new TavilySearchProvider("k", Http(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("""{"detail":{"error":"Unauthorized: missing or invalid API key."}}""", Encoding.UTF8, "application/json"),
        }));

        Assert.That(() => tavily.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains(expected).And.Message.Contains("missing or invalid API key"));
    }

    [Test]
    public void Factory_ExplainsMisconfiguration()
    {
        var missingKey = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "google" });
        var unknown = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "bing" });

        Assert.That(() => missingKey.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("GOOGLE_SEARCH_API_KEY").And.Message.Contains("GOOGLE_SEARCH_ENGINE_ID"));
        Assert.That(() => unknown.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("Supported: tavily, google, searxng, duckduckgo"));
    }

    // --- Google ---
    [Test]
    public async Task Google_SendsKeyEngineIdAndFilters()
    {
        HttpRequestMessage? sent = null;
        var google = new GoogleSearchProvider("my-key", "my-cx", Http(r => { sent = r; return Html(GoogleJson, "application/json"); }));

        var results = await google.SearchAsync(new WebSearchQuery("dotnet 10", 1, "learn.microsoft.com", SearchRecency.Month));

        string query = Uri.UnescapeDataString(sent!.RequestUri!.Query);
        Assert.That(sent.RequestUri.Host, Is.EqualTo("www.googleapis.com"));
        Assert.That(query, Does.Contain("key=my-key").And.Contain("cx=my-cx").And.Contain("q=dotnet 10").And.Contain("num=1")
            .And.Contain("siteSearch=learn.microsoft.com").And.Contain("siteSearchFilter=i").And.Contain("dateRestrict=m1"));
        Assert.That(results.Single(), Is.EqualTo(new WebSearchResult("What's new in .NET 10", "https://learn.microsoft.com/a", "Runtime & SDK")));
    }

    [Test]
    public async Task Google_NoItemsMeansNoResults()
    {
        var google = new GoogleSearchProvider("k", "cx", Http(_ => Html("""{"searchInformation":{"totalResults":"0"}}""", "application/json")));
        Assert.That(await google.SearchAsync(new WebSearchQuery("x", 5)), Is.Empty);
    }

    [TestCase(HttpStatusCode.Forbidden, "Check GOOGLE_SEARCH_API_KEY")]
    [TestCase(HttpStatusCode.BadRequest, "Check GOOGLE_SEARCH_API_KEY")]
    [TestCase(HttpStatusCode.TooManyRequests, "quota exceeded")]
    public void Google_ExplainsApiErrorsWithGooglesMessage(HttpStatusCode status, string expected)
    {
        var google = new GoogleSearchProvider("k", "cx", Http(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("""{"error":{"code":403,"message":"API key not valid."}}""", Encoding.UTF8, "application/json"),
        }));

        Assert.That(() => google.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains(expected).And.Message.Contains("API key not valid."));
    }

    // --- DuckDuckGo ---
    [Test]
    public void DuckDuckGo_ParseSkipsAdsDecodesEntitiesAndRedirectLinks()
    {
        var results = DuckDuckGoSearchProvider.ParseResults(DuckDuckGoHtml, 10);

        Assert.That(results.Select(r => r.Url), Is.EqualTo(new[] { "https://learn.microsoft.com/dotnet/whats-new", "https://github.com/dotnet/core" }));
        Assert.That(results[0].Title, Is.EqualTo("What's new in .NET 10"));
        Assert.That(results[0].Snippet, Is.EqualTo("New features for the runtime & SDK."));
    }

    [Test]
    public async Task DuckDuckGo_AppliesSiteAndFreshness()
    {
        string? form = null;
        var ddg = new DuckDuckGoSearchProvider(Http(r => { form = r.Content!.ReadAsStringAsync().Result; return Html(DuckDuckGoHtml); }));

        var results = await ddg.SearchAsync(new WebSearchQuery("dotnet 10", 1, "learn.microsoft.com", SearchRecency.Week));

        Assert.That(form, Does.Contain("q=dotnet+10+site%3Alearn.microsoft.com").And.Contain("df=w"));
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public void DuckDuckGo_ExplainsWhenItBlocksTheRequest()
    {
        var ddg = new DuckDuckGoSearchProvider(Http(_ => Html("<div class=\"anomaly-modal\">Unfortunately, bots use DuckDuckGo too.</div>")));
        Assert.That(() => ddg.SearchAsync(new WebSearchQuery("x", 1)),
            // F32 step 3: Google's Custom Search is closed to new customers; the advice is SearXNG for development
            // and a licensed API (Tavily) for the sold endpoint (ADR 0023).
            Throws.TypeOf<WebSearchException>().With.Message.Contains("WEB_SEARCH_PROVIDER=searxng")
                .And.Message.Contains("TAVILY_API_KEY").And.Message.Not.Contains("google"));
    }
}
