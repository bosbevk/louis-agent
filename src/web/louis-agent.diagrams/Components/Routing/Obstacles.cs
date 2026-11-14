using LouisAgent.Diagrams.Components.Placed;
using LouisAgent.Diagrams.Components.Placed.Lines;

namespace LouisAgent.Diagrams.Components.Routing;

/// <summary>
/// What stands in the way of a state's line (F134.7), and whether a way is clear of it.
/// </summary>
internal static class Obstacles
{
    /// <summary>How near a box, and a pin, a line may pass.</summary>
    private const double BoxRoom = 4, PinRoom = 3;

    /// <summary>Half a pin's height, and where a box's words stand: from its left edge, and under its top.</summary>
    private const double PinHalf = 9, WordsLeft = 22, WordsTop = 12, WordsHeight = 26, NoteTop = 38;

    /// <summary>
    /// Everything a line to this pin may not touch: every box that doesn't hold the pin's box, every other pin, and
    /// the words of the boxes it passes through.
    /// </summary>
    public static IReadOnlyList<Block> For(IReadOnlyList<PlacedBox> between, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins, PlacedPin own) =>
    [
        .. boxes.Except(between).Select(box => new Block(box.X - BoxRoom, box.Y - BoxRoom,
            box.Width + 2 * BoxRoom, box.Height + 2 * BoxRoom)),
        .. pins.Where(pin => pin != own).Select(pin => new Block(pin.X - PinRoom, pin.Y - PinHalf - PinRoom,
            pin.Width + 2 * PinRoom, 2 * (PinHalf + PinRoom))),
        .. between.SelectMany(Words)
    ];

    /// <summary>Whether a way of level and upright stretches touches none of the blocks.</summary>
    public static bool Clear(IReadOnlyList<LinePoint> points, IReadOnlyList<Block> blocks) =>
        !points.Zip(points.Skip(1)).Any(stretch => blocks.Any(block => Touches(stretch.First, stretch.Second, block)));

    /// <summary>Where a box's own words stand: its name and chip, the words at its right, and its note.</summary>
    private static IEnumerable<Block> Words(PlacedBox placed)
    {
        var box = placed.Box;
        double chip = box.Kind is null ? 0 : 8 + ComponentSizes.ChipWidth(box.Kind);
        yield return new Block(placed.X + WordsLeft, placed.Y + WordsTop,
            ComponentSizes.NameWidth(box.Name) + chip, WordsHeight);

        if (box.Aside is { } aside)
        {
            double width = ComponentSizes.WordsWidth(aside);
            yield return new Block(placed.Right - 16 - width, placed.Y + WordsTop, width, WordsHeight);
        }

        if (placed.NoteLines.Count == 0) yield break;
        double longest = placed.NoteLines.Max(ComponentSizes.WordsWidth);
        yield return new Block(placed.X + WordsLeft, placed.Y + NoteTop, longest,
            placed.NoteLines.Count * ComponentSizes.NoteLine);
    }

    /// <summary>Whether a level or upright stretch touches a block.</summary>
    private static bool Touches(LinePoint from, LinePoint to, Block block)
    {
        double left = Math.Min(from.X, to.X), right = Math.Max(from.X, to.X);
        double top = Math.Min(from.Y, to.Y), bottom = Math.Max(from.Y, to.Y);
        return left < block.X + block.Width && right > block.X && top < block.Y + block.Height && bottom > block.Y;
    }
}
