using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>
/// FX135, from Louis on louis-agent.dev: the guide moved him from a demo page to the home page, another layout, and the
/// guide there started folded, its talk not carried on ("the agentsession must be for the whole site", "except the
/// demos"). A site that keeps its guide hands every widget of an identity the same chat and the same state.
/// </summary>
public class GuideKeeperTests
{
    /// <summary>A context with what the widget asks for over the stub API, and the site's keeper.</summary>
    private static BunitContext Context(GuideKeeper keeper)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(keeper);
        return ctx;
    }

    /// <summary>
    /// The guide opened and sized under one layout is open, the same size and on the same chat when the next layout
    /// draws it again; another identity's widget keeps its own.
    /// </summary>
    [Test]
    public void TheNextLayoutsGuide_IsOpen_TheSameSize_OnTheSameChat()
    {
        var keeper = new GuideKeeper();
        using var first = Context(keeper);
        var before = first.Render<ChatWidget>(p => p.Add(w => w.Identity, "guide"));
        before.Find("button.widget-button").Click();
        var kept = keeper.For("guide");
        kept.Size = ChatWindowSize.Maximised;
        before.Dispose();

        using var second = Context(keeper);
        var after = second.Render<ChatWidget>(p => p.Add(w => w.Identity, "guide"));

        Assert.Multiple(() =>
        {
            Assert.That(after.Find(".chat-widget").ClassList, Does.Contain("is-open"));
            Assert.That(after.Find(".widget-panel").ClassList, Does.Contain("is-max"));
            Assert.That(keeper.For("guide"), Is.SameAs(kept), "one guide for the whole site");
            Assert.That(keeper.For("acme").Open, Is.False, "another agent's widget keeps its own");
        });
    }
}
