using LouisAgent.Web.Shared.Text;

namespace LouisAgent.Web.Shared.Tests.Text;

// T1: the markdown an answer is shown with. Tables and links render; raw HTML is text, never markup; only plain web
// and mail links survive; an image loads only from a shop's own host (F33), any other becomes a link.
public class MarkdownTests
{
    private static string Html(string markdown) => Markdown.ToHtml(markdown).Value;

    [Test]
    public void ATable_Renders()
    {
        string html = Html("| Bestanddeel | Prys |\n|---|---|\n| Rys | R24,99 |\n");

        Assert.That(html, Does.Contain("<table>").And.Contain("<th>Bestanddeel</th>").And.Contain("<td>R24,99</td>"));
    }

    [Test]
    public void ALink_Renders_WithItsAddress()
    {
        string html = Html("Sien [Checkers](https://www.checkers.co.za/) en https://example.com/pad.");

        Assert.That(html, Does.Contain("<a href=\"https://www.checkers.co.za/\">Checkers</a>"));
        Assert.That(html, Does.Contain("href=\"https://example.com/pad\""), "an autolink too");
    }

    [Test]
    public void AnUnsafeLink_LosesItsAddress()
    {
        Assert.That(Html("[klik](javascript:alert(1))"), Does.Contain("href=\"#\"").And.Not.Contain("javascript:"));
        Assert.That(Html("[data](data:text/html;base64,AAAA)"), Does.Contain("href=\"#\"").And.Not.Contain("data:"));
        Assert.That(Html("[pos](mailto:puls@example.com)"), Does.Contain("href=\"mailto:puls@example.com\""));
    }

    [Test]
    public void AShopsImage_IsShown_WithItsOriginalForTheDialog()
    {
        string html = Html("![KOO Chakalaka](https://catalog.sixty60.co.za/a/b.jpg?w=300)");

        Assert.That(html, Does.Contain("<img"));
        Assert.That(html, Does.Contain("src=\"https://catalog.sixty60.co.za/a/b.jpg?w=300\""));
        Assert.That(html, Does.Contain("data-original=\"https://catalog.sixty60.co.za/a/b.jpg\""), "the same file, without the size");
    }

    [Test]
    public void AnyOtherImage_BecomesALink_NeverLoaded()
    {
        string html = Html("![foto](https://evil.example/track.png?chat=secret)");

        Assert.That(html, Does.Not.Contain("<img"));
        Assert.That(html, Does.Contain("<a href=\"https://evil.example/track.png?chat=secret\">foto</a>"));
    }

    [Test]
    public void RawHtml_IsShownAsText()
    {
        string html = Html("Kyk <script>alert(1)</script> en <b>vet</b>");

        Assert.That(html, Does.Not.Contain("<script>").And.Not.Contain("<b>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }
}
