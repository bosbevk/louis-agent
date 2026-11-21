using LouisAgent.Diagrams.Engine.Nested.Placed;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws a pin of a nested picture (F134.7): on a box's top edge, and as a sample in the picture's key.
/// </summary>
internal static class NestedPins
{
    /// <summary>
    /// How far in from a pin's left the middle of its arrow is, how far the arrow reaches each side of that, how
    /// far after it the pin's name starts, and how far under the pin's middle the name's line is.
    /// </summary>
    private const double ArrowIn = 12, ArrowHalf = 4, NameAfter = 9, NameDrop = 3.5;

    /// <summary>
    /// Where an arrow's foot and its tip stand from the pin's middle: under it and above it for an arrow up, and
    /// the other way round for an arrow down.
    /// </summary>
    private const double UpFoot = 2.5, UpTip = -3.5, DownFoot = -3, DownTip = 3;

    /// <summary>
    /// A pin: filled with an arrow down for what is handed down, hollow with an arrow up for what is said back up.
    /// Its class starts with what it is told, so a sample in the key can be told from a real pin.
    /// </summary>
    public static void Draw(SvgWriter writer, PlacedPin pin, string tone, string cls)
    {
        double arrow = pin.Box.Left + ArrowIn;
        double foot = pin.Edge + (pin.Up ? UpFoot : DownFoot), tip = pin.Edge + (pin.Up ? UpTip : DownTip);
        string look = pin.Up ? $"{cls} pic-edge{tone} up" : $"{cls} pic-edge pic-tint{tone}";
        var head = new PathWriter().From(arrow - ArrowHalf, foot).To(arrow, tip).To(arrow + ArrowHalf, foot).Close();
        writer.Box(pin.Box, pin.Box.HalfHeight, look)
            .Path(head.Text, $"pic-fill{tone}", false)
            .Text(arrow + NameAfter, pin.Edge + NameDrop, pin.Name, $"cmp-pin-text pic-ink{tone}", "start");
    }
}
