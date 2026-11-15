using AngleSharp.Dom;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Components.Site.Looks;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Components.Site.Looks;

/// <summary>
/// F151.3: the switch a visitor picks the look with. It shows a group of the site's themes when it has two or more, a
/// group of the two styles when it has any, and nothing when it has none. Each group says what it is to a screen
/// reader, and one button in each says it is pressed: the default theme and the default style, until the browser
/// says otherwise.
/// </summary>
public class LookSwitchTests
{
    /// <summary>An area that carries a theme, as the themes project adds one.</summary>
    private static SiteArea ThemeArea(SiteTheme theme) => new($"theme-{theme.Id}", _ => { }, [], Theme: theme);

    /// <summary>The switch on a site that names these areas.</summary>
    private static IRenderedComponent<LookSwitch> Render(BunitContext ctx, Action<SiteAreasBuilder> areas)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(areas);
        return ctx.Render<LookSwitch>();
    }

    /// <summary>Each button's words in a group, and which of them says it is pressed.</summary>
    private static (string[] Words, string[] Pressed) Buttons(IElement group)
    {
        var buttons = group.QuerySelectorAll("button").ToList();
        return ([.. buttons.Select(b => b.TextContent.Trim())],
            [.. buttons.Where(b => b.GetAttribute("aria-pressed") == "true").Select(b => b.TextContent.Trim())]);
    }

    /// <summary>A site that names no theme gets no switch.</summary>
    [Test]
    public void WithNoTheme_ItDrawsNothing()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, areas => areas.Chat());

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    /// <summary>With one theme there is nothing to pick between, so only the styles are shown, light pressed.</summary>
    [Test]
    public void WithOneTheme_ItDrawsTheStylesAlone()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, areas => areas.Add(ThemeArea(new SiteTheme("glass", "Glass"))));
        var groups = cut.FindAll("[role=group]");

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(groups[0].GetAttribute("aria-label"), Is.EqualTo("Style"));
            Assert.That(Buttons(groups[0]).Words, Is.EqualTo(new[] { "Dark", "Light" }));
            Assert.That(Buttons(groups[0]).Pressed, Is.EqualTo(new[] { "Light" }));
        });
    }

    /// <summary>With two themes both groups show, the themes by name in the order named, the first pressed.</summary>
    [Test]
    public void WithTwoThemes_ItDrawsBothGroups_TheFirstThemePressed()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, areas => areas
            .Add(ThemeArea(new SiteTheme("glass", "Glass")))
            .Add(ThemeArea(new SiteTheme("flat", "Flat"))));
        var groups = cut.FindAll("[role=group]");

        Assert.That(groups, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(groups[0].GetAttribute("aria-label"), Is.EqualTo("Theme"));
            Assert.That(Buttons(groups[0]).Words, Is.EqualTo(new[] { "Glass", "Flat" }));
            Assert.That(Buttons(groups[0]).Pressed, Is.EqualTo(new[] { "Glass" }));
            Assert.That(groups[1].GetAttribute("aria-label"), Is.EqualTo("Style"));
            Assert.That(Buttons(groups[1]).Pressed, Is.EqualTo(new[] { "Light" }));
        });
    }

    /// <summary>The switch with glass and flat, and its module, which says what it is asked.</summary>
    private static (IRenderedComponent<LookSwitch> Switch, BunitJSModuleInterop Module) WithModule(BunitContext ctx,
        PageLook? onThePage = null)
    {
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.LookSwitch);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<PageLook?>("current").SetResult(onThePage);
        var cut = Render(ctx, areas => areas
            .Add(ThemeArea(new SiteTheme("glass", "Glass")))
            .Add(ThemeArea(new SiteTheme("flat", "Flat"))));
        return (cut, module);
    }

    /// <summary>The words of the pressed buttons, the theme's and then the style's.</summary>
    private static string[] PressedWords(IRenderedComponent<LookSwitch> cut) =>
        [.. cut.FindAll("button[aria-pressed=true]").Select(button => button.TextContent.Trim())];

    /// <summary>A press on Dark asks the module to pick the dark style, and Dark is the pressed one.</summary>
    [Test]
    public void APressOnAStyle_PicksIt_AndPressesIt()
    {
        using var ctx = new BunitContext();
        var (cut, module) = WithModule(ctx);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Dark").Click();

        Assert.Multiple(() =>
        {
            Assert.That(module.Invocations["pickStyle"].Single().Arguments, Is.EqualTo(new object?[] { "dark" }));
            Assert.That(PressedWords(cut), Is.EqualTo(new[] { "Glass", "Dark" }));
        });
    }

    /// <summary>A press on Flat asks the module to pick the flat theme, and Flat is the pressed one.</summary>
    [Test]
    public void APressOnATheme_PicksIt_AndPressesIt()
    {
        using var ctx = new BunitContext();
        var (cut, module) = WithModule(ctx);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Flat").Click();

        Assert.Multiple(() =>
        {
            Assert.That(module.Invocations["pickTheme"].Single().Arguments, Is.EqualTo(new object?[] { "flat" }));
            Assert.That(PressedWords(cut), Is.EqualTo(new[] { "Flat", "Light" }));
        });
    }

    /// <summary>
    /// Drawn on a page the first paint already put in flat and dark, the switch shows flat and dark pressed.
    /// </summary>
    [Test]
    public void WhenFirstDrawn_ItShowsTheLookThePageIsIn()
    {
        using var ctx = new BunitContext();

        var (cut, _) = WithModule(ctx, new PageLook("flat", "dark"));

        cut.WaitForAssertion(() => Assert.That(PressedWords(cut), Is.EqualTo(new[] { "Flat", "Dark" })));
    }

    /// <summary>On the narrowest phones the switch's two groups may sit one above the other, not overflow.</summary>
    [Test]
    public void OnANarrowPhone_TheGroupsMayWrap() =>
        Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                "src/web/louis-agent.web/Components/Site/Looks/LookSwitch.razor.scss")),
            Does.Contain(".look-switch { display: inline-flex; flex-wrap: wrap; gap: 8px; }"));

    /// <summary>The switch with two themes, its markup pinned, as every component's is.</summary>
    [Test]
    public void ItsMarkup_WithTwoThemes_IsPinned()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, areas => areas
            .Add(ThemeArea(new SiteTheme("glass", "Glass")))
            .Add(ThemeArea(new SiteTheme("flat", "Flat"))));

        Snapshots.Match("Components/Site/Looks/LookSwitch.html", cut.Markup);
    }
}
