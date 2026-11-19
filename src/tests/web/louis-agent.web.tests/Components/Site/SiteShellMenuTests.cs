using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
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

    /// <summary>A context whose site offers glass and flat, so the frame draws the look switch.</summary>
    private static BunitContext WithThemes()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(areas => areas
            .Add(new SiteArea("theme-glass", _ => { }, [], Theme: new SiteTheme("glass", "Glass")))
            .Add(new SiteArea("theme-flat", _ => { }, [], Theme: new SiteTheme("flat", "Flat"))));
        return ctx;
    }

    /// <summary>
    /// FX208 (Louis's pick A of three): on a phone the menu is the one button, so a site's settings, the look switch
    /// and what the site puts there, also stand at the foot of the menu's sheet.
    /// </summary>
    [Test]
    public void WithSettings_TheSheetHoldsThemToo()
    {
        using var ctx = WithThemes();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Links, "<a class=\"my-link\" href=\"x\">X</a>")
            .Add(shell => shell.Settings, "<a class=\"cv\" href=\"/cv\">CV</a>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site-menu .site-menu-settings .look-switch"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".site-menu .site-menu-settings a.cv"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// A site with settings and no links still has a menu button, since on a phone the settings have no other way in.
    /// </summary>
    [Test]
    public void WithSettingsAlone_TheMenuButtonIsStillDrawn()
    {
        using var ctx = WithThemes();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Settings, "<a class=\"cv\" href=\"/cv\">CV</a>"));

        Assert.That(cut.FindAll(".site-header .site-menu-button"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// FX208: on a phone the settings' own button and panel aren't drawn, so the header holds one three-line button
    /// and fits a 390 screen; on a wide screen the sheet's copy of the settings isn't shown.
    /// </summary>
    [Test]
    public void OnAPhone_OnlyTheMenuButtonStands()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/SiteShell.razor.scss")).ReplaceLineEndings("\n");
        int phoneAt = rules.IndexOf("@media (max-width: 720px)", StringComparison.Ordinal);
        string wide = rules[..phoneAt];
        string phone = rules[phoneAt..];

        Assert.Multiple(() =>
        {
            Assert.That(wide, Does.Contain(".site-menu-settings { display: none; }"));
            Assert.That(phone, Does.Contain(".site-settings-button, .site-settings { display: none; }"));
            Assert.That(phone, Does.Contain(".site-menu-settings {"));
        });
    }
}
