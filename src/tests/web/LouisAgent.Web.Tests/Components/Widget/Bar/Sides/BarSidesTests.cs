using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Bar.Sides;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Sides;

/// <summary>
/// FX168: a site may stand something in the empty room on each side of the agent bar (Louis: "we need to show off
/// some branding somewhere"), its name on the left and a note on the right, beside the bar's row.
/// </summary>
public class BarSidesTests
{
    /// <summary>A context with what the widget asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The guide's widget as the bar, with nothing round it.</summary>
    private static RenderFragment Bar => builder =>
    {
        builder.OpenComponent<ChatWidget>(0);
        builder.AddComponentParameter(1, nameof(ChatWidget.Identity), "guide");
        builder.AddComponentParameter(2, nameof(ChatWidget.Layout), WidgetLayout.Bar);
        builder.CloseComponent();
    };

    /// <summary>Given sides, the bar stands the first on its left and the second on its right.</summary>
    [Test]
    public void GivenSides_TheBarStandsThemLeftAndRight()
    {
        using var ctx = Context();
        RenderFragment start = builder => builder.AddMarkupContent(0, "<b class=\"the-name\">name</b>");
        RenderFragment end = builder => builder.AddMarkupContent(0, "<i class=\"the-note\">note</i>");

        var cut = ctx.Render<BarSides>(p => p.Add(s => s.Start, start).Add(s => s.End, end)
            .Add(s => s.ChildContent, Bar));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".agent-bar .agent-dock > .agent-side.is-start .the-name"), Is.Not.Null);
            Assert.That(cut.Find(".agent-bar .agent-dock > .agent-side.is-end .the-note"), Is.Not.Null);
        });
    }

    /// <summary>With no sides given, the bar stands alone, as it always has.</summary>
    [Test]
    public void WithNoSides_TheBarStandsAlone()
    {
        using var ctx = Context();

        var cut = ctx.Render(Bar);

        Assert.That(cut.FindAll(".agent-side"), Is.Empty);
    }
}
