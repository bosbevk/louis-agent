using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Nested.Placed;
using LouisAgent.Diagrams.Engine.Nested.Placed.Lines;
using LouisAgent.Diagrams.Engine.Nested.Routing;
using LouisAgent.Diagrams.Nested.Spec;

namespace LouisAgent.Diagrams.Engine.Nested;

/// <summary>
/// Places a nested picture (F134.7): every box inside the one that holds it, every pin on its box's top edge,
/// every neighbour beside the root, and the key. No place is written in a picture's JSON: it all comes from here.
/// </summary>
/// <remarks>
/// A layout as a pure function of data, over the Composite pattern's tree, walked once: a box is as wide as its own
/// words and the boxes it holds need, and the boxes it holds stretch to its inner width, stacked in the JSON's
/// order. So the same JSON always draws the same picture, and the layout is tested with no browser.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/composite">Composite (Refactoring.Guru)</seealso>
public static class NestedLayout
{
    /// <summary>The room the picture's title takes, one small line, above everything else.</summary>
    public const double TitleRoom = 32;

    /// <summary>
    /// The room above the root, between the root and a column of neighbours for their lines, and how far under a
    /// box's name and note its pill stands.
    /// </summary>
    private const double Top = 12, ColumnGap = 60, PillDrop = 4;

    /// <summary>
    /// The diagram, placed, its boxes and points made by a geometry: the one in use when none is told.
    /// </summary>
    public static PlacedNested Of(NestedSpec components, IGeometry? geometry = null)
    {
        geometry ??= Geometries.InUse;
        var around = components.Around ?? [];

        // The root stands after the column of what comes before it, when there is one. The key stands under that
        // column, so the column is never narrower than the key.
        double column = NeighbourLayout.ColumnWidth(around, NeighbourSide.Before);
        double before = column == 0 ? 0 : Math.Max(column, KeyLayout.Width);
        double left = before == 0 ? 0 : before + ColumnGap;
        var start = geometry.Point(left, TitleRoom + Top);
        var boxes = Boxes(components.Root, start, NestedSizes.Width(components.Root), 0, geometry).ToList();
        var root = boxes[0].Box;

        var neighbours = NeighbourLayout.Place(around, boxes, before, root.Right + ColumnGap, geometry);
        var pins = boxes.SelectMany(box => Pins(box, geometry)).ToList();
        List<PlacedLine> lines =
        [
            .. neighbours.SelectMany(neighbour => NeighbourLayout.Lines(neighbour, boxes, geometry)),
            .. StateRoutes.Of(boxes, pins, geometry)
        ];
        var key = KeyLayout.Of(components, root, neighbours, geometry);

        // The picture reaches as far right and as far down as the furthest thing in it.
        IBox[] all = [.. neighbours.Select(neighbour => neighbour.Box), root, key.Box];
        double width = all.Max(box => box.Right), height = all.Max(box => box.Bottom);
        return new PlacedNested(width, height, boxes, pins, neighbours, lines, key);
    }

    /// <summary>
    /// A box at its place, then every box it holds, row by row: in one column each as wide as the room inside, under
    /// the one before it; in more, side by side, each as wide as a column, a row under the tallest of the row before.
    /// </summary>
    private static IEnumerable<PlacedBox> Boxes(NestedBox box, IPoint at, double width, int depth,
        IGeometry geometry)
    {
        var notes = NestedSizes.NoteLines(box.Note);
        double below = at.Y + NestedSizes.Header + notes.Count * NestedSizes.NoteLine;
        var own = geometry.Box(at.X, at.Y, width, NestedSizes.Height(box));
        var pill = box.State is { } state ? Pill(state.Says, at.X, below, width, geometry) : null;
        yield return new PlacedBox(box, own, depth, notes, pill);

        if (pill is not null) below += NestedSizes.PillRow;
        double column = NestedSizes.ColumnWidth(box, width);
        foreach (var row in box.Rows())
        {
            below += NestedSizes.Gap;
            for (int slot = 0; slot < row.Count; slot++)
            {
                var corner = geometry.Point(at.X + NestedSizes.Pad + slot * (column + NestedSizes.Gap), below);
                foreach (var placed in Boxes(row[slot], corner, column, depth + 1, geometry)) yield return placed;
            }

            below += row.Max(NestedSizes.Height);
        }
    }

    /// <summary>
    /// The pill of the one thing a box holds: in the middle of the box, just under its name and note.
    /// </summary>
    private static PlacedPill Pill(string says, double x, double below, double width, IGeometry geometry)
    {
        double pillWidth = NestedSizes.PillWidth(says);
        double left = x + (width - pillWidth) / 2;
        return new PlacedPill(says, geometry.Box(left, below + PillDrop, pillWidth, NestedSizes.PillHeight));
    }

    /// <summary>
    /// A box's pins on its top edge, ending a little before its right side: what is handed down first, then what
    /// is said back up.
    /// </summary>
    private static IEnumerable<PlacedPin> Pins(PlacedBox placed, IGeometry geometry)
    {
        int handedDown = placed.Component.In?.Count ?? 0;
        double x = placed.Box.Right - NestedSizes.PinInset - NestedSizes.PinsWidth(placed.Component);
        foreach (var (name, at) in NestedSizes.Pins(placed.Component).Select((name, at) => (name, at)))
        {
            var pin = NestedSizes.Pin(geometry, placed.Component.Id, name, at >= handedDown, x, placed.Box.Top);
            yield return pin;
            x += pin.Box.Width + NestedSizes.PinGap;
        }
    }
}
