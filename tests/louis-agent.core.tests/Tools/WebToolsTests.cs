using System.Net;
using System.Text;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

public class WebToolsTests
{
    private const string DuckDuckGoHtml = """
        <div class="result results_links results_links_deep result--ad">
          <a rel="nofollow" class="result__a" href="https://ads.example.com/buy">Buy now</a>
          <a class="result__snippet" href="https://ads.example.com/buy">Sponsored</a>
        </div>
        <div class="result results_links results_links_deep web-result ">
          <a rel="nofollow" class="result__a" href="https://learn.microsoft.com/dotnet/whats-new">What&#x27;s new in .NET <b>10</b></a>
          <a class="result__snippet" href="https://learn.microsoft.com/dotnet/whats-new">New features for the <b>runtime</b> &amp; SDK.</a>
        </div>
        <div class="result results_links results_links_deep web-result ">
          <a rel="nofollow" class="result__a" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Fgithub.com%2Fdotnet%2Fcore&amp;rut=abc">dotnet/core</a>
          <a class="result__snippet" href="#">Release notes</a>
        </div>
        """;

    private const string ArticleHtml = """
        <html><head><title>Guide &amp; Notes</title><script>alert('x')</script></head>
        <body>
          <nav><a href="/home">Home</a></nav>
          <main>
            <h1>Install</h1>
            <p>Run the <code>dotnet</code> CLI. See <a href="/docs/setup">setup docs</a> or <a href="#top">top</a>.</p>
            <ul><li>First</li><li>Second</li></ul>
            <pre><code>dotnet new console
        dotnet run</code></pre>
            <style>.x{color:red}</style>
          </main>
          <footer>Copyright</footer>
        </body></html>
        """;

    private const string GoogleJson = """
        {"items":[
          {"title":"What's new in <b>.NET 10</b>","link":"https://learn.microsoft.com/a","snippet":"Runtime &amp; SDK"},
          {"title":"Second","link":"https://example.org/b","snippet":""}
        ]}
        """;

    private static Task<IPAddress[]> PublicDns(string host, CancellationToken _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") });

    private static WebTools Tools(Func<HttpRequestMessage, HttpResponseMessage> respond, IWebSearchProvider? search = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? dns = null) =>
        new(search ?? new FakeSearch(), new StubHandler(respond), dns ?? PublicDns);

    private static HttpClient Http(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(new StubHandler(respond));

    private static HttpResponseMessage Html(string html, string mediaType = "text/html") =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, mediaType) };

    /// <summary>An injected engine: records the query and returns canned results.</summary>
    private sealed class FakeSearch(params WebSearchResult[] results) : IWebSearchProvider
    {
        public WebSearchQuery? Last { get; private set; }
        public string Name => "FakeSearch";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
        {
            Last = query;
            return Task.FromResult<IReadOnlyList<WebSearchResult>>(results);
        }
    }

    // --- WebTools with an injected provider ---

    [Test]
    public async Task WebSearch_UsesInjectedProviderAndFormatsResults()
    {
        var fake = new FakeSearch(new("First", "https://a.example/1", "one"), new("Second", "https://a.example/2", ""), new("Third", "https://a.example/3", "three"));

        var result = await Tools(_ => Html(""), fake).WebSearch("dotnet 10", maxResults: 2, site: "learn.microsoft.com", freshness: "w");

        Assert.That(fake.Last, Is.EqualTo(new WebSearchQuery("dotnet 10", 2, "learn.microsoft.com", "week")));
        Assert.That(result, Does.StartWith("Search results for 'dotnet 10' on learn.microsoft.com (via FakeSearch):"));
        Assert.That(result, Does.Contain("1. First\n   https://a.example/1\n   one"));
        Assert.That(result, Does.Contain("2. Second\n   https://a.example/2"));
        Assert.That(result, Does.Not.Contain("Third"), "never more than maxResults, even if the provider returns more");
    }

    [Test]
    public async Task WebSearch_ReportsNoResultsAndProviderErrors()
    {
        Assert.That(await Tools(_ => Html("")).WebSearch("x"), Does.Contain("No results for 'x' (via FakeSearch)"));
        Assert.That(await Tools(_ => Html(""), new UnavailableSearchProvider("Google", "needs a key")).WebSearch("x"), Is.EqualTo("Error: needs a key"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task WebSearch_RejectsEmptyQuery(string query) =>
        Assert.That(await Tools(_ => Html("")).WebSearch(query), Does.Contain("cannot be empty"));

    [Test]
    public async Task WebSearch_RejectsUnknownFreshness() =>
        Assert.That(await Tools(_ => Html("")).WebSearch("x", freshness: "decade"), Does.Contain("day, week, month or year"));

    [Test]
    public async Task AgentEngine_UsesInjectedSearchProvider()
    {
        var fake = new FakeSearch(new WebSearchResult("Injected", "https://injected.example", ""));
        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(), TestPaths.Agent(), webSearch: fake);
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

    [Test]
    public void Factory_ExplainsMisconfiguration()
    {
        var missingKey = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "google" });
        var unknown = WebSearchProviderFactory.Create(new AgentOptions { WebSearchProvider = "bing" });

        Assert.That(() => missingKey.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("GOOGLE_SEARCH_API_KEY").And.Message.Contains("GOOGLE_SEARCH_ENGINE_ID"));
        Assert.That(() => unknown.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("Supported: google, duckduckgo"));
    }

    // --- Google ---

    [Test]
    public async Task Google_SendsKeyEngineIdAndFilters()
    {
        HttpRequestMessage? sent = null;
        var google = new GoogleSearchProvider("my-key", "my-cx", Http(r => { sent = r; return Html(GoogleJson, "application/json"); }));

        var results = await google.SearchAsync(new WebSearchQuery("dotnet 10", 1, "learn.microsoft.com", "month"));

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

        var results = await ddg.SearchAsync(new WebSearchQuery("dotnet 10", 1, "learn.microsoft.com", "week"));

        Assert.That(form, Does.Contain("q=dotnet+10+site%3Alearn.microsoft.com").And.Contain("df=w"));
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public void DuckDuckGo_ExplainsWhenItBlocksTheRequest()
    {
        var ddg = new DuckDuckGoSearchProvider(Http(_ => Html("<div class=\"anomaly-modal\">Unfortunately, bots use DuckDuckGo too.</div>")));
        Assert.That(() => ddg.SearchAsync(new WebSearchQuery("x", 1)),
            Throws.TypeOf<WebSearchException>().With.Message.Contains("WEB_SEARCH_PROVIDER=google"));
    }

    [Test]
    public void HtmlToText_KeepsMainContentAsMarkdown()
    {
        var (title, text) = WebTools.HtmlToText(ArticleHtml, new Uri("https://example.com/guide/page"));

        Assert.That(title, Is.EqualTo("Guide & Notes"));
        Assert.That(text, Does.Contain("# Install"));
        Assert.That(text, Does.Contain("Run the `dotnet` CLI. See [setup docs](https://example.com/docs/setup) or top."));
        Assert.That(text, Does.Contain("- First\n- Second"));
        Assert.That(text, Does.Contain("```\ndotnet new console\ndotnet run\n```"));
        Assert.That(text, Does.Not.Contain("alert").And.Not.Contain("color:red").And.Not.Contain("Home").And.Not.Contain("Copyright"));
    }

    [Test]
    public async Task FetchUrl_ReturnsTextMarkedAsUntrusted()
    {
        var result = await Tools(_ => Html(ArticleHtml)).FetchUrl("https://example.com/guide");

        Assert.That(result, Does.StartWith("URL: https://example.com/guide"));
        Assert.That(result, Does.Contain("Title: Guide & Notes"));
        Assert.That(result, Does.Contain("untrusted web content"));
        Assert.That(result, Does.Contain("# Install"));
    }

    [Test]
    public async Task FetchUrl_PagesLongContentWithStartIndex()
    {
        string longText = string.Concat(Enumerable.Range(0, 300).Select(i => $"line {i:D3}\n"));
        var tools = Tools(_ => Html(longText, "text/plain"));

        var first = await tools.FetchUrl("https://example.com/log.txt", maxChars: 500);
        var second = await tools.FetchUrl("https://example.com/log.txt", maxChars: 500, startIndex: 500);

        Assert.That(first, Does.Contain("line 000").And.Contain("startIndex=500"));
        Assert.That(second, Does.Not.Contain("line 000").And.Contain("Showing characters 500-1000"));
    }

    [TestCase("ftp://example.com/file")]
    [TestCase("file:///etc/passwd")]
    [TestCase("not a url")]
    public async Task FetchUrl_RejectsNonHttpUrls(string url) =>
        Assert.That(await Tools(_ => Html("")).FetchUrl(url), Does.Contain("absolute http"));

    [TestCase("http://localhost:8080/admin")]
    [TestCase("http://127.0.0.1/")]
    [TestCase("http://169.254.169.254/latest/meta-data/")]
    [TestCase("http://192.168.1.10/")]
    [TestCase("http://[::1]/")]
    public async Task FetchUrl_BlocksLocalAndPrivateAddresses(string url)
    {
        bool requested = false;
        var result = await Tools(_ => { requested = true; return Html("secret"); }).FetchUrl(url);

        Assert.That(result, Does.Contain("refusing"));
        Assert.That(requested, Is.False, "no request may be sent to a blocked address");
    }

    [Test]
    public async Task FetchUrl_BlocksHostnamesResolvingToPrivateAddresses()
    {
        var tools = Tools(_ => Html("secret"), dns: (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") }));
        Assert.That(await tools.FetchUrl("https://intranet.example.com/"), Does.Contain("private or local address"));
    }

    [Test]
    public async Task FetchUrl_BlocksRedirectToPrivateAddress()
    {
        var requests = new List<Uri>();
        var tools = Tools(request =>
        {
            requests.Add(request.RequestUri!);
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("http://127.0.0.1/admin");
            return redirect;
        });

        var result = await tools.FetchUrl("https://example.com/go");

        Assert.That(result, Does.Contain("refusing"));
        Assert.That(requests, Has.Count.EqualTo(1), "the redirect target must never be requested");
    }

    [Test]
    public async Task FetchUrl_FollowsPublicRedirects()
    {
        var tools = Tools(request => request.RequestUri!.AbsolutePath == "/old"
            ? new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Headers = { Location = new Uri("/new", UriKind.Relative) } }
            : Html("<main><p>moved here</p></main>"));

        var result = await tools.FetchUrl("https://example.com/old");

        Assert.That(result, Does.Contain("URL: https://example.com/new").And.Contain("moved here"));
    }

    [Test]
    public async Task FetchUrl_RejectsBinaryContent()
    {
        var tools = Tools(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new("image/png") } } });
        Assert.That(await tools.FetchUrl("https://example.com/a.png"), Does.Contain("cannot be read as text"));
    }

    [TestCase("8.8.8.8", true)]
    [TestCase("2606:4700:4700::1111", true)]
    [TestCase("10.1.2.3", false)]
    [TestCase("172.20.0.1", false)]
    [TestCase("192.168.0.1", false)]
    [TestCase("100.64.0.1", false)]
    [TestCase("169.254.169.254", false)]
    [TestCase("0.0.0.0", false)]
    [TestCase("224.0.0.1", false)]
    [TestCase("::1", false)]
    [TestCase("fe80::1", false)]
    [TestCase("fd00::1", false)]
    [TestCase("::ffff:127.0.0.1", false)]
    public void IsPublicAddress_ClassifiesRanges(string address, bool expected) =>
        Assert.That(WebTools.IsPublicAddress(IPAddress.Parse(address)), Is.EqualTo(expected));

    [Test]
    [Explicit("Hits the real internet")]
    public async Task Live_SearchAndFetch()
    {
        var tools = new WebTools(WebSearchProviderFactory.Create(AgentOptions.FromEnvironment()));

        var search = await tools.WebSearch("dotnet 10 what's new", maxResults: 3);
        var page = await tools.FetchUrl("https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview", maxChars: 1500);

        TestContext.Out.WriteLine(search);
        TestContext.Out.WriteLine(page);
        Assert.That(search, Does.Contain("1. "));
        Assert.That(page, Does.Contain(".NET 10"));
    }

    [Test]
    [Explicit("Hits the real network")]
    public async Task Live_ConnectTimeGuardBlocksPrivateTargets()
    {
        // Real handler: nip.io resolves 127.0.0.1.nip.io to 127.0.0.1.
        Assert.That(await new WebTools().FetchUrl("http://127.0.0.1.nip.io/"), Does.Contain("refusing"));
    }
}
