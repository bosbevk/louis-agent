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

    /// <summary>
    /// A band that is a part of a section (F169.2) heads that part with an h3, which carries the framework's
    /// <c>part-title</c> before the site's class, so the docs layout can size it apart from the other h3s a band holds.
    /// </summary>
    [Test]
    public void ABandThatIsAPart_HasAnH3WithThePartsClass()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, "the-engine-family")
            .Add(band => band.PartOf, "the-whole")
            .AddChildContent<BandTitle>(title => title
                .Add(t => t.Class, "chrome")
                .AddChildContent("Agents")));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("h2"), Is.Empty);
            Assert.That(cut.Find("section h3").GetAttribute("class"), Is.EqualTo("part-title chrome"));
            Assert.That(cut.Find("section h3").TextContent, Is.EqualTo("Agents"));
        });
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
