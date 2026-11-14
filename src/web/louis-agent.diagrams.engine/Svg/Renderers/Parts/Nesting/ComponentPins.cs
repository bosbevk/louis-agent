using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws a pin of a component diagram (F134.7): on a box's top edge, and as a sample in the picture's key.
/// </summary>
internal static class ComponentPins
{
    /// <summary>
    /// A pin: filled with an arrow down for what is handed down, hollow with an arrow up for what is said back up.
    /// Its class starts with what it is told, so a sample in the key can be told from a real pin.
    /// </summary>
    public static void Draw(SvgWriter writer, PlacedPin pin, string tone, string cls)
    {
        double half = ComponentSizes.PinHeight / 2, arrow = pin.X + 12;
        string look = pin.Up ? $"{cls} pic-edge{tone} up" : $"{cls} pic-edge pic-tint{tone}";
        string tip = pin.Up
            ? $"M{N(arrow - 4)} {N(pin.Y + 2.5)}L{N(arrow)} {N(pin.Y - 3.5)}L{N(arrow + 4)} {N(pin.Y + 2.5)}Z"
            : $"M{N(arrow - 4)} {N(pin.Y - 3)}L{N(arrow)} {N(pin.Y + 3)}L{N(arrow + 4)} {N(pin.Y - 3)}Z";
        writer.Box(pin.X, pin.Y - half, pin.Width, ComponentSizes.PinHeight, half, look)
            .Path(tip, $"pic-fill{tone}", false)
            .Text(arrow + 9, pin.Y + 3.5, pin.Name, $"cmp-pin-text pic-ink{tone}", "start");
    }

    /// <summary>A number as the SVG writes it.</summary>
    private static string N(double value) => SvgWriter.N(value);
}
