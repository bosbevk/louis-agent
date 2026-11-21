using LouisAgent.Web.Components.Site.Showroom.Mock.Menu;
using LouisAgent.Web.Components.Site.Showroom.Mock.Places;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock;

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

    /// <summary>
    /// A card with a photo (Louis: "like having a real image of the room") shows it first, named by the card's title.
    /// </summary>
    [Test]
    public void ACardWithAPhoto_ShowsItFirst()
    {
        using var ctx = new BunitContext();

        var card = ctx.Render<MockCard>(parameters => parameters
            .Add(place => place.Path, "/bread/rye")
            .Add(place => place.Title, "Rye")
            .Add(place => place.Photo, "images/rye.webp")).Find("article.mock-card");

        var photo = card.FirstElementChild!;
        Assert.That((card.ClassList.Contains("has-photo"), photo.LocalName, photo.ClassName,
                photo.GetAttribute("src"), photo.GetAttribute("alt")),
            Is.EqualTo((true, "img", "mock-card-photo", "images/rye.webp", "Rye")));
    }

    /// <summary>A section with a photo shows it beside its words, named by the section's heading.</summary>
    [Test]
    public void ASectionWithAPhoto_ShowsItBesideItsWords()
    {
        using var ctx = new BunitContext();

        var section = ctx.Render<MockSection>(parameters => parameters
            .Add(place => place.Path, "/bread")
            .Add(place => place.Heading, "Our bread")
            .Add(place => place.Photo, "images/bread.webp")
            .Add(place => place.ChildContent, "<p class=\"words\">Baked at dawn.</p>")).Find("section.mock-section");

        Assert.Multiple(() =>
        {
            Assert.That(section.ClassList, Does.Contain("has-photo"));
            Assert.That(section.QuerySelector(".mock-section-photo img")!.GetAttribute("src"),
                Is.EqualTo("images/bread.webp"));
            Assert.That(section.QuerySelector(".mock-section-photo img")!.GetAttribute("alt"), Is.EqualTo("Our bread"));
            Assert.That(section.QuerySelector(".mock-section-words .words"), Is.Not.Null);
        });
    }

    /// <summary>
    /// A real menu (Louis: "like having a real menu"): its courses, each dish with its price and a few words, the
    /// price set off by a dotted leader as a printed menu sets it.
    /// </summary>
    [Test]
    public void AMenu_ListsItsCourses_AndEachDishWithItsPrice()
    {
        using var ctx = new BunitContext();

        var menu = ctx.Render<MockMenu>(parameters => parameters
            .Add(m => m.ChildContent, builder =>
            {
                builder.OpenComponent<MockCourse>(0);
                builder.AddComponentParameter(1, nameof(MockCourse.Name), "Bread");
                builder.AddComponentParameter(2, nameof(MockCourse.ChildContent), (RenderFragment)(dishes =>
                {
                    dishes.OpenComponent<MockDish>(0);
                    dishes.AddComponentParameter(1, nameof(MockDish.Name), "Rye");
                    dishes.AddComponentParameter(2, nameof(MockDish.Price), "R45");
                    dishes.AddComponentParameter(3, nameof(MockDish.ChildContent), (RenderFragment)(words =>
                        words.AddContent(0, "Dark and sour.")));
                    dishes.CloseComponent();
                }));
                builder.CloseComponent();
            })).Find(".mock-menu");

        Assert.Multiple(() =>
        {
            Assert.That(menu.QuerySelector(".mock-course h4")!.TextContent, Is.EqualTo("Bread"));
            Assert.That(menu.QuerySelector(".mock-course li.mock-dish .mock-dish-name")!.TextContent,
                Is.EqualTo("Rye"));
            Assert.That(menu.QuerySelector(".mock-dish .mock-dish-leader"), Is.Not.Null);
            Assert.That(menu.QuerySelector(".mock-dish .mock-dish-price")!.TextContent, Is.EqualTo("R45"));
            Assert.That(menu.QuerySelector(".mock-dish .mock-dish-words")!.TextContent, Is.EqualTo("Dark and sour."));
        });
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
