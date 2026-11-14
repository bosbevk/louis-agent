namespace LouisAgent.Web.Tests.Components.Staging.Decks;

/// <summary>
/// F134.1: on a wide screen the deck's items lean a little towards the mouse, as the stage did in the look Louis
/// picked, so the depth is felt and not only seen. The deck's module writes how far, as two numbers on the deck,
/// and the styles turn the items by them. No browser runs here, so these tests hold the two files to each other:
/// the names the module writes are the names the styles read. How it moves is looked at in a browser.
/// </summary>
public class DeckLeanTests
{
    private static readonly string Folder =
        Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Components/Staging/Decks");

    /// <summary>The deck's styles, their line ends made plain.</summary>
    private static string Styles => File.ReadAllText(Path.Combine(Folder, "Deck.razor.scss")).ReplaceLineEndings("\n");

    /// <summary>The deck's module.</summary>
    private static string Module => File.ReadAllText(Path.Combine(Folder, "Deck.razor.js"));

    /// <summary>The block a query opens in the styles, up to its closing brace. Empty when there is none.</summary>
    private static string Block(string query)
    {
        string styles = Styles;
        int start = styles.IndexOf(query + " {", StringComparison.Ordinal);
        return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    /// <summary>The rule for the items together on a narrow screen, where they are a row.</summary>
    private static string RowRule
    {
        get
        {
            string narrow = Block("@media (max-width: 900px)");
            int start = narrow.IndexOf(".deck-items {", StringComparison.Ordinal);
            return start < 0 ? "" : narrow[start..narrow.IndexOf('}', start)];
        }
    }

    /// <summary>
    /// The module says how far the items lean, up or down and sideways, and the styles turn them by those two
    /// numbers. With nothing said, they stand straight.
    /// </summary>
    [TestCase("--deck-lean-x", "rotateX(var(--deck-lean-x, 0deg))")]
    [TestCase("--deck-lean-y", "rotateY(var(--deck-lean-y, 0deg))")]
    public void TheModuleSaysHowFar_AndTheStylesTurnTheItems(string name, string turn)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain($"\"{name}\""), "the module writes it");
            Assert.That(Styles, Does.Contain(turn), "the styles turn by it");
        });
    }

    /// <summary>
    /// The lean follows a mouse, and goes back when the mouse leaves the deck. A finger is not followed: a touch is
    /// a swipe or a press.
    /// </summary>
    [Test]
    public void TheLeanFollowsAMouse_AndGoesBackWhenItLeaves()
    {
        string module = Module;

        Assert.Multiple(() =>
        {
            Assert.That(module, Does.Contain("\"pointermove\""));
            Assert.That(module, Does.Contain("\"pointerleave\""));
            Assert.That(module, Does.Contain("pointerType !== \"mouse\""), "only a mouse is followed");
        });
    }

    /// <summary>
    /// A visitor who asked for less motion gets none of it: the module leans nothing, and the styles ease nothing.
    /// On a narrow screen the items are a row, and a row never leans.
    /// </summary>
    [Test]
    public void LessMotion_AndANarrowScreen_GetNoLean()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("if (still() || "), "the module asks before it leans");
            Assert.That(Block("@media (prefers-reduced-motion: reduce)"), Does.Contain(".deck-items"));
            Assert.That(RowRule, Does.Contain("transform: none;"), "the row stands straight");
        });
    }
}
