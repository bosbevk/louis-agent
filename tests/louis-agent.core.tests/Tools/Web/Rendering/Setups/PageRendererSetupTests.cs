using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Rendering.Setups;

namespace LouisAgent.Core.Tests.Tools.Web.Rendering.Setups;

/// <summary>
/// F18 step 7a.1 follow-up (rule 11): Strategy, one setup per page renderer that knows whether its settings are there and how to build it, and Null Object, the
/// unavailable renderer a setup hands back when they are not. These tests hold each setup to that, and the factory's list to one setup per renderer in fallback order.
/// </summary>
public class PageRendererSetupTests
{
    /// <summary>The setup for a renderer, taken from the factory's list.</summary>
    private static IPageRendererSetup SetupFor(PageRendererKind kind) => PageRendererFactory.Setups.Single(s => s.Kind == kind);

    /// <summary>Each renderer is ready with its own setting and not without it (a blank key and an address that is no URL count as missing).</summary>
    [TestCase(PageRendererKind.Chrome, "http://puls-browser:9222", null, true)]
    [TestCase(PageRendererKind.Chrome, "not a url", null, false)]
    [TestCase(PageRendererKind.Chrome, null, "tvly", false)]
    [TestCase(PageRendererKind.Tavily, null, "tvly", true)]
    [TestCase(PageRendererKind.Tavily, null, "  ", false)]
    [TestCase(PageRendererKind.Tavily, "http://puls-browser:9222", null, false)]
    public void ASetup_IsReady_WithItsSetting_AndNotWithout(PageRendererKind kind, string? browserUrl, string? tavilyKey, bool expected)
    {
        var options = new AgentOptions { BrowserUrl = browserUrl, TavilyApiKey = tavilyKey };

        Assert.That(SetupFor(kind).IsReady(options), Is.EqualTo(expected));
    }

    /// <summary>Null Object: whichever settings are given, a setup's renderer is the unavailable one exactly when the setup is not ready.</summary>
    [Test]
    public void Create_GivesTheUnavailableRenderer_ExactlyWhenTheSetupIsNotReady()
    {
        var everything = new AgentOptions { BrowserUrl = "http://puls-browser:9222", TavilyApiKey = "tvly" };
        foreach (var setup in PageRendererFactory.Setups)
            foreach (var options in new[] { new AgentOptions(), everything })
                Assert.That(setup.Create(options, null, null) is UnavailablePageRenderer, Is.EqualTo(!setup.IsReady(options)), $"{setup.Kind}, ready: {setup.IsReady(options)}");
    }

    /// <summary>The list holds exactly one setup per renderer the enum names, so a new renderer cannot be added without its setup.</summary>
    [Test]
    public void EveryRenderer_HasExactlyOneSetup() =>
        Assert.That(Enum.GetValues<PageRendererKind>().Select(kind => PageRendererFactory.Setups.Count(s => s.Kind == kind)), Is.All.EqualTo(1));

    /// <summary>The list is in fallback order: with no renderer named, the first ready one wins, so Chrome comes before Tavily.</summary>
    [Test]
    public void TheSetups_AreInFallbackOrder() =>
        Assert.That(PageRendererFactory.Setups.Select(s => s.Kind), Is.EqualTo(new[] { PageRendererKind.Chrome, PageRendererKind.Tavily }));
}
