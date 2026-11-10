using LouisAgent.Web.Components.Site.Bands;

namespace LouisAgent.Web.Tests.Components.Site.Bands;

/// <summary>
/// FX125: the band gives the heading. A page's own band, the one at its top (ADR 0107), heads it with the page's one
/// h1, so search engines and screen readers find what the page is; every other band's title is an h2.
/// </summary>
public class BandTitleTests
{
    /// <summary>A band with its title, drawn the way a page writes it.</summary>
    private static IRenderedComponent<Band> BandWithTitle(BunitContext ctx, string id) =>
        ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, id)
            .AddChildContent<BandTitle>(title => title
                .Add(t => t.Class, "chrome")
                .AddChildContent("Two plans. No surprises.")));

    /// <summary>The page's own band heads the page: an h1, with the site's class kept.</summary>
    [Test]
    public void ThePagesOwnBand_HeadsItWithTheH1()
    {
        using var ctx = new BunitContext();

        var heading = BandWithTitle(ctx, "top").Find("section h1");

        Assert.That((heading.TextContent, heading.ClassList.Contains("chrome")),
            Is.EqualTo(("Two plans. No surprises.", true)));
    }

    /// <summary>Any other band's title is a section's heading, an h2.</summary>
    [Test]
    public void AnotherBand_HasAnH2()
    {
        using var ctx = new BunitContext();

        var cut = BandWithTitle(ctx, "plans");

        Assert.That((cut.FindAll("h1").Count, cut.FindAll("h2").Count), Is.EqualTo((0, 1)));
    }

    /// <summary>A title drawn outside a band is a section's heading too, never a second h1.</summary>
    [Test]
    public void ATitleOutsideABand_IsAnH2()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<BandTitle>(title => title.AddChildContent("Alone"));

        Assert.That(cut.Find("h2").TextContent, Is.EqualTo("Alone"));
    }
}
