using LouisAgent.Web.Components.Site;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// A site's frame: a header with the brand, the links and the actions, the page, a footer, and a corner for what
/// floats over every page. A slot left empty leaves no gap, so a site only names what it has.
/// </summary>
public class SiteShellTests
{
    /// <summary>A context whose browser answers the frame's module with nothing, and lists what was asked.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>
    /// The frame attaches its motion after it draws (F110): the module is imported from its path and asked to attach,
    /// so a page's index, progress line and reveals move without a script of the site's own.
    /// </summary>
    [Test]
    public void TheFrame_AttachesItsMotion_WhenItDraws()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters.AddChildContent("<div class=\"page\">Page</div>"));

        cut.WaitForAssertion(() =>
            Assert.That(ctx.JSInterop.Invocations.Select(call => call.Identifier), Does.Contain("attach")));
        Assert.That(ctx.JSInterop.Invocations.Where(call => call.Identifier == "import")
            .Select(call => call.Arguments[0]), Does.Contain(FrameworkScripts.SiteShell));
    }

    /// <summary>The brand links home, or where the site says.</summary>
    [Test]
    public void TheBrand_LinksHome()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Brand, "<b class=\"logo\">Brand</b>"));

        var brand = cut.Find(".site-header a.site-brand");
        Assert.That(brand.GetAttribute("href"), Is.EqualTo("/"));
        Assert.That(brand.QuerySelectorAll(".logo"), Has.Length.EqualTo(1));
    }

    /// <summary>The links go in the header's menu, and the actions come after them.</summary>
    [Test]
    public void TheLinks_GoInTheMenu_AndTheActionsAfter()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Links, "<a class=\"my-link\" href=\"x\">X</a>")
            .Add(shell => shell.Actions, "<a class=\"my-action\" href=\"y\">Y</a>"));

        var inside = cut.Find(".site-header").Children.Select(child => child.ClassName).ToList();
        Assert.That(cut.FindAll(".site-nav .my-link"), Has.Count.EqualTo(1));
        Assert.That(inside, Is.EqualTo(new[] { "site-nav", "site-actions" }));
    }

    /// <summary>The page sits in the main column, the footer under it, and the corner comes last of all.</summary>
    [Test]
    public void ThePage_TheFooter_ThenTheCorner()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Footer, "<span class=\"note\">Note</span>")
            .Add(shell => shell.Corner, "<div class=\"widget\">Hi</div>")
            .AddChildContent("<div class=\"page\">Page</div>"));

        var inside = cut.Find(".site").Children.Select(child => child.ClassName).ToList();
        Assert.That(cut.FindAll(".site-main .page"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".site-footer .note"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".site-corner .widget"), Has.Count.EqualTo(1));
        Assert.That(inside, Is.EqualTo(new[] { "site-header", "site-main", "site-footer", "site-corner" }));
    }

    /// <summary>With nothing named, the page still shows, and the empty slots leave no gaps.</summary>
    [Test]
    public void WithNothingNamed_ThePageShows_AndNoSlotLeavesAGap()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters.AddChildContent("<div class=\"page\">Page</div>"));

        Assert.That(cut.FindAll(".site-main .page"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".site-brand"), Is.Empty);
        Assert.That(cut.FindAll(".site-nav"), Is.Empty);
        Assert.That(cut.FindAll(".site-actions"), Is.Empty);
        Assert.That(cut.FindAll(".site-footer"), Is.Empty);
        Assert.That(cut.FindAll(".site-corner"), Is.Empty);
    }
}
