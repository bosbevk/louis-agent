using LouisAgent.FilmExport.Cards.Pages;

namespace LouisAgent.FilmExport.Tests.Cards.Pages;

/// <summary>
/// A page's card carries the page's own words (F169.6): its name, which is its title without the site's name, as the
/// host's breadcrumb has it, its first heading and its address; and one line of them says what the card shows.
/// </summary>
public class PageCardWordsTests
{
    private static PageCardWords GettingStarted() => PageCardWords.Of("Getting started · louis-agent", "louis-agent",
        "Your agent, in two lines.", "framework.louis-agent.dev/getting-started");

    /// <summary>The page's name is its title without the site's name.</summary>
    [Test]
    public void ThePagesName_IsItsTitle_WithoutTheSitesName()
    {
        var words = GettingStarted();

        Assert.Multiple(() =>
        {
            Assert.That(words.Page, Is.EqualTo("Getting started"));
            Assert.That(words.Heading, Is.EqualTo("Your agent, in two lines."));
            Assert.That(words.Address, Is.EqualTo("framework.louis-agent.dev/getting-started"));
        });
    }

    /// <summary>
    /// The card's description is its words in one line: the site's name, the page's name and the heading.
    /// </summary>
    [Test]
    public void TheDescription_IsTheCardsWords_InOneLine() =>
        Assert.That(GettingStarted().Description,
            Is.EqualTo("louis-agent: Getting started. Your agent, in two lines."));

    /// <summary>A title without the site's name is refused, since the card would print the whole title.</summary>
    [Test]
    public void ATitleWithoutTheSitesName_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() =>
            PageCardWords.Of("Getting started", "louis-agent", "Your agent.", "framework.louis-agent.dev"));

        Assert.That(refused!.Message, Does.Contain("Getting started").And.Contain("louis-agent"));
    }
}
