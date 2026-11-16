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
    private static string CaptionStyles => Read("Caption/AgentCaption.razor.scss");
    private static string CaptionModule => Read("Caption/AgentCaption.razor.js");

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
            Assert.That(BarStyles, Does.Contain(".agent-chips.is-away {"));
            Assert.That(BarStyles, Does.Contain("transition: opacity .35s"));
        });
    }

    /// <summary>
    /// The caption glides to a place its answer lights after its first words, from where it stood. The glide is laid
    /// over where the caption stands, so it still keeps exactly with the page while the page scrolls to the place.
    /// The caption's entry lets go of it once it has played, or it would hold the glide still.
    /// </summary>
    [Test]
    public void TheCaption_GlidesToAPlaceLitAfterItsFirstWords()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CaptionModule, Does.Contain("caption.animate("));
            Assert.That(CaptionModule, Does.Contain("\"(prefers-reduced-motion: reduce)\""));
            Assert.That(CaptionStyles, Does.Contain("animation: rise .45s var(--web-spring, ease-out) backwards;"));
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

        Assert.That(row, Does.Contain("overflow: clip;").And.Contain("padding: 14px 25px 14px 14px;"));
    }

    /// <summary>A visitor who asked for less motion gets none of it: each thing is simply where it belongs.</summary>
    [Test]
    public void WithLessMotionAsked_NothingMoves()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LessMotion(BarStyles), Does.Contain(".agent-chips").And.Contain(".agent-total"));
            Assert.That(LessMotion(CaptionStyles), Does.Contain(".agent-caption { animation: none; }"));
        });
    }
}
