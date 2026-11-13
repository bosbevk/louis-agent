using LouisAgent.Web.Components.Staging.Decks;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Components.Staging.Decks;

/// <summary>
/// F134.1: on a phone there is no room for depth, so the deck is a row the visitor swipes, one item at a time
/// (Louis's pick: "Swipe, one at a time"). The browser does the swiping. The deck's module only says which item a
/// swipe left in view, and brings an item into view when the page puts another forward. The page still owns which
/// is forward.
/// </summary>
public class DeckOnAPhoneTests
{
    private static readonly string[] Letters = ["a", "b", "c", "d", "e"];

    /// <summary>What the deck last asked to bring forward; none until it asks.</summary>
    private sealed class Asked
    {
        public int? Index { get; set; }
    }

    /// <summary>
    /// The deck's rules for a narrow screen: the block its narrow-screen query opens, up to its closing brace. Empty
    /// when the deck has no such block, so each test says which rule it misses.
    /// </summary>
    private static string PhoneRules
    {
        get
        {
            string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                "src/web/louis-agent.web/Components/Staging/Decks/Deck.razor.scss")).ReplaceLineEndings("\n");
            int start = styles.IndexOf("@media (max-width: 900px) {", StringComparison.Ordinal);

            return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start, StringComparison.Ordinal)];
        }
    }

    /// <summary>
    /// A deck of five letters with this one forward, in a browser that answers its module with nothing.
    /// </summary>
    private static IRenderedComponent<Deck<string>> Deck(BunitContext ctx, int forward, Asked asked)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<Deck<string>>(parameters => parameters
            .Add(deck => deck.Items, Letters)
            .Add(deck => deck.Forward, forward)
            .Add(deck => deck.ForwardChanged, index => asked.Index = index)
            .Add(deck => deck.Item, letter => $"<b class=\"letter\">{letter}</b>")
            .Add(deck => deck.Label, "Letters")
            .Add(deck => deck.BeforeLabel, "The letter before")
            .Add(deck => deck.NextLabel, "The next letter"));
    }

    /// <summary>What the deck asked its module, by the function's name.</summary>
    private static List<JSRuntimeInvocation> Calls(BunitContext ctx, string function) =>
        [.. ctx.JSInterop.Invocations.Where(call => call.Identifier == function)];

    /// <summary>
    /// The row is swiped sideways and stops on each item, one at a time, with the item in the middle.
    /// </summary>
    [Test]
    public void OnANarrowScreen_TheItemsAreARow_SwipedOneAtATime()
    {
        string rules = PhoneRules;

        Assert.Multiple(() =>
        {
            Assert.That(rules, Does.Contain("overflow-x: auto;"));
            Assert.That(rules, Does.Contain("scroll-snap-type: x mandatory;"));
            Assert.That(rules, Does.Contain("scroll-snap-align: center;"));
            Assert.That(rules, Does.Contain("scroll-snap-stop: always;"));
        });
    }

    /// <summary>
    /// Nothing stands in depth there: no perspective, no item moved, turned or dimmed by its distance, and no
    /// reflection. The arrows are gone, since the swipe does their work. The reflection is put back to its first
    /// value, <c>initial</c>: Chrome refuses <c>none</c> for it and then keeps the reflection, as the page showed.
    /// </summary>
    [Test]
    public void OnANarrowScreen_ThereIsNoDepth_AndThereAreNoArrows()
    {
        string rules = PhoneRules;

        Assert.Multiple(() =>
        {
            Assert.That(rules, Does.Contain("perspective: none;"));
            Assert.That(rules, Does.Contain("transform: none;"));
            Assert.That(rules, Does.Contain("filter: none;"));
            Assert.That(rules, Does.Contain("-webkit-box-reflect: initial;"));
            Assert.That(rules, Does.Contain(".deck-arrow { display: none; }"));
        });
    }

    /// <summary>The deck's module is asked to watch its row once the deck is drawn.</summary>
    [Test]
    public void TheDeck_HasItsRowWatched_WhenItDraws()
    {
        using var ctx = new BunitContext();

        var deck = Deck(ctx, 1, new Asked());

        deck.WaitForAssertion(() => Assert.That(Calls(ctx, "watch"), Has.Count.EqualTo(1)));
        Assert.That(ctx.JSInterop.Invocations.Where(call => call.Identifier == "import")
            .Select(call => call.Arguments[0]), Does.Contain(FrameworkScripts.Deck));
    }

    /// <summary>
    /// A swipe leaves another item in view, and the deck asks the page for it. An item that is forward already, or
    /// one the deck doesn't hold, asks for nothing.
    /// </summary>
    [TestCase(3, 3)]
    [TestCase(1, null)]
    [TestCase(7, null)]
    [TestCase(-1, null)]
    public async Task ASwipeToAnotherItem_AsksForIt(int inView, int? asksFor)
    {
        using var ctx = new BunitContext();
        var asked = new Asked();
        var deck = Deck(ctx, 1, asked);

        await deck.InvokeAsync(() => deck.Instance.Viewed(inView));

        Assert.That(asked.Index, Is.EqualTo(asksFor));
    }

    /// <summary>
    /// When the page puts another item forward, by a button elsewhere on it, the row is brought to that item. A
    /// redraw with the same one forward moves nothing.
    /// </summary>
    [Test]
    public void WhenAnotherItemComesForward_TheRowIsBroughtToIt()
    {
        using var ctx = new BunitContext();
        var deck = Deck(ctx, 1, new Asked());
        deck.WaitForAssertion(() => Assert.That(Calls(ctx, "watch"), Has.Count.EqualTo(1)));

        deck.Render(parameters => parameters.Add(letters => letters.Forward, 1));
        deck.Render(parameters => parameters.Add(letters => letters.Forward, 3));

        deck.WaitForAssertion(() => Assert.That(Calls(ctx, "bring").Select(call => call.Arguments[^1]),
            Is.EqualTo(new object[] { 3 })));
    }
}
