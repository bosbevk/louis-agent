using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Engine.Keys;

/// <summary>
/// What every kind's key is drawn with (CL65.6): its rule and heading, the row an item stands in, a sample's box
/// on the row's middle line, a sample line along it, and an item's words after its sample.
/// </summary>
/// <remarks>
/// Three kinds' keys each worked these out by sums of their own, the same sums, so the keys stood alike only
/// because the copies agreed. They are here once, on boxes, and a key says only what its samples are.
/// <see cref="KeyRows"/> places a key's items, and this draws what was placed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal static class KeyParts
{
    /// <summary>
    /// How tall the rule is, where the heading's words stand under it, and how far under a row's middle line an
    /// item's words do.
    /// </summary>
    private const double Rule = 1, HeadingLine = 22, WordDrop = 4.5;

    /// <summary>
    /// The start of a key: a thin rule across the picture at the key's top, in the kind's own look, and the
    /// heading under it at the left.
    /// </summary>
    public static SvgWriter Head(SvgWriter writer, IGeometry geometry, double top, double width, string rule) =>
        writer.Box(geometry.Box(0, top, width, Rule), 0, rule)
            .Text(0, top + HeadingLine, DiagramText.Format(DrawingMessage.KeyHeading), "pic-group", "start");

    /// <summary>An item's row: where its layout put it, as wide as its sample and a row tall.</summary>
    public static IBox Row(IGeometry geometry, double x, double y, double sample) =>
        geometry.Box(x, y, sample, KeyRows.Row);

    /// <summary>A sample's box, that wide and that tall: at its row's left, on the row's middle line.</summary>
    public static IBox Sample(IGeometry geometry, IBox row, double wide, double tall) =>
        geometry.Box(row.Left, 0, wide, tall).OnLine(row.Middle.Y);

    /// <summary>A sample line's path: along its row's middle line, from the row's left to its right.</summary>
    public static string Line(IBox row) => PathWriter.Level(row.Left, row.Right, row.Middle.Y);

    /// <summary>An item's words: after its sample, a little under the row's middle line.</summary>
    public static SvgWriter Words(SvgWriter writer, IBox row, string words, string cls) =>
        writer.Text(row.Right + KeyRows.Words, row.Middle.Y + WordDrop, words, cls, "start");
}
