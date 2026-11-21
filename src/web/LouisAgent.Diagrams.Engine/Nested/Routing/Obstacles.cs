using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Nested.Placed;

namespace LouisAgent.Diagrams.Engine.Nested.Routing;

/// <summary>
/// What stands in the way of a state's line (F134.7), and whether a way is clear of it.
/// </summary>
internal static class Obstacles
{
    /// <summary>How near a box, and a pin, a line may pass.</summary>
    private const double BoxRoom = 4, PinRoom = 3;

    /// <summary>
    /// Where a box's words stand under its top, how tall the line of its name is, and where its note starts.
    /// </summary>
    private const double WordsTop = 12, WordsHeight = 26, NoteTop = 38;

    /// <summary>
    /// Everything a line to this pin may not touch: every box that doesn't hold the pin's box, every other pin, and
    /// the words of the boxes it passes through.
    /// </summary>
    public static IReadOnlyList<IBox> For(IReadOnlyList<PlacedBox> between, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins, PlacedPin own, IGeometry geometry) =>
    [
        .. boxes.Except(between).Select(box => box.Box.Grown(BoxRoom)),
        .. pins.Where(pin => pin != own).Select(pin => pin.Box.Grown(PinRoom)),
        .. between.SelectMany(box => Words(box, geometry))
    ];

    /// <summary>Whether a way of level and upright stretches touches none of what stands in it.</summary>
    public static bool Clear(IReadOnlyList<IPoint> points, IReadOnlyList<IBox> blocks) =>
        !points.Zip(points.Skip(1)).Any(stretch =>
            blocks.Any(block => block.CrossedBy(stretch.First, stretch.Second, 0)));

    /// <summary>Where a box's own words stand: its name and chip, the words at its right, and its note.</summary>
    private static IEnumerable<IBox> Words(PlacedBox placed, IGeometry geometry)
    {
        var box = placed.Component;
        double left = placed.Box.Left + NestedSizes.NameIn, top = placed.Box.Top + WordsTop;
        double name = NestedSizes.NameWidth(box.Name) + NestedSizes.ChipAfterName(box.Kind);
        yield return geometry.Box(left, top, name, WordsHeight);

        if (box.Aside is { } aside)
        {
            double width = NestedSizes.WordsWidth(aside);
            yield return geometry.Box(placed.Box.Right - NestedSizes.AsideIn - width, top, width, WordsHeight);
        }

        if (placed.NoteLines.Count == 0) yield break;
        double longest = placed.NoteLines.Max(NestedSizes.WordsWidth);
        yield return geometry.Box(left, placed.Box.Top + NoteTop, longest,
            placed.NoteLines.Count * NestedSizes.NoteLine);
    }
}
