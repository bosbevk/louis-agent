using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Cards;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// A deck of cards that open (F113): the cards inside become a row of summary buttons with no story shown; a click
/// opens that card's story under the row, a second click closes it, another card swaps it in; and the open state is
/// pinned to its golden.
/// </summary>
public class CardDeckTests
{
    /// <summary>Two cards: Puls with a summary, and one with its title alone.</summary>
    private static RenderFragment TwoCards() => builder =>
    {
        builder.OpenComponent<Card>(0);
        builder.AddComponentParameter(1, nameof(Card.Title), "Puls");
        builder.AddComponentParameter(2, nameof(Card.Summary), "A South African assistant.");
        builder.AddComponentParameter(3, nameof(Card.ChildContent),
            (RenderFragment)(b => b.AddMarkupContent(0, "<p>Puls hands each task to a specialist.</p>")));
        builder.CloseComponent();
        builder.OpenComponent<Card>(4);
        builder.AddComponentParameter(5, nameof(Card.Title), "The guide");
        builder.AddComponentParameter(6, nameof(Card.ChildContent),
            (RenderFragment)(b => b.AddMarkupContent(0, "<p>The one in the corner.</p>")));
        builder.CloseComponent();
    };

    private static IRenderedComponent<CardDeck> Render(BunitContext ctx) =>
        ctx.Render<CardDeck>(parameters => parameters.Add(deck => deck.Columns, 2).AddChildContent(TwoCards()));

    /// <summary>The row shows every card's title and summary, and no story until a click.</summary>
    [Test]
    public void TheRow_ShowsTheSummaries_AndNoStory()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.card-summary"), Has.Count.EqualTo(2));
            Assert.That(cut.FindAll(".card-title").Select(t => t.TextContent),
                Is.EqualTo(new[] { "Puls", "The guide" }));
            Assert.That(cut.FindAll(".card-summary-text"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".card-detail"), Is.Empty);
            Assert.That(cut.Find(".card-deck-row").GetAttribute("style"), Is.EqualTo("--cols: 2"));
        });
    }

    /// <summary>A click opens that card's story; a second click closes it; the other card swaps its story in.</summary>
    [Test]
    public void AClick_OpensTheStory_AndAgainClosesIt()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.FindAll("button.card-summary")[0].Click();
        string first = cut.Find(".card-detail-body").TextContent;
        bool marked = cut.FindAll("button.card-summary")[0].ClassList.Contains("open");
        cut.FindAll("button.card-summary")[1].Click();
        string second = cut.Find(".card-detail-body").TextContent;
        cut.FindAll("button.card-summary")[1].Click();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Puls hands each task to a specialist."));
            Assert.That(marked, Is.True);
            Assert.That(second, Is.EqualTo("The one in the corner."));
            Assert.That(cut.FindAll(".card-detail"), Is.Empty);
        });
    }

    /// <summary>
    /// Every card is an area the guide can open (F118, Louis: "the agent should be able to click this also to show
    /// people"): its button carries an id from its title, or the one the site gives, and is marked as a card, so the
    /// widget can find it, open it and light it up.
    /// </summary>
    [Test]
    public void EveryCard_IsNamed_AndMarkedAsACard()
    {
        using var ctx = new BunitContext();
        RenderFragment cards = builder =>
        {
            builder.OpenComponent<Card>(0);
            builder.AddComponentParameter(1, nameof(Card.Title), "The coding agent");
            builder.CloseComponent();
            builder.OpenComponent<Card>(2);
            builder.AddComponentParameter(3, nameof(Card.Title), "Puls");
            builder.AddComponentParameter(4, nameof(Card.Id), "puls-the-assistant");
            builder.CloseComponent();
        };

        var cut = ctx.Render<CardDeck>(parameters => parameters.AddChildContent(cards));

        var buttons = cut.FindAll("button.card-summary");
        Assert.Multiple(() =>
        {
            Assert.That(buttons.Select(b => b.Id), Is.EqualTo(new[] { "the-coding-agent", "puls-the-assistant" }));
            Assert.That(buttons.All(b => b.HasAttribute("data-card")), Is.True);
            Assert.That(buttons.All(b => b.HasAttribute("data-piece")), Is.True,
                "a piece the guide can select (F127.7, Louis: \"the agent must be able to select each one\")");
        });
    }

    /// <summary>The open state matches the golden file.</summary>
    [Test]
    public void TheOpenDeck_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.FindAll("button.card-summary")[0].Click();

        Snapshots.Match("Components/Site/CardDeck.html", cut.Markup);
    }
}
