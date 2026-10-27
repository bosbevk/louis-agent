using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;
using LouisAgent.Core.Tools.Web.Search.Setups;

namespace LouisAgent.Core.Tests.Tools.Web.Search.Setups;

/// <summary>
/// F18 step 7a.1 follow-up (rule 11): Strategy, one setup per search engine that knows whether its settings are there and how to build its provider, and Null Object, the
/// unavailable provider a setup hands back when they are not. These tests hold each setup to that, and the factory's list to one setup per engine in fallback order.
/// </summary>
public class SearchProviderSetupTests
{
    /// <summary>The setup for an engine, taken from the factory's list.</summary>
    private static ISearchProviderSetup SetupFor(WebSearchProviderKind kind) => WebSearchProviderFactory.Setups.Single(s => s.Kind == kind);

    /// <summary>Every setting an engine could need, so all the engines are ready.</summary>
    private static AgentOptions Everything() => new()
    {
        TavilyApiKey = "tvly", GoogleSearchApiKey = "g", GoogleSearchEngineId = "cx", SearxngUrl = "http://searxng:8080",
    };

    /// <summary>Each engine is ready with its own settings and not without them (a blank key, half of Google's pair and an address that is no URL all count as missing).</summary>
    [TestCase(WebSearchProviderKind.Tavily, "tvly", null, null, null, true)]
    [TestCase(WebSearchProviderKind.Tavily, "  ", null, null, null, false)]
    [TestCase(WebSearchProviderKind.Tavily, null, null, null, null, false)]
    [TestCase(WebSearchProviderKind.Google, null, "g", "cx", null, true)]
    [TestCase(WebSearchProviderKind.Google, null, "g", null, null, false)]
    [TestCase(WebSearchProviderKind.Google, null, null, "cx", null, false)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, "http://searxng:8080", true)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, "not a url", false)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, null, false)]
    public void ASetup_IsReady_WithItsSettings_AndNotWithout(WebSearchProviderKind kind, string? tavilyKey, string? googleKey, string? engineId, string? searxngUrl, bool expected)
    {
        var options = new AgentOptions { TavilyApiKey = tavilyKey, GoogleSearchApiKey = googleKey, GoogleSearchEngineId = engineId, SearxngUrl = searxngUrl };

        Assert.That(SetupFor(kind).IsReady(options), Is.EqualTo(expected));
    }

    /// <summary>DuckDuckGo needs nothing, so it is ready with no settings at all.</summary>
    [Test]
    public void DuckDuckGo_IsAlwaysReady() =>
        Assert.That(SetupFor(WebSearchProviderKind.DuckDuckGo).IsReady(new AgentOptions()), Is.True);

    /// <summary>Null Object: whichever settings are given, a setup's provider is the unavailable one exactly when the setup is not ready.</summary>
    [Test]
    public void Create_GivesTheUnavailableProvider_ExactlyWhenTheSetupIsNotReady()
    {
        foreach (var setup in WebSearchProviderFactory.Setups)
            foreach (var options in new[] { new AgentOptions(), Everything() })
                Assert.That(setup.Create(options, null) is UnavailableSearchProvider, Is.EqualTo(!setup.IsReady(options)), $"{setup.Kind}, ready: {setup.IsReady(options)}");
    }

    /// <summary>The list holds exactly one setup per engine the enum names, so a new engine cannot be added without its setup.</summary>
    [Test]
    public void EveryEngine_HasExactlyOneSetup() =>
        Assert.That(Enum.GetValues<WebSearchProviderKind>().Select(kind => WebSearchProviderFactory.Setups.Count(s => s.Kind == kind)), Is.All.EqualTo(1));

    /// <summary>The list is in fallback order: with no engine named, the first ready one wins, so Tavily comes before Google before SearXNG before DuckDuckGo.</summary>
    [Test]
    public void TheSetups_AreInFallbackOrder() =>
        Assert.That(WebSearchProviderFactory.Setups.Select(s => s.Kind),
            Is.EqualTo(new[] { WebSearchProviderKind.Tavily, WebSearchProviderKind.Google, WebSearchProviderKind.Searxng, WebSearchProviderKind.DuckDuckGo }));
}
