using LouisAgent.Packs.Site.Tools;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>
/// The one tool of the site toolset (F117): it takes a page of this site, with an area after the hash when there is
/// one, says the page is shown and asks for one line about it; anything that isn't a page of this site is refused
/// with the reason, so the model gives a path that is.
/// </summary>
public class SiteToolsTests
{
    private readonly SiteTools _tools = new();

    /// <summary>
    /// A page of the site, and a place on it by its own address (F118, ADR 0107): a section, a card (whose id may hold
    /// digits), the landing's under the home word, a page of a paged section.
    /// </summary>
    [TestCase("/identities")]
    [TestCase("/identities/levels")]
    [TestCase("/")]
    [TestCase("/home/rule")]
    [TestCase("/identities/the-coding-agent")]
    [TestCase("/proof/a-10-fixes")]
    [TestCase("/how-it-works/parts/2")]
    [TestCase("/build-your-own")]
    public void ASitePath_IsShown(string path)
    {
        Assert.That(_tools.ShowPage(path), Is.EqualTo($"Shown: the visitor's browser is now on {path}. Say in one line what to look at there."));
    }

    /// <summary>Not a page of this site: another host, a scheme, a climb out of the site, a space, an empty path, a bad area.</summary>
    [TestCase("https://example.com/")]
    [TestCase("example.com/packs")]
    [TestCase("//evil.example/")]
    [TestCase("/../secret")]
    [TestCase("/pa cks")]
    [TestCase("")]
    [TestCase("/identities/Levels Two")]
    [TestCase("/identities#levels")]
    [TestCase("/how-it-works/parts/0")]
    [TestCase("/how-it-works/parts/02")]
    [TestCase("/how-it-works?x=1")]
    [TestCase("/identities//levels")]
    public void AnythingElse_IsRefused_WithTheReason(string path)
    {
        Assert.That(_tools.ShowPage(path),
            Is.EqualTo($"Not shown: '{path}' isn't a page of this site. Give an address from the page's list: a page such as /packs, or a place on one such as /identities/levels."));
    }
}
