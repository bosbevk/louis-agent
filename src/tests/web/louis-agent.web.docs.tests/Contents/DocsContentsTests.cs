using LouisAgent.Web.Docs.Contents;

namespace LouisAgent.Web.Docs.Tests.Contents;

/// <summary>
/// F161.1: a docs site's contents, its pages in reading order under their groups, is the one list both the menu and a
/// page's previous and next are read from, so the two can't disagree.
/// </summary>
public class DocsContentsTests
{
    /// <summary>Two groups, three pages, in reading order.</summary>
    private static readonly DocsContents Book = new([
        new DocsGroup("Start", [new DocsEntry("Overview", "/"), new DocsEntry("Getting started", "/getting-started")]),
        new DocsGroup("Understand", [new DocsEntry("How it works", "/how-it-works")])
    ]);

    /// <summary>A page's place is its own address, or a section's under it.</summary>
    [TestCase("/how-it-works", "How it works")]
    [TestCase("/how-it-works/the-turn", "How it works")]
    [TestCase("/", "Overview")]
    [TestCase("/getting-started/", "Getting started")]
    public void APage_IsFoundByItsAddress(string address, string title) =>
        Assert.That(Book.FindEntry(address)?.Title, Is.EqualTo(title));

    /// <summary>An address of no page in the contents finds none, and the root matches only itself.</summary>
    [TestCase("/contact")]
    [TestCase("/nothing/here")]
    public void AnAddressOfNoPage_FindsNone(string address) => Assert.That(Book.FindEntry(address), Is.Null);

    /// <summary>The page before and the page after follow reading order, across groups.</summary>
    [Test]
    public void BeforeAndAfter_FollowReadingOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Book.FindBefore("/"), Is.Null, "nothing before the first");
            Assert.That(Book.FindAfter("/")?.Title, Is.EqualTo("Getting started"));
            Assert.That(Book.FindAfter("/getting-started")?.Title, Is.EqualTo("How it works"), "across a group");
            Assert.That(Book.FindBefore("/how-it-works/the-turn")?.Title, Is.EqualTo("Getting started"));
            Assert.That(Book.FindAfter("/how-it-works"), Is.Null, "nothing after the last");
        });
    }
}
