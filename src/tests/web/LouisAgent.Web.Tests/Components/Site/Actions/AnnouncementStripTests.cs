using LouisAgent.Web.Components.Site.Actions;

namespace LouisAgent.Web.Tests.Components.Site.Actions;

/// <summary>
/// A one-line announcement that leads to its own page (ADR 0125): the whole line is one link, with an optional
/// status pill, a sentence and the words that say where it goes.
/// </summary>
public class AnnouncementStripTests
{
    /// <summary>The whole line is one link, its pill, its sentence and its words in that order.</summary>
    [Test]
    public void TheLine_IsOneLink_WithItsPillSentenceAndWords()
    {
        using var ctx = new BunitContext();

        var link = ctx.Render<AnnouncementStrip>(p => p.Add(s => s.Href, "/free-websites")
            .Add(s => s.Status, "Entries open").Add(s => s.Text, "Two businesses get a free website.")
            .Add(s => s.LinkText, "See how it works")).Find("a.announcement-strip");

        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("href"), Is.EqualTo("/free-websites"));
            Assert.That(link.QuerySelector(".status-pill")?.TextContent, Is.EqualTo("Entries open"));
            Assert.That(link.QuerySelector(".announcement-text")?.TextContent, Is.EqualTo("Two businesses get a free website."));
            Assert.That(link.QuerySelector(".announcement-go")?.TextContent, Is.EqualTo("See how it works"));
        });
    }

    /// <summary>With no status, no pill is drawn.</summary>
    [Test]
    public void NoStatus_DrawsNoPill()
    {
        using var ctx = new BunitContext();

        var strip = ctx.Render<AnnouncementStrip>(p => p.Add(s => s.Href, "/free-websites")
            .Add(s => s.Text, "Two businesses get a free website.").Add(s => s.LinkText, "See how it works"));

        Assert.That(strip.FindAll(".status-pill"), Is.Empty);
    }
}
