using LouisAgent.Choices;
using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using static LouisAgent.Diagrams.Svg.SvgWriter;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence's block: a glass frame with the accent along its top, its kind in a pill with its label
/// beside it, and each other case under a dashed divider with "else" in a quiet pill.
/// </summary>
/// <remarks>
/// A frame is drawn in two goes, since the lifelines run between them: its panel under the lifelines, and its tab
/// and dividers over them. The panel stands on a cover of the ground's colour, so an outer frame's glass doesn't
/// show through an inner one twice. A pill is as wide as the layout counted for it
/// (<see cref="SequenceMeasures.KindWidth"/>), so a label always starts where the layout made room.
/// </remarks>
internal static class SequenceFrames
{
    /// <summary>A frame's corner, how far in its accent starts along its top, and how thick the accent is.</summary>
    private const double Corner = 14, EdgeIn = 28, Edge = 1.6;

    /// <summary>
    /// A pill: how far under the frame's top or a divider it stands and how tall it is, where its word stands,
    /// and where a label stands beside it and how far after it.
    /// </summary>
    private const double PillTop = 10, PillHeight = 20, WordDrop = 24, LabelDrop = 24.5, LabelGap = 10;

    private static readonly SequenceMeasures Measures = SequenceMeasures.Marquee;

    /// <summary>A frame's panel, where the layout put it: the cover, the glass and the accent along its top.</summary>
    public static void Panel(SvgWriter writer, PlacedBlock block) => writer
        .Box(block.X, block.Y, block.W, block.H, Corner, "seq-cover")
        .Box(block.X, block.Y, block.W, block.H, Corner, "mrq-glass")
        .Box(block.X + EdgeIn, block.Y - 0.5, Math.Max(0, block.W - 2 * EdgeIn), Edge, 0, "mrq-edge");

    /// <summary>
    /// A frame's tab, its kind in a pill and its label, and for each other case a divider across the frame with
    /// "else" in a quiet pill and the case's label.
    /// </summary>
    public static void Tabs(SvgWriter writer, PlacedBlock block)
    {
        var kind = block.Block.Block;
        Tab(writer, block.X, block.Y, kind.Wire(), Measures.KindWidth(kind), "seq-kind", "mrq-code", block.Lines);
        foreach (var @case in block.Cases)
        {
            writer.Path($"M{N(block.X)} {N(@case.Y)}H{N(block.X + block.W)}", "seq-divide", false);
            Tab(writer, block.X, @case.Y, SequenceMeasures.ElseWord, Measures.WordsWidth([SequenceMeasures.ElseWord]),
                "seq-else", "seq-else-word", @case.Lines);
        }
    }

    /// <summary>A pill with a word in it, a little inside the frame, and a label's lines beside it.</summary>
    private static void Tab(SvgWriter writer, double x, double y, string word, double wide, string pill,
        string look, IReadOnlyList<string> lines)
    {
        double left = x + Measures.TabLeft;
        writer.Box(left, y + PillTop, wide, PillHeight, PillHeight / 2, pill)
            .Text(left + wide / 2, y + WordDrop, word, look);
        for (int at = 0; at < lines.Count; at++)
            writer.Text(left + wide + LabelGap, y + LabelDrop + at * Measures.LineStep, lines[at], "seq-tab",
                "start");
    }
}
