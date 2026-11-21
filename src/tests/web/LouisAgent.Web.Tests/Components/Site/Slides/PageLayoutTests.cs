using LouisAgent.Web.Components.Site;
using LouisAgent.Web.Components.Site.Slides;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Slides;

/// <summary>
/// A site's pages are laid out as slides or as a flowing page, a framework option (F118, Louis: "like a layout option in
/// the framework", option 2): the site sets its default on the frame, and a page may choose the other for itself.
/// </summary>
public class PageLayoutTests
{
    /// <summary>A context whose browser answers the frame's modules with nothing.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>A frame with no layout named draws a flowing page, so a site gets an ordinary page unless it asks.</summary>
    [Test]
    public void AFrame_Flows_UnlessTheSiteAsks()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters.AddChildContent("<div class=\"page\">Page</div>"));

        Assert.That(cut.Find(".site").ClassList, Is.EquivalentTo(new[] { "site", "layout-flow" }));
    }

    /// <summary>A site that sets slides gets slides on every page that says nothing.</summary>
    [Test]
    public void ASiteThatSetsSlides_GetsSlides()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Layout, PageLayout.Slides)
            .AddChildContent("<div class=\"page\">Page</div>"));

        Assert.That(cut.Find(".site").ClassList, Is.EquivalentTo(new[] { "site", "layout-slides" }));
    }

    /// <summary>A page that chooses to flow flows on a site of slides, and the site's default comes back when it goes.</summary>
    [Test]
    public void APagesChoice_WinsWhileItIsThere()
    {
        using var ctx = Context();
        RenderFragment page = builder =>
        {
            builder.OpenComponent<PageLayoutChoice>(0);
            builder.AddComponentParameter(1, nameof(PageLayoutChoice.Layout), PageLayout.Flow);
            builder.CloseComponent();
        };
        var cut = ctx.Render<SiteShell>(parameters => parameters
            .Add(shell => shell.Layout, PageLayout.Slides)
            .Add(shell => shell.ChildContent, page));

        cut.WaitForAssertion(() => Assert.That(cut.Find(".site").ClassList, Does.Contain("layout-flow")));
        cut.Render(parameters => parameters.Add(shell => shell.ChildContent, (RenderFragment)(_ => { })));

        cut.WaitForAssertion(() => Assert.That(cut.Find(".site").ClassList, Does.Contain("layout-slides")));
    }
}
