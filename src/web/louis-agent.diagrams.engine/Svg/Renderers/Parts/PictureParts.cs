using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// The small parts every picture in the new look is drawn from (F134.7, shared since F134.8): a panel, and a tone
/// as a shape's class. How a whole picture is put together is <see cref="PictureBuilder"/>'s (CL65.2).
/// </summary>
/// <remarks>
/// They wear <see cref="PictureLook"/>'s classes, so a kind that draws with them looks like the others without
/// writing a class name of its own.
/// </remarks>
internal static class PictureParts
{
    /// <summary>A panel's corner.</summary>
    public const double Radius = 14;

    /// <summary>
    /// A panel: the shape a box and a neighbour share. It wears no tone and no depth, since a panel inside a panel
    /// is a step deeper by itself.
    /// </summary>
    public static SvgWriter Panel(SvgWriter writer, double x, double y, double width, double height) =>
        writer.Box(x, y, width, height, Radius, "pic-panel");

    /// <summary>A panel, where a box of the layout stands.</summary>
    public static SvgWriter Panel(SvgWriter writer, IBox box) => writer.Box(box, Radius, "pic-panel");

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    public static string Tone(DiagramTone tone) => tone == DiagramTone.Plain ? "" : $" tone-{tone.Wire()}";
}
