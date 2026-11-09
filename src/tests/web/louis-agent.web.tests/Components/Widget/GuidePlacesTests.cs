using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Widget;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// The guide works in addresses (F118, ADR 0107, Louis: "the agent sections must have an url"): what it reads of the
/// page names every place by its address, and what it hands its page tool is read back into the page and the place.
/// </summary>
public class GuidePlacesTests
{
    private static readonly SiteAddresses Site = new("home", ["/", "/identities", "/how-it-works"]);

    /// <summary>Every area, card and page of a paged section, and the one in view, read as addresses.</summary>
    [Test]
    public void ThePage_NamesItsPlaces_ByTheirAddresses()
    {
        var page = new Page("/identities/the-guide", "Identities", "Who an agent is.",
            [new PageArea("levels", "The three that exist"), new PageArea("the-guide", "The guide (card, opens)"), new PageArea("parts/2", "page 2 of 3")],
            new PageArea("levels", "The three that exist"));

        var read = GuidePlaces.Addressed(page, Site, "/identities");

        Assert.Multiple(() =>
        {
            Assert.That(read.Url, Is.EqualTo("/identities"));
            Assert.That(read.Areas.Select(area => area.Id), Is.EqualTo(new[] { "/identities/levels", "/identities/the-guide", "/identities/parts/2" }));
            Assert.That(read.InView!.Id, Is.EqualTo("/identities/levels"));
        });
    }

    /// <summary>
    /// A page's first section is the page itself, so the guide reads it by the same address the bar shows: the page's,
    /// and its later pages after it.
    /// </summary>
    [Test]
    public void TheFirstSection_IsThePage()
    {
        var page = new Page("/architecture", "Architecture", "The whole framework.",
            [new PageArea("top", "Architecture"), new PageArea("top/2", "page 2 of 2"), new PageArea("engine", "src/engine")]);

        var read = GuidePlaces.Addressed(page, new SiteAddresses("home", ["/", "/architecture"]), "/architecture");

        Assert.That(read.Areas.Select(area => area.Id), Is.EqualTo(new[] { "/architecture", "/architecture/2", "/architecture/engine" }));
    }

    /// <summary>The landing's places sit under the home word, so they never clash with a page of the same name.</summary>
    [Test]
    public void TheLandingsPlaces_SitUnderTheHomeWord() =>
        Assert.That(GuidePlaces.Addressed(new Page("/", "Home", "Hi.", [new PageArea("proof", "Measured")]), Site, "/").Areas[0].Id,
            Is.EqualTo("/home/proof"));

    /// <summary>A page tool's address goes to the page and the place; one that names no page goes nowhere.</summary>
    [TestCase("/identities", "/identities", "top")]
    [TestCase("/identities/2", "/identities", "top/2")]
    [TestCase("/identities/the-guide", "/identities", "the-guide")]
    [TestCase("/how-it-works/parts/2", "/how-it-works", "parts/2")]
    [TestCase("/home/proof", "/", "proof")]
    public void AnAddress_GoesToItsPlace(string address, string page, string? place) =>
        Assert.That(GuidePlaces.Target(address, Site), Is.EqualTo((page, place)));

    /// <summary>An address of no page is nothing to follow.</summary>
    [Test]
    public void AnAddressOfNoPage_GoesNowhere() => Assert.That(GuidePlaces.Target("/nowhere/x", Site), Is.Null);
}
