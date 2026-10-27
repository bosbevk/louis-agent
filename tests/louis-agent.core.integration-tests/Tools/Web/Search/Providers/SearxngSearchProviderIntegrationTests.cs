using LouisAgent.Testing;
using System.Net;
using System.Text;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.IntegrationTests.Tools.Web.Search.Providers;

// F32 step 1: WebSearch through SearXNG, the search engine we run ourselves (for development, tests and benchmarks;
// the sold endpoint keeps a licensed API, ADR 0023). One GET with format=json; the engines SearXNG asked and couldn't
// reach come back as unresponsive_engines, so a search where none answered says why, engine by engine.
[Category("Integration")]
public class SearxngSearchProviderIntegrationTests
{
    // --- Against the SearXNG we run (F32 step 4): SEARXNG_URL set, as the Puls stack's puls-searxng on 127.0.0.1:8888 ---

    [Test, Category("Live")]
    public async Task TheSearxngWeRun_FindsAShopsProduct()
    {
        string url = Outside.Stack("SEARXNG_URL", "SearXNG to search");

        var results = await new SearxngSearchProvider(new Uri(url!)).SearchAsync(new WebSearchQuery("beef mince price", 5, "pnp.co.za"));

        Assert.That(results, Is.Not.Empty, "settings.yml lists json, and at least one engine answered");
        Assert.That(results.Select(r => r.Url), Has.Some.Contains("pnp.co.za"));
    }
}
