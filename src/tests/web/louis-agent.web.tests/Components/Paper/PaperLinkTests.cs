using LouisAgent.Web.Components.Paper;
using LouisAgent.Web.Paper;

namespace LouisAgent.Web.Tests.Components.Paper;

/// <summary>
/// FX154, Louis: "in a pdf hyperlinks is qr codes always", and of a link on paper, "other wise how else?". A link
/// that is printed is its QR code, its words and its address written short. The code is the link too, so a PDF read
/// on a screen can still be clicked. On a screen the page keeps its ordinary link, and this one is not drawn: that is
/// its style's doing, held by a look at the printed page.
/// </summary>
public class PaperLinkTests
{
    /// <summary>The link for paper, to this address.</summary>
    private static IRenderedComponent<PaperLink> Render(BunitContext ctx, string href, string? address = null) =>
        ctx.Render<PaperLink>(parameters => parameters
            .Add(link => link.Href, href)
            .Add(link => link.Label, "GitHub")
            .Add(link => link.Address, address));

    /// <summary>The code, the link's words and its address written short, the whole of it a link.</summary>
    [Test]
    public void ALinkOnPaper_IsItsCode_ItsWords_AndItsShortAddress()
    {
        using var ctx = new BunitContext();

        var link = Render(ctx, "https://github.com/bosbevk");

        Assert.Multiple(() =>
        {
            Assert.That(link.Find("a.paper-link").GetAttribute("href"), Is.EqualTo("https://github.com/bosbevk"));
            Assert.That(link.Find("a.paper-link svg.qr-image path").GetAttribute("d"),
                Is.EqualTo(QrDrawing.Of("https://github.com/bosbevk").Path));
            Assert.That(link.Find(".paper-link-words b").TextContent, Is.EqualTo("GitHub"));
            Assert.That(link.Find(".paper-link-address").TextContent, Is.EqualTo("github.com/bosbevk"));
        });
    }

    /// <summary>
    /// A link to a place on the site itself holds the site's whole address in its code, since a phone that scans it
    /// knows nothing of which site the paper came from.
    /// </summary>
    [Test]
    public void ALinkToThisSite_HoldsTheWholeAddress()
    {
        using var ctx = new BunitContext();

        var link = Render(ctx, "/framework/how-it-works");

        Assert.Multiple(() =>
        {
            Assert.That(link.Find("svg.qr-image path").GetAttribute("d"),
                Is.EqualTo(QrDrawing.Of("http://localhost/framework/how-it-works").Path));
            Assert.That(link.Find(".paper-link-address").TextContent, Is.EqualTo("localhost/framework/how-it-works"));
        });
    }

    /// <summary>
    /// A PDF is often read in a browser, where the code is clicked. A link that leaves the site opens in a new tab
    /// and tells the other site nothing, as every link out does. A place on this site, or a mail link, opens as it is.
    /// </summary>
    [Test]
    public void ALinkOut_OpensANewTab_WithNoOpener()
    {
        using var ctx = new BunitContext();

        var outward = Render(ctx, "https://github.com/bosbevk").Find("a.paper-link");
        var inward = Render(ctx, "/framework/how-it-works").Find("a.paper-link");
        var mail = Render(ctx, "mailto:louis@example.org").Find("a.paper-link");

        Assert.Multiple(() =>
        {
            Assert.That(outward.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(outward.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(inward.HasAttribute("target"), Is.False);
            Assert.That(mail.HasAttribute("target"), Is.False);
            Assert.That(outward.GetAttribute("class"), Is.EqualTo("paper-link"), "no stray space in its class");
        });
    }

    /// <summary>Its owner may say what is written beside the code: a phone number, a name.</summary>
    [Test]
    public void ItsOwner_MaySayWhatIsWritten()
    {
        using var ctx = new BunitContext();

        var link = Render(ctx, "https://wa.me/27663912470", address: "066 391 2470");

        Assert.That(link.Find(".paper-link-address").TextContent, Is.EqualTo("066 391 2470"));
    }

    /// <summary>The code is a picture with a name, for someone who can't see it.</summary>
    [Test]
    public void TheCode_IsAPictureWithAName()
    {
        using var ctx = new BunitContext();

        var code = Render(ctx, "https://github.com/bosbevk").Find("svg.qr-image");
        var drawing = QrDrawing.Of("https://github.com/bosbevk");

        Assert.Multiple(() =>
        {
            Assert.That(code.GetAttribute("role"), Is.EqualTo("img"));
            Assert.That(code.GetAttribute("aria-label"), Is.EqualTo("QR code: GitHub"));
            Assert.That(code.GetAttribute("viewBox"), Is.EqualTo($"0 0 {drawing.Size} {drawing.Size}"));
        });
    }
}
