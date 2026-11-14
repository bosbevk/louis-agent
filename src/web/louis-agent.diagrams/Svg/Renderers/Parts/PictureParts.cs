using LouisAgent.Choices;
using LouisAgent.Diagrams.Svg.Theme.Looks;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts;

/// <summary>
/// The parts every picture in the new look is drawn from (F134.7, shared since F134.8): the start of a picture,
/// which is the soft light behind its top with its title over it and what type of diagram it is, and a panel.
/// </summary>
/// <remarks>
/// They wear <see cref="PictureLook"/>'s classes, so a kind that draws with them looks like the others without
/// writing a class name of its own. The title and the light are <see cref="MarqueeLook"/>'s, the ones a cards
/// picture, a sequence and a class chart have (F134.12, Louis: "make the theme of the flow and component diagrams
/// resemble the others").
/// </remarks>
internal static class PictureParts
{
    /// <summary>A panel's corner.</summary>
    public const double Radius = 14;

    /// <summary>The room the picture's title takes above the drawing, and where its words stand in it.</summary>
    public const double Head = 44, TitleLine = 14;

    /// <summary>How wide a character of the title is taken to be, generously, since no font is measured.</summary>
    public const double TitleChar = 7;

    /// <summary>
    /// How wide a character of the type's name is taken to be, in its spaced capitals, and the room kept between
    /// the title and it.
    /// </summary>
    private const double TypeChar = 7.6, TypeGap = 28;

    /// <summary>
    /// The light behind the top of the picture: how far under the title's room its middle is, and how far it
    /// reaches down. Across, it reaches the picture's sides, where it has faded to nothing.
    /// </summary>
    private const double LightDrop = 30, LightDown = 190;

    /// <summary>
    /// How wide a picture must be at least for its title and its type to stand side by side on one line.
    /// </summary>
    public static double HeadWidth(string title, string? type) =>
        title.Length * TitleChar + (type is null ? 0 : TypeGap + type.Length * TypeChar);

    /// <summary>
    /// A picture begun: the light first, an oval as wide as the picture, so everything stands on it, and the
    /// title over it as one small line. A picture that names its type has it at the right of that line (F134.13,
    /// Louis: "the type of diagram must always display").
    /// </summary>
    public static SvgWriter Open(string title, double width, string? type = null)
    {
        double reach = width / 2 + SvgWriter.Pad, y = Head + LightDrop;
        string across = N(reach), down = N(LightDown), there = N(2 * reach);
        var writer = new SvgWriter().Paint(MarqueeLook.LightPaint, PaintRun.Out, 0, 0.5, 1)
            .Path($"M{N(-SvgWriter.Pad)} {N(y)}a{across} {down} 0 1 0 {there} 0a{across} {down} 0 1 0 -{there} 0Z",
                "mrq-light", false)
            .Text(0, TitleLine, title, "mrq-title", "start");
        return type is null ? writer : writer.Text(width, TitleLine, type, "pic-group", "end");
    }

    /// <summary>
    /// A panel: the shape a box and a neighbour share. It wears no tone and no depth, since a panel inside a panel
    /// is a step deeper by itself.
    /// </summary>
    public static SvgWriter Panel(SvgWriter writer, double x, double y, double width, double height) =>
        writer.Box(x, y, width, height, Radius, "pic-panel");

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    public static string Tone(DiagramTone tone) => tone == DiagramTone.Plain ? "" : $" tone-{tone.Wire()}";

    /// <summary>A number as the SVG writes it.</summary>
    private static string N(double value) => SvgWriter.N(value);
}
