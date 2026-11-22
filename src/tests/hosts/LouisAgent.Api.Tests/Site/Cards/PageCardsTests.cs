using LouisAgent.Api.Site.Cards;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Api.Tests.Site.Cards;

/// <summary>
/// A page's card is found by where its file is (F169.6): a picture named for the page's route in
/// <c>social-cards/</c>, with a text file beside it that says what it shows. Anything less is no card, so the page
/// wears the site's own picture.
/// </summary>
public class PageCardsTests
{
    private string _root = "";

    [SetUp]
    public void MakeTheWebRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "page-cards-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "social-cards", "guides"));
    }

    [TearDown]
    public void RemoveTheWebRoot() => Directory.Delete(_root, recursive: true);

    /// <summary>A file in the web root, written with these words.</summary>
    private void Write(string path, string text) => File.WriteAllText(Path.Combine(_root, path), text);

    /// <summary>The cards of the web root made for this test.</summary>
    private PageCards Cards() => new(new PhysicalFileProvider(_root));

    /// <summary>A picture with its words beside it is the page's card, the words trimmed of their line end.</summary>
    [Test]
    public void APictureAndItsWords_AreTheCard()
    {
        Write("social-cards/getting-started.png", "png");
        Write("social-cards/getting-started.txt", "louis-agent: Getting started. Your agent, in two lines.\n");

        var card = Cards().For("/getting-started");

        Assert.That(card, Is.EqualTo(new PageCard("social-cards/getting-started.png",
            "louis-agent: Getting started. Your agent, in two lines.")));
    }

    /// <summary>A picture with no words is no card, so a picture never shows with another's words.</summary>
    [Test]
    public void APictureWithNoWords_IsNoCard()
    {
        Write("social-cards/features.png", "png");

        Assert.That(Cards().For("/features"), Is.Null);
    }

    /// <summary>A page with neither file has no card of its own.</summary>
    [Test]
    public void NoFiles_IsNoCard() => Assert.That(Cards().For("/examples"), Is.Null);

    /// <summary>The root wears the site's own picture, even when a file is named for it.</summary>
    [Test]
    public void TheRoot_HasNoCard()
    {
        Write("social-cards/.png", "png");
        Write("social-cards/.txt", "words");

        Assert.That(Cards().For("/"), Is.Null);
    }

    /// <summary>A route of two parts looks in a folder of the first part's name.</summary>
    [Test]
    public void ARouteOfTwoParts_LooksInAFolder()
    {
        Write("social-cards/guides/install.png", "png");
        Write("social-cards/guides/install.txt", "Install");

        Assert.That(Cards().For("/guides/install")?.Picture, Is.EqualTo("social-cards/guides/install.png"));
    }
}
