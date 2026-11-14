using LouisAgent.Choices;
using LouisAgent.Diagrams.Svg.Theme.Looks;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts;

/// <summary>
/// The parts every picture in the new look is drawn from (F134.7, shared since F134.8): the ground of dots, the
/// picture's own title with the line under it, and a panel with its marker and the highlight along its top.
/// </summary>
/// <remarks>
/// They wear <see cref="PictureLook"/>'s classes, so a kind that draws with them looks like the others without
/// writing a class name of its own.
/// </remarks>
internal static class PictureParts
{
    /// <summary>A panel's corner, and how far apart the ground's dots are.</summary>
    public const double Radius = 14, DotStep = 22;

    /// <summary>The ground's dots: a row of them every so far, each row one dashed line with round ends.</summary>
    public static SvgWriter Ground(SvgWriter writer, double width, double height) => writer.Path(string.Concat(
        Enumerable.Range(0, (int)(height / DotStep) + 1)
            .Select(row => $"M0 {N(row * DotStep + DotStep / 2)}H{N(width)}")), "pic-dots", false);

    /// <summary>The picture's own title, and under it a line of smaller words when it has one.</summary>
    public static SvgWriter Head(SvgWriter writer, string title, string? under)
    {
        writer.Text(0, 24, title, "pic-title", "start");
        return under is null ? writer : writer.Text(0, 46, under, "pic-group", "start");
    }

    /// <summary>
    /// A panel by its depth, tinted in its tone, with its marker and the highlight along its top: the shape a box
    /// and a neighbour share.
    /// </summary>
    public static SvgWriter Panel(SvgWriter writer, double x, double y, double width, double height, int depth,
        string tone) => writer
        .Box(x, y, width, height, Radius, $"pic-panel depth-{depth} pic-edge{tone}")
        .Path($"M{N(x + Radius)} {N(y + 1.5)}H{N(x + width - Radius)}", "pic-sheen", false)
        .Box(x + 7, y + 14, 4, Math.Min(height - 28, 22), 2, $"pic-fill{tone}");

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    public static string Tone(DiagramTone tone) => tone == DiagramTone.Plain ? "" : $" tone-{tone.Wire()}";

    /// <summary>A number as the SVG writes it.</summary>
    private static string N(double value) => SvgWriter.N(value);
}
