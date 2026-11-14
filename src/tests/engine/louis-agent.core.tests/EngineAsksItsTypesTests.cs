using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The diagram engine asks its types (CL65.6, ADR 0123, Louis: "we cannot write spaghetti code"): a box knows its
/// edges, a face how wide words are, and a path writer a path's text, so no layout or drawing works those out
/// where it stands.
/// </summary>
/// <remarks>
/// A ratchet, as the file sizes are held. Each list names the files that still broke the rule when the types
/// were built. A test fails for a file new to its list, and for a listed file that no longer breaks the rule,
/// so a list only shrinks, and the work is done when all four are empty.
/// </remarks>
public partial class EngineAsksItsTypesTests
{
    private const string Engine = "src/web/louis-agent.diagrams.engine";

    /// <summary>What to do about a list that no longer matches the code.</summary>
    private const string Hint = "a file new to the list broke the rule: ask the type (ADR 0123). " +
                                "A file that left it: take it off the list.";

    /// <summary>The folders of the types themselves, where the sums are meant to be.</summary>
    private static readonly string[] TheTypes = ["/Geometry/", "/Words/", "/Svg/Paths/"];

    /// <summary>
    /// A record that is a box by four numbers. It finds "double X, double Y, double W, double H" and the same
    /// with <c>Width</c> and <c>Height</c>, over several lines too, and nothing in "double X, double Y, double H".
    /// </summary>
    [GeneratedRegex(@"double X,\s*double Y,\s*double (?:W|Width),\s*double (?:H|Height)\b",
        RegexOptions.NonBacktracking)]
    private static partial Regex BoxByNumbers();

    /// <summary>
    /// A box's edge or half its size, worked out. It finds "node.X + node.W", "box.Top + box.Height" and
    /// "node.W / 2", and nothing in "x + 4" or "width / 2", where no box is named.
    /// </summary>
    [GeneratedRegex(@"\.(?:X|Left) \+ [\w.]*\.?(?:W|Width)\b|\.(?:Y|Top) \+ [\w.]*\.?(?:H|Height)\b" +
                    @"|\.(?:W|H|Width|Height) / 2\b", RegexOptions.NonBacktracking)]
    private static partial Regex BoxSum();

    /// <summary>
    /// Words measured by their length. It finds "name.Length * NameChar" and "Max(line => line.Length) * Char",
    /// and nothing in "lines.Count * LineStep".
    /// </summary>
    [GeneratedRegex(@"\.Length\)? \* ", RegexOptions.NonBacktracking)]
    private static partial Regex WordsByLength();

    /// <summary>
    /// A path built as text. It finds <c>$"M{N(x)} …"</c>, <c>$"M {N(x)} …"</c>, <c>$"M0 {N(top)}"</c> and
    /// <c>"M" + string.Join(…)</c>, and nothing in <c>"Marquee"</c>.
    /// </summary>
    [GeneratedRegex("""\$"M ?[{\d]|"M" \+""", RegexOptions.NonBacktracking)]
    private static partial Regex PathAsText();

    /// <summary>The engine's files that still hold a shape, outside the types' own folders, by path under it.</summary>
    private static List<string> Holding(Regex shape) =>
    [
        .. ProjectSources.Of(Engine).Where(source => !TheTypes.Any(source.Path.Contains))
            .Where(source => shape.IsMatch(source.Text))
            .Select(source => source.Path[(Engine.Length + 1)..]).Order(StringComparer.Ordinal)
    ];

    /// <summary>The records that are still a box by four numbers.</summary>
    private static readonly string[] BoxesByNumbers =
    [
        "Cards/Placed/PlacedCard.cs",
        "Components/Placed/Keys/PlacedKey.cs",
        "Components/Placed/PlacedBox.cs",
        "Components/Placed/PlacedNeighbour.cs",
        "Components/Placed/PlacedPill.cs",
        "Components/Routing/Block.cs",
        "Sequence/Placed/PlacedBlock.cs",
        "Sequence/Placed/PlacedHead.cs",
        "Sequence/SequenceMeasures.cs",
        "Tickets/Placed/PlacedPill.cs"
    ];

    /// <summary>The files that still work out a box's edge or half its size.</summary>
    private static readonly string[] BoxSums =
    [
        "Cards/OverviewLayout.cs",
        "Components/ComponentLayout.cs",
        "Components/Routing/Obstacles.cs",
        "Components/Routing/StateRoutes.cs",
        "Sequence/SequenceRows.cs",
        "Svg/Renderers/Parts/Lifelines/SequenceFrames.cs",
        "Svg/Renderers/Parts/Lifelines/SequenceHeads.cs",
        "Svg/Renderers/Parts/Nesting/ComponentsRenderer.cs",
        "Svg/Renderers/Parts/Overview/OverviewCards.cs",
        "Svg/Renderers/Parts/Overview/OverviewDrawing.cs",
        "Svg/Renderers/TicketRenderer.cs"
    ];

    /// <summary>The files that still measure words by their length.</summary>
    private static readonly string[] WordsMeasured =
    [
        "Cards/Sizes/CardSizes.cs",
        "Components/ComponentSizes.cs",
        "Components/NeighbourLayout.cs",
        "Keys/KeyRows.cs",
        "Sequence/SequenceMeasures.cs",
        "Svg/Renderers/Parts/Overview/OverviewCards.cs",
        "Svg/Renderers/Parts/PictureBuilder.cs",
        "Tickets/TicketLayout.cs"
    ];

    /// <summary>The files that still build a path as text.</summary>
    private static readonly string[] PathsAsText =
    [
        "Components/Placed/Lines/LinePath.cs",
        "Svg/Renderers/Parts/Lifelines/SequenceFrames.cs",
        "Svg/Renderers/Parts/Lifelines/SequenceSteps.cs",
        "Svg/Renderers/Parts/Nesting/ComponentPins.cs",
        "Svg/Renderers/Parts/Nesting/ComponentsKey.cs",
        "Svg/Renderers/Parts/Overview/OverviewDrawing.cs",
        "Svg/Renderers/Parts/Overview/OverviewRails.cs",
        "Svg/Renderers/Parts/PictureBuilder.cs",
        "Svg/Renderers/Parts/Structure/CompositeKey.cs",
        "Svg/Renderers/Parts/Types/ClassesKey.cs",
        "Svg/Renderers/TicketRenderer.cs"
    ];

    /// <summary>A place and a size are a box, never four numbers side by side.</summary>
    [Test]
    public void NoRecord_IsABoxByFourNumbers() =>
        Assert.That(Holding(BoxByNumbers()), Is.EqualTo(BoxesByNumbers), Hint);

    /// <summary>A box is asked for its edge and its middle.</summary>
    [Test]
    public void NoFile_WorksOutABoxsEdge() => Assert.That(Holding(BoxSum()), Is.EqualTo(BoxSums), Hint);

    /// <summary>A face is asked how wide words are.</summary>
    [Test]
    public void NoFile_MeasuresWordsByTheirLength() =>
        Assert.That(Holding(WordsByLength()), Is.EqualTo(WordsMeasured), Hint);

    /// <summary>A path writer writes a path.</summary>
    [Test]
    public void NoFile_BuildsAPathAsText() => Assert.That(Holding(PathAsText()), Is.EqualTo(PathsAsText), Hint);
}
