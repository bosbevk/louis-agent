using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tests.Tools.Web.Search;

/// <summary>Which search engine the settings pick: the one named, or else the first that's set up, ending at keyless DuckDuckGo.</summary>
public class WebSearchProviderFactoryTests
{
    /// <summary>The engine follows the settings: the one named, otherwise the first that's set up.</summary>
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

    /// <summary>With no key set but a SearXNG address, SearXNG is the one.</summary>
    [Test]
    public void SearxngUrl_IsTheFallback_WhenNoKeyIsSet() =>
        Assert.That(WebSearchProviderFactory.Resolve(new AgentOptions { SearxngUrl = "http://searxng:8080" }), Is.EqualTo(WebSearchProviderKind.Searxng));

    /// <summary>Says whether the engine you'd get is set up, and `Create` agrees: you get the "not set up" provider exactly when this says no.</summary>
    [TestCase("tavily", "tvly", null, null, null, true)]
    [TestCase("tavily", null, null, null, null, false)]
    [TestCase("google", null, "g", "cx", null, true)]
    [TestCase("google", null, "g", null, null, false)]
    [TestCase("google", null, null, "cx", null, false)]
    [TestCase("searxng", null, null, null, "http://searxng:8080", true)]
    [TestCase("searxng", null, null, null, null, false)]
    [TestCase("searxng", null, null, null, "not a url", false)]
    [TestCase("duckduckgo", null, null, null, null, true)]
    [TestCase("ddg", null, null, null, null, true)]
    [TestCase(null, null, null, null, null, true)]
    [TestCase(null, "tvly", null, null, null, true)]
    [TestCase(null, null, null, null, "http://searxng:8080", true)]
    [TestCase("bing", "tvly", null, null, null, false)]
    public void IsReady_SaysWhetherTheSelectedEngineCanWork_AndCreateAgrees(string? named, string? tavilyKey, string? googleKey, string? engineId, string? searxngUrl, bool expected)
    {
        var options = new AgentOptions
        {
            WebSearchProvider = named,
            TavilyApiKey = tavilyKey,
            GoogleSearchApiKey = googleKey,
            GoogleSearchEngineId = engineId,
            SearxngUrl = searxngUrl,
        };

        Assert.That(WebSearchProviderFactory.IsReady(options), Is.EqualTo(expected));

        // Create agrees: the "not set up" provider comes back exactly when it isn't ready.
        Assert.That(WebSearchProviderFactory.Create(options) is UnavailableSearchProvider, Is.EqualTo(!expected));
    }

    /// <summary>An engine name we do not know picks nothing.</summary>
    [Test]
    public void AnUnknownName_ResolvesToNothing() =>
        Assert.That(WebSearchProviderFactory.Resolve(new AgentOptions { WebSearchProvider = "bing" }), Is.Null);
}
