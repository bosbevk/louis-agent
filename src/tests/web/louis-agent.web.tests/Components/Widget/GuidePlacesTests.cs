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
        var page = new Page("/identities/puls", "Identities", "Who an agent is.",
            [new PageArea("levels", "The three that exist"), new PageArea("puls", "Puls (card, opens)"), new PageArea("parts/2", "page 2 of 3")],
            new PageArea("levels", "The three that exist"));

        var read = GuidePlaces.Addressed(page, Site, "/identities");

        Assert.Multiple(() =>
        {
            Assert.That(read.Url, Is.EqualTo("/identities"));
            Assert.That(read.Areas.Select(area => area.Id), Is.EqualTo(new[] { "/identities/levels", "/identities/puls", "/identities/parts/2" }));
            Assert.That(read.InView!.Id, Is.EqualTo("/identities/levels"));
        });
    }

    /// <summary>The landing's places sit under the home word, so they never clash with a page of the same name.</summary>
    [Test]
    public void TheLandingsPlaces_SitUnderTheHomeWord() =>
        Assert.That(GuidePlaces.Addressed(new Page("/", "Home", "Hi.", [new PageArea("proof", "Measured")]), Site, "/").Areas[0].Id,
            Is.EqualTo("/home/proof"));

    /// <summary>A page tool's address goes to the page and the place; one that names no page goes nowhere.</summary>
    [TestCase("/identities", "/identities", null)]
    [TestCase("/identities/puls", "/identities", "puls")]
    [TestCase("/how-it-works/parts/2", "/how-it-works", "parts/2")]
    [TestCase("/home/proof", "/", "proof")]
    public void AnAddress_GoesToItsPlace(string address, string page, string? place) =>
        Assert.That(GuidePlaces.Target(address, Site), Is.EqualTo((page, place)));

    /// <summary>An address of no page is nothing to follow.</summary>
    [Test]
    public void AnAddressOfNoPage_GoesNowhere() => Assert.That(GuidePlaces.Target("/nowhere/x", Site), Is.Null);
}
