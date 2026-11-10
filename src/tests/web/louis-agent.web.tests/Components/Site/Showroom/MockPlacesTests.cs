using LouisAgent.Web.Components.Site.Showroom.Mock;

namespace LouisAgent.Web.Tests.Components.Site.Showroom;

/// <summary>
/// F127.1: a mock website's places, each with its own address on the business's site (<c>/rooms/garden</c>), so its
/// agent can show one: a section with its heading, its words and its cards, and a card with its title and a note.
/// </summary>
public class MockPlacesTests
{
    /// <summary>A section carries its address, its heading, its words, then its cards in a row of their own.</summary>
    [Test]
    public void ASection_HasItsAddress_ItsHeading_ItsWords_AndItsCards()
    {
        using var ctx = new BunitContext();

        var section = ctx.Render<MockSection>(parameters => parameters
            .Add(place => place.Path, "/bread")
            .Add(place => place.Heading, "Our bread")
            .Add(place => place.ChildContent, "<p class=\"words\">Baked at dawn.</p>")
            .Add(place => place.Cards, "<article class=\"rye\">Rye</article>")).Find("section.mock-section");

        Assert.Multiple(() =>
        {
            Assert.That(section.GetAttribute("data-place"), Is.EqualTo("/bread"));
            Assert.That(section.QuerySelector("h3.mock-section-heading")!.TextContent, Is.EqualTo("Our bread"));
            Assert.That(section.QuerySelector(".words")!.TextContent, Is.EqualTo("Baked at dawn."));
            Assert.That(section.QuerySelector(".mock-cards > .rye"), Is.Not.Null);
        });
    }

    /// <summary>A section without cards draws no empty row for them.</summary>
    [Test]
    public void ASectionWithoutCards_DrawsNoRow()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<MockSection>(parameters => parameters
            .Add(place => place.Path, "/bread")
            .Add(place => place.Heading, "Our bread"));

        Assert.That(cut.FindAll(".mock-cards"), Is.Empty);
    }

    /// <summary>A card of a title and a price alone (a dish) draws no empty words under them.</summary>
    [Test]
    public void ACardWithoutWords_DrawsNone()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<MockCard>(parameters => parameters
            .Add(place => place.Path, "/bread/rye")
            .Add(place => place.Title, "Rye")
            .Add(place => place.Note, "R45"));

        Assert.That(cut.FindAll(".mock-card-words"), Is.Empty);
    }

    /// <summary>A card carries its address, its title, its note (a price) and its words.</summary>
    [Test]
    public void ACard_HasItsAddress_ItsTitle_ItsNote_AndItsWords()
    {
        using var ctx = new BunitContext();

        var card = ctx.Render<MockCard>(parameters => parameters
            .Add(place => place.Path, "/bread/rye")
            .Add(place => place.Title, "Rye")
            .Add(place => place.Note, "R45")
            .Add(place => place.ChildContent, "Dark and sour.")).Find("article.mock-card");

        Assert.That((card.GetAttribute("data-place"), card.QuerySelector(".mock-card-title")!.TextContent,
                card.QuerySelector(".mock-card-note")!.TextContent,
                card.QuerySelector(".mock-card-words")!.TextContent),
            Is.EqualTo(("/bread/rye", "Rye", "R45", "Dark and sour.")));
    }
}
