using LouisAgent.Api.Site.Places;

namespace LouisAgent.Api.Tests.Site.Places;

/// <summary>
/// A place's words are the words a visitor reads (F164.2): what a <c>style</c> or a <c>script</c> holds, such as a
/// diagram's own rules, is never read as words, so the guide and the site's search never show it.
/// </summary>
public class PlaceReaderTests
{
    /// <summary>A section's style and script are left out of its words, and the words around them kept.</summary>
    [Test]
    public void AStyleAndAScript_AreNoWords()
    {
        const string page =
            "<section id=\"loads\"><h2>What it loads</h2><p>The catalogue names the identities.</p>" +
            "<svg><style>@scope { :scope { --ground: #ffffff; } }</style><text>The program</text></svg>" +
            "<script>window.x = 1;</script><p>And the packs.</p></section>";

        var place = PlaceReader.Read(page, "/getting-started").Single();

        Assert.That(place.Text,
            Is.EqualTo("What it loads The catalogue names the identities. The program And the packs."));
    }
}
