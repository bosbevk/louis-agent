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
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tests.Tools.Web;

/// <summary>A web that is a stub (no network): pages, search answers, a public DNS, and the tools on them. Shared by the web tests (S, CL15).</summary>
internal static class FakeWeb
{
    public const string DuckDuckGoHtml = """
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

    public const string ArticleHtml = """
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

    public const string GoogleJson = """
        {"items":[
          {"title":"What's new in <b>.NET 10</b>","link":"https://learn.microsoft.com/a","snippet":"Runtime &amp; SDK"},
          {"title":"Second","link":"https://example.org/b","snippet":""}
        ]}
        """;

    public static Task<IPAddress[]> PublicDns(string host, CancellationToken _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") });

    public static WebTools WebToolsOn(Func<HttpRequestMessage, HttpResponseMessage> respond, IWebSearchProvider? search = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? dns = null, IPageRenderer? renderer = null) =>
        new(search ?? new FakeSearch(), new PageFetcher(new StubHandler(respond), dns ?? PublicDns), renderer);

    public const string ExtractJson = """
        {"results":[{"url":"https://www.pnp.co.za/pnp-large-eggs-6-pack/p/118676_EA","raw_content":"# PnP Large Eggs 6 Pack\n\nR34.99\n\nAdd to cart"}],
         "failed_results":[],"response_time":1.2}
        """;

    public static HttpClient Http(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(new StubHandler(respond));

    public static HttpResponseMessage Html(string html, string mediaType = "text/html") =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, mediaType) };

    /// <summary>An injected engine: records the query and returns canned results.</summary>
    public sealed class FakeSearch(params WebSearchResult[] results) : IWebSearchProvider
    {
        public WebSearchQuery? Last { get; private set; }
        public string Name => "FakeSearch";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
        {
            Last = query;
            return Task.FromResult<IReadOnlyList<WebSearchResult>>(results);
        }
    }

    public const string TavilyJson = """
        {"query":"beef mince","results":[
          {"title":"Beef Mince 500g | Checkers","url":"https://www.checkers.co.za/product/beef-mince-500g","content":"Beef Mince 500g  R64.99","score":0.9},
          {"title":"Second","url":"https://example.org/b","content":""},
          {"title":"No url","content":"x"}
        ],"response_time":0.4}
        """;
}
