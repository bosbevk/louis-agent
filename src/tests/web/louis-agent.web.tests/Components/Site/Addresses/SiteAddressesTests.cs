using LouisAgent.Web.Components.Site.Addresses;

namespace LouisAgent.Web.Tests.Components.Site.Addresses;

/// <summary>
/// Every section of a site's page has its own address (F118, ADR 0107, Louis: "the agent sections must have an url in
/// the framework"): a page's address and the section's id, the landing's sections under the site's home word, and a
/// page of a section paged sideways after it. One rule, read both ways.
/// </summary>
public class SiteAddressesTests
{
    private static readonly SiteAddresses Site = new("home", ["/", "/proof", "/identities", "/how-it-works", "/contact"]);

    /// <summary>A section's address: the page and the id; on the landing, the home word and the id.</summary>
    [TestCase("/proof", "rand", "/proof/rand")]
    [TestCase("/identities", "levels", "/identities/levels")]
    [TestCase("/", "rule", "/home/rule")]
    [TestCase("/", "proof", "/home/proof")]
    [TestCase("/proof", null, "/proof")]
    [TestCase("/identities", "top", "/identities")]
    [TestCase("/", "top", "/")]
    public void ASection_HasAnAddress(string page, string? section, string address) =>
        Assert.That(Site.Address(page, section), Is.EqualTo(address));

    /// <summary>
    /// A page's first section, the one every page names "top", is the page itself (Louis: "see this will confuse the
    /// agent"): one name for one place, so its second page is the page's address and the number.
    /// </summary>
    [TestCase("/how-it-works", "parts", 2, "/how-it-works/parts/2")]
    [TestCase("/how-it-works", "parts", 1, "/how-it-works/parts")]
    [TestCase("/identities", "top", 2, "/identities/2")]
    [TestCase("/", "top", 2, "/home/2")]
    public void APageOfAPagedSection_HasAnAddress(string page, string section, int turn, string address) =>
        Assert.That(Site.Address(page, section, turn), Is.EqualTo(address));

    /// <summary>An address read back: the page, the section and the page of a paged section, when it names them.</summary>
    [TestCase("/proof", "/proof", null, null)]
    [TestCase("/proof/rand", "/proof", "rand", null)]
    [TestCase("/home/proof", "/", "proof", null)]
    [TestCase("/home", "/", null, null)]
    [TestCase("/", "/", null, null)]
    [TestCase("/how-it-works/parts/2", "/how-it-works", "parts", 2)]
    [TestCase("/how-it-works/parts/1", "/how-it-works", "parts", null)]
    [TestCase("/identities/2", "/identities", "top", 2)]
    [TestCase("/home/2", "/", "top", 2)]
    [TestCase("/Proof/Rand/", "/proof", "rand", null)]
    [TestCase("http://127.0.0.1:5080/identities/puls", "/identities", "puls", null)]
    [TestCase("https://example.com/proof?x=1#y", "/proof", null, null)]
    public void AnAddress_ReadsBack(string address, string page, string? section, int? turn)
    {
        var read = Site.Read(address);

        Assert.That(read, Is.EqualTo(new SiteAddress(page, section, turn)));
    }

    /// <summary>An address that names no page of the site is none: the not-found page answers it.</summary>
    [TestCase("/nowhere")]
    [TestCase("/nowhere/rand")]
    [TestCase("/proof/rand/extra/2")]
    [TestCase("/proof/Rand Two")]
    public void AnAddressOfNoPage_IsNone(string address) =>
        Assert.That(Site.Read(address), Is.Null);
}
