using LouisAgent.Web.Components.Site.Showroom.Mock.Website;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock.Website;

/// <summary>
/// F127.2: which page of a mock website an address is on. The page is the longest page path the address starts with,
/// by whole words, so a section and a card are on their page and a page under another page is its own.
/// </summary>
public class MockPathsTests
{
    private static readonly string[] Guesthouse = ["/", "/rooms", "/stay"];

    private static readonly string[] LawPractice = ["/", "/practice", "/practice/wills", "/consultations"];

    /// <summary>A page, a section on it and a card in that section are all on the page.</summary>
    [TestCase("/rooms", "/rooms")]
    [TestCase("/rooms/garden", "/rooms")]
    [TestCase("/stay/breakfast", "/stay")]
    [TestCase("/", "/")]
    public void AnAddress_IsOnItsPage(string address, string page) =>
        Assert.That(MockPaths.PageOf(address, Guesthouse), Is.EqualTo(page));

    /// <summary>A page under another page is its own page, and its sections are on it.</summary>
    [TestCase("/practice/wills", "/practice/wills")]
    [TestCase("/practice/wills/after-divorce", "/practice/wills")]
    [TestCase("/practice/property", "/practice")]
    public void APageUnderAPage_IsItsOwn(string address, string page) =>
        Assert.That(MockPaths.PageOf(address, LawPractice), Is.EqualTo(page));

    /// <summary>No address, an unknown one, or one that only starts like a page (<c>/roomsy</c>) is home.</summary>
    [TestCase(null)]
    [TestCase("/nowhere")]
    [TestCase("/roomsy")]
    public void NoKnownAddress_IsHome(string? address) =>
        Assert.That(MockPaths.PageOf(address, Guesthouse), Is.EqualTo("/"));

    /// <summary>A website without a home page starts on its first page.</summary>
    [Test]
    public void WithoutAHome_TheFirstPageIsWhereItStarts() =>
        Assert.That(MockPaths.PageOf(null, ["/menu", "/visit"]), Is.EqualTo("/menu"));
}
