using LouisAgent.Diagrams.Components.Placed;
using LouisAgent.Diagrams.Components.Spec;

namespace LouisAgent.Diagrams.Components;

/// <summary>
/// Places a component diagram (F134.7): every box inside the one that holds it, every pin on its box's top edge,
/// and every neighbour beside the root. No place is written in a picture's JSON: it all comes from here.
/// </summary>
/// <remarks>
/// A layout as a pure function of data, over the Composite pattern's tree, walked once: a box is as wide as its own
/// words and the boxes it holds need, and the boxes it holds stretch to its inner width, stacked in the JSON's
/// order. So the same JSON always draws the same picture, and the layout is tested with no browser.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/composite">Composite (Refactoring.Guru)</seealso>
public static class ComponentLayout
{
    /// <summary>The room above the root, and between the root and a column of neighbours for their lines.</summary>
    private const double Top = 12, ColumnGap = 60;

    /// <summary>The diagram, placed.</summary>
    public static PlacedComponents Of(ComponentsSpec components)
    {
        var around = components.Around ?? [];

        // The root stands after the column of what comes before it, when there is one.
        double before = NeighbourLayout.ColumnWidth(around, NeighbourSide.Before);
        double left = before == 0 ? 0 : before + ColumnGap;
        var boxes = Boxes(components.Root, left, Top, ComponentSizes.Width(components.Root), 0).ToList();
        var root = boxes[0];

        var neighbours = NeighbourLayout.Place(around, boxes, root.Right + ColumnGap);
        var lines = neighbours.SelectMany(neighbour => NeighbourLayout.Lines(neighbour, boxes)).ToList();
        var pins = boxes.SelectMany(Pins).ToList();

        double width = neighbours.Select(neighbour => neighbour.Right).Append(root.Right).Max();
        double height = neighbours.Select(neighbour => neighbour.Bottom).Append(root.Bottom).Max();
        return new PlacedComponents(width, height, boxes, pins, neighbours, lines);
    }

    /// <summary>
    /// A box at its place, then every box it holds: each as wide as the room inside, under the one before it.
    /// </summary>
    private static IEnumerable<PlacedBox> Boxes(ComponentBox box, double x, double y, double width, int depth)
    {
        var notes = ComponentSizes.NoteLines(box.Note);
        double below = y + ComponentSizes.Header + notes.Count * ComponentSizes.NoteLine;
        var pill = box.State is { } state ? Pill(state.Says, x, below, width) : null;
        yield return new PlacedBox(box, x, y, width, ComponentSizes.Height(box), depth, notes, pill);

        if (pill is not null) below += ComponentSizes.PillRow;
        double inside = width - 2 * ComponentSizes.Pad;
        foreach (var held in box.Holds ?? [])
        {
            below += ComponentSizes.Gap;
            foreach (var placed in Boxes(held, x + ComponentSizes.Pad, below, inside, depth + 1)) yield return placed;
            below += ComponentSizes.Height(held);
        }
    }

    /// <summary>
    /// The pill of the one thing a box holds: in the middle of the box, just under its name and note.
    /// </summary>
    private static PlacedPill Pill(string says, double x, double below, double width)
    {
        double pillWidth = ComponentSizes.PillWidth(says);
        return new PlacedPill(says, x + (width - pillWidth) / 2, below + 4, pillWidth, ComponentSizes.PillHeight);
    }

    /// <summary>
    /// A box's pins on its top edge, ending a little before its right side: what is handed down first, then what
    /// is said back up.
    /// </summary>
    private static IEnumerable<PlacedPin> Pins(PlacedBox placed)
    {
        int handedDown = placed.Box.In?.Count ?? 0;
        double x = placed.Right - ComponentSizes.PinInset - ComponentSizes.PinsWidth(placed.Box);
        foreach (var (name, at) in ComponentSizes.Pins(placed.Box).Select((name, at) => (name, at)))
        {
            double width = ComponentSizes.PinWidth(name);
            yield return new PlacedPin(placed.Box.Id, name, at >= handedDown, x, placed.Y, width);
            x += width + ComponentSizes.PinGap;
        }
    }
}
