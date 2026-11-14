using LouisAgent.Web.Components.Staging.Decks;

namespace LouisAgent.Web.Tests.Components.Staging.Decks;

/// <summary>
/// F134.1: Louis picked the look where one demo stands forward as a window and the others wait behind it in depth.
/// The deck is that, for anything: it draws each item through the caller's template, marks the one forward, and says
/// which item the visitor asked for with an arrow, a key or a press on an item behind. The page owns which is forward.
/// </summary>
public class DeckTests
{
    private static readonly string[] Letters = ["a", "b", "c", "d", "e"];

    /// <summary>What the deck last asked to bring forward; none until it asks.</summary>
    private sealed class Asked
    {
        public int? Index { get; set; }
    }

    /// <summary>A deck of five letters with this one forward.</summary>
    private static IRenderedComponent<Deck<string>> Deck(BunitContext ctx, int forward, Asked asked) =>
        ctx.Render<Deck<string>>(parameters => parameters
            .Add(deck => deck.Items, Letters)
            .Add(deck => deck.Forward, forward)
            .Add(deck => deck.ForwardChanged, index => asked.Index = index)
            .Add(deck => deck.Item, letter => $"<b class=\"letter\">{letter}</b>")
            .Add(deck => deck.Label, "Letters")
            .Add(deck => deck.BeforeLabel, "The letter before")
            .Add(deck => deck.NextLabel, "The next letter"));

    /// <summary>
    /// Every item is drawn through the template, in order, each told how far from the middle it stands. One is
    /// forward, and what the others hold can't be reached or read until they are.
    /// </summary>
    [Test]
    public void EveryItemIsDrawn_OneForward_TheRestBehind()
    {
        using var ctx = new BunitContext();

        var deck = Deck(ctx, 1, new Asked());

        var items = deck.FindAll(".deck-item");
        Assert.Multiple(() =>
        {
            Assert.That(deck.Find(".deck").GetAttribute("aria-label"), Is.EqualTo("Letters"));
            Assert.That(items.Select(item => item.QuerySelector(".letter")!.TextContent), Is.EqualTo(Letters));
            Assert.That(items.Select(item => item.GetAttribute("style")),
                Is.EqualTo(new[]
                {
                    "--d: -1; --abs: 1; z-index: 4;", "--d: 0; --abs: 0; z-index: 5;",
                    "--d: 1; --abs: 1; z-index: 4;", "--d: 2; --abs: 2; z-index: 3;",
                    "--d: -2; --abs: 2; z-index: 3;",
                }));
            Assert.That(items.Select(item => item.ClassList.Contains("is-forward")),
                Is.EqualTo(new[] { false, true, false, false, false }));
            Assert.That(items.Select(item => item.GetAttribute("aria-current")),
                Is.EqualTo(new[] { null, "true", null, null, null }));
            Assert.That(items.Select(item => item.QuerySelector(".deck-item-in")!.HasAttribute("inert")),
                Is.EqualTo(new[] { true, false, true, true, true }));
        });
    }

    /// <summary>An arrow asks for the item beside the one forward, and comes round at either end.</summary>
    [TestCase(1, ".deck-next", 2)]
    [TestCase(1, ".deck-before", 0)]
    [TestCase(4, ".deck-next", 0)]
    [TestCase(0, ".deck-before", 4)]
    public void AnArrow_AsksForTheItemBeside(int forward, string arrow, int asksFor)
    {
        using var ctx = new BunitContext();
        var asked = new Asked();
        var deck = Deck(ctx, forward, asked);

        deck.Find(arrow).Click();

        Assert.That(asked.Index, Is.EqualTo(asksFor));
    }

    /// <summary>The arrows are buttons that say where they go, for a visitor who can't see them.</summary>
    [Test]
    public void TheArrows_SayWhereTheyGo()
    {
        using var ctx = new BunitContext();

        var deck = Deck(ctx, 0, new Asked());

        Assert.Multiple(() =>
        {
            Assert.That(deck.Find("button.deck-before").GetAttribute("aria-label"), Is.EqualTo("The letter before"));
            Assert.That(deck.Find("button.deck-next").GetAttribute("aria-label"), Is.EqualTo("The next letter"));
            Assert.That(deck.FindAll("button.deck-arrow svg.line-icon"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>The keyboard's arrows do what the deck's arrows do, and no other key does anything.</summary>
    [TestCase("ArrowRight", 3)]
    [TestCase("ArrowLeft", 1)]
    [TestCase("Enter", null)]
    public void TheArrowKeys_DoTheSame(string key, int? asksFor)
    {
        using var ctx = new BunitContext();
        var asked = new Asked();
        var deck = Deck(ctx, 2, asked);

        deck.Find(".deck").KeyDown(key);

        Assert.That(asked.Index, Is.EqualTo(asksFor));
    }

    /// <summary>A press on an item behind asks for it. A press on the one forward asks for nothing.</summary>
    [TestCase(3, 3)]
    [TestCase(1, null)]
    public void APressOnAnItemBehind_AsksForIt(int pressed, int? asksFor)
    {
        using var ctx = new BunitContext();
        var asked = new Asked();
        var deck = Deck(ctx, 1, asked);

        deck.FindAll(".deck-item")[pressed].Click();

        Assert.That(asked.Index, Is.EqualTo(asksFor));
    }
}
