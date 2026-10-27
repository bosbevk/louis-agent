using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Tools.Web.Search;

// F18 step 7a.1: a run records which engine the instance searched with, so Resolve is the one place that says which. Unset, the
// first whose settings are set wins (Tavily, Google, SearXNG), else keyless DuckDuckGo.
public class WebSearchProviderFactoryTests
{
    [TestCase(null, "tvly", null, null, WebSearchProviderKind.Tavily)]
    [TestCase(null, null, "g", "cx", WebSearchProviderKind.Google)]
    [TestCase(null, null, "g", null, WebSearchProviderKind.DuckDuckGo)]
    [TestCase(null, null, null, null, WebSearchProviderKind.DuckDuckGo)]
    [TestCase("Google", "tvly", null, null, WebSearchProviderKind.Google)]
    [TestCase("ddg", "tvly", null, null, WebSearchProviderKind.DuckDuckGo)]
    [TestCase("searxng", null, null, null, WebSearchProviderKind.Searxng)]
    public void TheEngine_IsTheOneNamed_ElseTheFirstWhoseSettingsAreSet(string? named, string? tavilyKey, string? googleKey, string? engineId, WebSearchProviderKind expected)
    {
        var options = new AgentOptions { WebSearchProvider = named, TavilyApiKey = tavilyKey, GoogleSearchApiKey = googleKey, GoogleSearchEngineId = engineId };

        Assert.That(WebSearchProviderFactory.Resolve(options), Is.EqualTo(expected));
    }

    [Test]
    public void SearxngUrl_IsTheFallback_WhenNoKeyIsSet() =>
        Assert.That(WebSearchProviderFactory.Resolve(new AgentOptions { SearxngUrl = "http://searxng:8080" }), Is.EqualTo(WebSearchProviderKind.Searxng));

    [Test]
    public void AnUnknownName_ResolvesToNothing() =>
        Assert.That(WebSearchProviderFactory.Resolve(new AgentOptions { WebSearchProvider = "bing" }), Is.Null);
}
