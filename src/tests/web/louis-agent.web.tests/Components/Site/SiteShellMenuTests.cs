using LouisAgent.Web.Components.Site;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// The frame's menu on a phone (F151.6, Louis's pick A of three): the header is one slim row, the site's name and a
/// menu button, and the button opens a sheet with the links, the switches and the site's actions. A link taken
/// closes the sheet as its page opens.
/// </summary>
public class SiteShellMenuTests
{
    /// <summary>A frame with a link and an action, in a browser that answers the frame's module with nothing.</summary>
    private static IRenderedComponent<SiteShell> Frame(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Links, "<a class=\"my-link\" href=\"x\">X</a>")
            .Add(shell => shell.Actions, "<a class=\"my-action\" href=\"y\">Y</a>"));
    }

    /// <summary>The sheet starts shut, and the button says so to a screen reader.</summary>
    [Test]
    public void TheMenu_StartsShut()
    {
        using var ctx = new BunitContext();

        var cut = Frame(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-menu-button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(cut.Find(".site-menu").HasAttribute("data-open"), Is.False);
        });
    }

    /// <summary>The button opens the sheet, and a second press shuts it.</summary>
    [Test]
    public void TheButton_OpensAndShutsTheMenu()
    {
        using var ctx = new BunitContext();
        var cut = Frame(ctx);

        cut.Find(".site-menu-button").Click();
        bool opened = cut.Find(".site-menu").HasAttribute("data-open");
        string said = cut.Find(".site-menu-button").GetAttribute("aria-expanded") ?? "";
        cut.Find(".site-menu-button").Click();

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.True);
            Assert.That(said, Is.EqualTo("true"));
            Assert.That(cut.Find(".site-menu").HasAttribute("data-open"), Is.False);
        });
    }

    /// <summary>A link taken from the open sheet goes to its page, and the sheet shuts.</summary>
    [Test]
    public void ALinkTaken_ShutsTheMenu()
    {
        using var ctx = new BunitContext();
        var cut = Frame(ctx);

        cut.Find(".site-menu-button").Click();
        cut.InvokeAsync(() => ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/pricing"));

        Assert.That(cut.Find(".site-menu").HasAttribute("data-open"), Is.False);
    }
}
