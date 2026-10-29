using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Rendering.Setups;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering.Setups;

/// <summary>Checks each renderer setup on its own, and that the factory lists one per renderer in fallback order.</summary>
public class PageRendererSetupTests
{
    /// <summary>The setup for one renderer.</summary>
    private static IPageRendererSetup SetupFor(PageRendererKind kind) =>
        PageRendererFactory.Setups.Single(s => s.Kind == kind);

    /// <summary>Each renderer is ready with its own setting and not without it. A blank key or a bad address counts as missing.</summary>
    [TestCase(PageRendererKind.Chrome, "http://puls-browser:9222", null, true)]
    [TestCase(PageRendererKind.Chrome, "not a url", null, false)]
    [TestCase(PageRendererKind.Chrome, null, "tvly", false)]
    [TestCase(PageRendererKind.Tavily, null, "tvly", true)]
    [TestCase(PageRendererKind.Tavily, null, "  ", false)]
    [TestCase(PageRendererKind.Tavily, "http://puls-browser:9222", null, false)]
    public void ASetup_IsReady_WithItsSetting_AndNotWithout(PageRendererKind kind, string? browserUrl,
        string? tavilyKey, bool expected)
    {
        var options = new AgentOptions { BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(SetupFor(kind).IsReady(options), Is.EqualTo(expected));
    }

    /// <summary>If a renderer isn't set up you get the "not set up" one, and only then.</summary>
    [Test]
    public void Create_GivesTheUnavailableRenderer_ExactlyWhenTheSetupIsNotReady()
    {
        var everything = new AgentOptions { BrowserUrl = "http://puls-browser:9222", TavilyApiKey = "tvly" };

        // Every setup, once with nothing set and once with everything set.
        foreach (var setup in PageRendererFactory.Setups)
            foreach (var options in new[] { new AgentOptions(), everything })
                Assert.That(setup.Create(options, null, null) is UnavailablePageRenderer,
                    Is.EqualTo(!setup.IsReady(options)), $"{setup.Kind}, ready: {setup.IsReady(options)}");
    }

    /// <summary>Each renderer has exactly one setup, so you can't add a renderer and forget its setup.</summary>
    [Test]
    public void EveryRenderer_HasExactlyOneSetup() =>
        Assert.That(
            Enum.GetValues<PageRendererKind>().Select(kind => PageRendererFactory.Setups.Count(s => s.Kind == kind)),
            Is.All.EqualTo(1));

    /// <summary>The order is the fallback order: Chrome, then Tavily.</summary>
    [Test]
    public void TheSetups_AreInFallbackOrder() =>
        Assert.That(PageRendererFactory.Setups.Select(s => s.Kind),
            Is.EqualTo(new[] { PageRendererKind.Chrome, PageRendererKind.Tavily }));
}
