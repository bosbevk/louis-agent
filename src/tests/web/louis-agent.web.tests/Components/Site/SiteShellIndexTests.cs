using LouisAgent.Web.Components.Site;
using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// FX175: a site may switch the section index off, and the frame keeps it ready for the day it is switched on again
/// (Louis: "we must just hide this disabled, not use it, not delete it").
/// </summary>
public class SiteShellIndexTests
{
    /// <summary>A context with what the frame asks for, over the stub API.</summary>
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

    /// <summary>A page of two bands, enough for an index.</summary>
    private static RenderFragment TwoBands => builder =>
    {
        builder.OpenComponent<Band>(0);
        builder.AddComponentParameter(1, nameof(Band.Id), "rule");
        builder.AddComponentParameter(2, nameof(Band.Topic), "One rule");
        builder.CloseComponent();
        builder.OpenComponent<Band>(3);
        builder.AddComponentParameter(4, nameof(Band.Id), "contact");
        builder.AddComponentParameter(5, nameof(Band.Short), "Contact");
        builder.CloseComponent();
    };

    /// <summary>With the index switched off, the frame draws none, and still has its way back up.</summary>
    [Test]
    public void WithTheIndexOff_TheFrameDrawsNone()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.ChildContent, TwoBands).Add(shell => shell.ShowIndex, false));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site-index"), Is.Empty);
            Assert.That(cut.FindAll(".site-up"), Has.Count.EqualTo(1));
        });
    }
}
