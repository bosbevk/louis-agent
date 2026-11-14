using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

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
    /// <summary>A frame's corner, and how far in its accent starts along its top.</summary>
    private const double Corner = 14, EdgeIn = 28;

    /// <summary>
    /// A pill: how far under the frame's top or a divider it stands and how tall it is, where its word stands,
    /// and where a label stands beside it and how far after it.
    /// </summary>
    private const double PillTop = 10, PillHeight = 20, WordDrop = 24, LabelDrop = 24.5, LabelGap = 10;

    private static readonly SequenceMeasures Measures = SequenceMeasures.Marquee;

    /// <summary>A frame's panel, where the layout put it: the cover, the glass and the accent along its top.</summary>
    public static void Panel(SvgWriter writer, PlacedBlock block, IGeometry geometry)
    {
        writer.Box(block.Box, Corner, "seq-cover").Box(block.Box, Corner, "mrq-glass");
        PictureParts.Accent(writer, geometry, block.Box, EdgeIn);
    }

    /// <summary>
    /// A frame's tab, its kind in a pill and its label, and for each other case a divider across the frame with
    /// "else" in a quiet pill and the case's label.
    /// </summary>
    public static void Tabs(SvgWriter writer, PlacedBlock block, IGeometry geometry)
    {
        var kind = block.Block.Block;
        var frame = block.Box;
        double left = frame.Left + Measures.TabLeft;
        var tab = geometry.Box(left, frame.Top + PillTop, Measures.KindWidth(kind), PillHeight);
        Tab(writer, tab, frame.Top, kind.Wire(), "seq-kind", "mrq-code", block.Lines);
        foreach (var @case in block.Cases)
        {
            writer.Path(PathWriter.Level(frame.Left, frame.Right, @case.Y), "seq-divide", false);
            double wide = Measures.WordsWidth([SequenceMeasures.ElseWord]);
            var other = geometry.Box(left, @case.Y + PillTop, wide, PillHeight);
            Tab(writer, other, @case.Y, SequenceMeasures.ElseWord, "seq-else", "seq-else-word", @case.Lines);
        }
    }

    /// <summary>
    /// A pill with a word in it, a little inside the frame and under a height, and a label's lines beside it.
    /// </summary>
    private static void Tab(SvgWriter writer, IBox pill, double y, string word, string look, string words,
        IReadOnlyList<string> lines)
    {
        writer.Box(pill, pill.HalfHeight, look).Text(pill.Middle.X, y + WordDrop, word, words);
        for (int at = 0; at < lines.Count; at++)
            writer.Text(pill.Right + LabelGap, y + LabelDrop + at * Measures.LineStep, lines[at], "seq-tab", "start");
    }
}
