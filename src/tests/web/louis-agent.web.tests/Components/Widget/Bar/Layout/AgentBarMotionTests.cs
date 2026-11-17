namespace LouisAgent.Web.Tests.Components.Widget.Bar.Layout;

/// <summary>
/// F134.3 and F134.4: nothing the bar shows pops in or snaps from one spot to another. Each thing eases, and each
/// holds still for a visitor who asked for less motion. Read from the styles and the caption's module, since how a
/// thing moves is theirs to say.
/// </summary>
public class AgentBarMotionTests
{
    private static readonly string Bar =
        Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Components/Widget/Bar");

    /// <summary>A file of the bar's, its line ends folded.</summary>
    private static string Read(string path) => File.ReadAllText(Path.Combine(Bar, path)).ReplaceLineEndings("\n");

    private static string BarStyles => Read("AgentBar.razor.scss");
    private static string ChipsStyles => Read("Chips/BarChips.razor.scss");
    private static string CaptionStyles => Read("Caption/AgentCaption.razor.scss");
    private static string CaptionModule => Read("Caption/AgentCaption.razor.js");
    private static string MotionModule => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/wwwroot/js/caption-motion.js")).ReplaceLineEndings("\n");

    /// <summary>The rules a file keeps for a visitor who asked for less motion; empty when it has none.</summary>
    private static string LessMotion(string styles)
    {
        int start = styles.IndexOf("@media (prefers-reduced-motion: reduce) {", StringComparison.Ordinal);
        return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    /// <summary>The chips step away and come back by easing, never by being taken off the page.</summary>
    [Test]
    public void TheChips_EaseAwayAndBack()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChipsStyles, Does.Contain(".agent-chips.is-away {"));
            Assert.That(ChipsStyles, Does.Contain("transition: opacity .35s"));
        });
    }

    /// <summary>
    /// The caption glides to a place its answer lights after its first words, from where it stood. The glide is laid
    /// over where the caption stands, so it still keeps exactly with the page while the page scrolls to the place.
    /// Since F154 the caption's entry is its motion module's grow, added to whatever moves it, so it never holds the
    /// glide still, and the styles keep no entry of their own.
    /// </summary>
    [Test]
    public void TheCaption_GlidesToAPlaceLitAfterItsFirstWords()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CaptionModule, Does.Contain("caption.animate("));
            Assert.That(CaptionModule, Does.Contain("\"(prefers-reduced-motion: reduce)\""));
            Assert.That(MotionModule, Does.Contain("composite: \"add\""));
            Assert.That(CaptionStyles, Does.Not.Contain("animation: rise"));
        });
    }

    /// <summary>The talk's total and a selected piece's chip rise into the bar as the caption does.</summary>
    [Test]
    public void TheTotal_AndThePiecesChip_RiseIn() =>
        Assert.That(BarStyles, Does.Contain(".agent-total, .agent-bar > .piece-chip { animation: rise"));

    /// <summary>
    /// The orb's light stays inside the bar: the row has 14 pixels of room around the ball for its rings, which makes
    /// the bar 104 high round a ball of 76 as the design draws it (F151.6), and it cuts whatever glow would still
    /// reach past its edge.
    /// </summary>
    [Test]
    public void TheOrbsLight_StaysInsideTheBar()
    {
        int start = BarStyles.IndexOf("\n.agent-row {", StringComparison.Ordinal);
        string row = start < 0 ? "" : BarStyles[start..BarStyles.IndexOf("\n}", start + 1, StringComparison.Ordinal)];

        Assert.That(row, Does.Contain("overflow: clip;")
            .And.Contain("padding: var(--agent-air) 28px var(--agent-air) var(--agent-air);"));
    }

    /// <summary>
    /// FX173: while the bar is closed, its chips take six places round the orb (Louis: "space for 6", "straight not
    /// at an angle", "only when it is closed", "like this"): the first over the orb, the next two over the site's name
    /// on the left and its note on the right, the next two under them, and the note that the box is there under the
    /// orb, each on one line; the bar stands higher by a chip to hold the lowest. Open, they stand in their row over
    /// the box as before.
    /// </summary>
    [Test]
    public void WhileClosed_TheChipsTakeSixPlacesRoundTheOrb()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChipsStyles, Does.Contain(".agent-bar.is-closed .agent-chips {"));
            Assert.That(ChipsStyles, Does.Contain("> button:nth-of-type(1) { grid-area: 1 / 2; justify-self: center; }"));
            Assert.That(ChipsStyles, Does.Contain("> button:nth-of-type(2) { grid-area: 2 / 1; justify-self: end; align-self: start; }"));
            Assert.That(ChipsStyles, Does.Contain("> button:nth-of-type(3) { grid-area: 2 / 3; justify-self: start; align-self: start; }"));
            Assert.That(ChipsStyles, Does.Contain("> button:nth-of-type(4) { grid-area: 3 / 1; justify-self: end; align-self: end; }"));
            Assert.That(ChipsStyles, Does.Contain("> button:nth-of-type(5) { grid-area: 3 / 3; justify-self: start; align-self: end; }"));
            Assert.That(ChipsStyles, Does.Contain(".agent-bar.is-closed .agent-chips-note { grid-area: 4 / 2; justify-self: center; }"));
            Assert.That(BarStyles,
                Does.Contain(".agent-bar.is-closed { bottom: calc(22px + var(--agent-chip) + var(--agent-chip-gap)); }"));
            // A little room between the orb and the chips round it (Louis: "increase spacing a tad").
            Assert.That(BarStyles, Does.Contain("--agent-chip-gap: 8px;"));
            // With no brand beside the orb, the chips stand close round it, the same gap on every side (Louis: "then
            // we can space better", "you know what i mean these gaps").
            Assert.That(ChipsStyles, Does.Contain(
                "grid-template-columns: 1fr calc(var(--agent-ball) + 2 * var(--agent-air) + 2 * var(--agent-chip-gap)) 1fr;"));
        });
    }

    /// <summary>
    /// FX173: the site's sides stand as high as the bar, over the chips above and below them, so they let the pointer
    /// through to the chips, and only a link in them takes it (Louis: "hover not wroking here").
    /// </summary>
    [Test]
    public void TheSides_LetThePointerThroughToTheChips_ButTheirLinks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BarStyles, Does.Contain("pointer-events: none;"));
            Assert.That(BarStyles, Does.Contain(".agent-side ::deep a { pointer-events: auto; }"));
        });
    }

    /// <summary>A visitor who asked for less motion gets none of it: each thing is simply where it belongs.</summary>
    [Test]
    public void WithLessMotionAsked_NothingMoves()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LessMotion(ChipsStyles), Does.Contain(".agent-chips"));
            Assert.That(LessMotion(BarStyles), Does.Contain(".agent-total"));
            Assert.That(LessMotion(CaptionStyles), Does.Contain(".agent-caption { animation: none; }"));
        });
    }
}
