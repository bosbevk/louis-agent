using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;

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

    /// <summary>
    /// F18 step 7a.1 follow-up: <c>IsReady</c> says whether the engine the options select can work. Named and set up is ready; named without its setting is not; unset is the
    /// fallback's answer (DuckDuckGo, always ready); an unknown name is not. And <c>Create</c> hands back the Null Object (the unavailable provider) exactly when it is not ready.
    /// </summary>
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
            WebSearchProvider = named, TavilyApiKey = tavilyKey, GoogleSearchApiKey = googleKey, GoogleSearchEngineId = engineId, SearxngUrl = searxngUrl,
        };

        Assert.That(WebSearchProviderFactory.IsReady(options), Is.EqualTo(expected));
        Assert.That(WebSearchProviderFactory.Create(options) is UnavailableSearchProvider, Is.EqualTo(!expected));
    }

    [Test]
    public void AnUnknownName_ResolvesToNothing() =>
        Assert.That(WebSearchProviderFactory.Resolve(new AgentOptions { WebSearchProvider = "bing" }), Is.Null);
}
