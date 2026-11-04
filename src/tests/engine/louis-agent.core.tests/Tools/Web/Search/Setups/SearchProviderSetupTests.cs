using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Search.Providers;
using LouisAgent.Core.Tools.Web.Search.Setups;

namespace LouisAgent.Core.Tests.Tools.Web.Search.Setups;

/// <summary>Checks each search setup on its own, and that the factory lists one per engine in fallback order.</summary>
public class SearchProviderSetupTests
{
    /// <summary>The setup for one engine.</summary>
    private static ISearchProviderSetup SetupFor(WebSearchProviderKind kind) =>
        WebSearchProviderFactory.Setups.Single(s => s.Kind == kind);

    /// <summary>Settings for every engine, so they're all ready.</summary>
    private static AgentOptions Everything() => new()
    {
        TavilyApiKey = "tvly",
        GoogleSearchApiKey = "g",
        GoogleSearchEngineId = "cx",
        SearxngUrl = "http://searxng:8080",
    };

    /// <summary>Each engine is ready with its own settings and not without them. A blank key, half of Google's pair or a bad address counts as missing.</summary>
    [TestCase(WebSearchProviderKind.Tavily, "tvly", null, null, null, true)]
    [TestCase(WebSearchProviderKind.Tavily, "  ", null, null, null, false)]
    [TestCase(WebSearchProviderKind.Tavily, null, null, null, null, false)]
    [TestCase(WebSearchProviderKind.Google, null, "g", "cx", null, true)]
    [TestCase(WebSearchProviderKind.Google, null, "g", null, null, false)]
    [TestCase(WebSearchProviderKind.Google, null, null, "cx", null, false)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, "http://searxng:8080", true)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, "not a url", false)]
    [TestCase(WebSearchProviderKind.Searxng, null, null, null, null, false)]
    public void ASetup_IsReady_WithItsSettings_AndNotWithout(WebSearchProviderKind kind, string? tavilyKey,
        string? googleKey, string? engineId, string? searxngUrl, bool expected)
    {
        var options = new AgentOptions
        {
            TavilyApiKey = tavilyKey,
            GoogleSearchApiKey = googleKey,
            GoogleSearchEngineId = engineId,
            SearxngUrl = searxngUrl
        };

        Assert.That(SetupFor(kind).IsReady(options), Is.EqualTo(expected));
    }

    /// <summary>DuckDuckGo needs nothing, so it's ready with no settings at all.</summary>
    [Test]
    public void DuckDuckGo_IsAlwaysReady() =>
        Assert.That(SetupFor(WebSearchProviderKind.DuckDuckGo).IsReady(new AgentOptions()), Is.True);

    /// <summary>If an engine isn't set up you get the "not set up" provider, and only then.</summary>
    [Test]
    public void Create_GivesTheUnavailableProvider_ExactlyWhenTheSetupIsNotReady()
    {
        // Every setup, once with nothing set and once with everything set.
        foreach (var setup in WebSearchProviderFactory.Setups)
            foreach (var options in new[] { new AgentOptions(), Everything() })
                Assert.That(setup.Create(options, null) is UnavailableSearchProvider, Is.EqualTo(!setup.IsReady(options)),
                    $"{setup.Kind}, ready: {setup.IsReady(options)}");
    }

    /// <summary>Each engine has exactly one setup, so you can't add an engine and forget its setup.</summary>
    [Test]
    public void EveryEngine_HasExactlyOneSetup() =>
        Assert.That(
            Enum.GetValues<WebSearchProviderKind>()
                .Select(kind => WebSearchProviderFactory.Setups.Count(s => s.Kind == kind)), Is.All.EqualTo(1));

    /// <summary>The order is the fallback order: Tavily, Google, SearXNG, then DuckDuckGo.</summary>
    [Test]
    public void TheSetups_AreInFallbackOrder() =>
        Assert.That(WebSearchProviderFactory.Setups.Select(s => s.Kind),
            Is.EqualTo(new[]
            {
                WebSearchProviderKind.Tavily, WebSearchProviderKind.Google, WebSearchProviderKind.Searxng,
                WebSearchProviderKind.DuckDuckGo
            }));
}
