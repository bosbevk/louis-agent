using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Engine.Svg.Paints;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// Puts a picture together (CL65.2): its title and its type over a soft light, its body, its key, its caption and
/// its styles. A kind tells it each part, in any order, and <see cref="Build"/> writes them in the one order
/// every picture has, in a picture as wide and as tall as they need.
/// </summary>
/// <remarks>
/// Builder (Gang of Four, Refactoring.Guru), Louis's pick of three shapes: "then use builder pattern for
/// implementation". Six kinds each had a method that did these steps, and four of the steps were the same in
/// all six. Here they are written once, and a kind's drawing is the director: it places with its own layout and
/// hands over its body and its key. The numbers a kind differs in are <see cref="PictureMeasures"/>, so nothing
/// here asks which kind it is building. Nothing is written before <see cref="Build"/>, because a key's rows
/// wrap at the picture's width, and that is only known once the title and the type are measured. Drawn for a phone
/// (F169.5), the type stands on its own line under the title: Louis's pick "C: the type under the title", over
/// leaving the head as it is, which shrank two pictures' words under 10 pixels, and over dropping the type, which
/// his rule that the type always shows forbids.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/builder">Builder (Refactoring.Guru)</seealso>
internal sealed class PictureBuilder
{
    /// <summary>Where the title's words stand.</summary>
    public const double TitleLine = 14;

    /// <summary>The title's words, which say how wide they run, generously.</summary>
    public static readonly Face TitleFace = new(7);

    /// <summary>
    /// The room kept between the title and the type's name, the room a caption takes, how far above the bottom
    /// its words stand, and where along its run the light behind the top changes from its first colour to its
    /// second.
    /// </summary>
    private const double TypeGap = 28, CaptionRoom = 34, CaptionRise = 8;

    /// <summary>How far under the title's words the header's hairline runs.</summary>
    private const double HeaderRule = 12;

    /// <summary>
    /// How far in from the picture's sides the header's words and the brand stand, so they don't sit on its edge
    /// (Louis: "must also move in a bit"). The hairlines still run the whole width.
    /// </summary>
    public const double Inset = 16;

    /// <summary>The type's name, in its spaced capitals.</summary>
    private static readonly Face TypeFace = new(7.6);

    /// <summary>
    /// How far under the title's words the type's own line stands on a phone (F169.5), and so how much taller the
    /// head is there.
    /// </summary>
    private const double TypeLine = 14;

    private readonly string _title;
    private readonly IGeometry _geometry;
    private readonly PictureMeasures _measures;
    private readonly List<Fade> _paints = [];
    private readonly List<string> _styles = [];
    private string? _heading, _type, _caption;
    private Action<SvgWriter>? _lights;
    private Action<SvgWriter, double>? _body;
    private PictureFit _fit = PictureFit.Wide;
    private Func<double, (double Height, Action<SvgWriter, double> Draw)>? _key;
    private double _width, _height;

    private PictureBuilder(string title, IGeometry geometry, PictureMeasures measures) =>
        (_title, _geometry, _measures) = (title, geometry, measures);

    /// <summary>
    /// A picture begun, by its title: the name it is known by, and the words in its top left corner, drawn with the
    /// geometry the kind draws its own parts with.
    /// </summary>
    public static PictureBuilder Titled(string title, IGeometry geometry, PictureMeasures? measures = null) =>
        new(title, geometry, measures ?? PictureMeasures.Usual);

    /// <summary>The words in the title's line, when they say more than the picture's name does.</summary>
    public PictureBuilder Heading(string words)
    {
        _heading = words;
        return this;
    }

    /// <summary>
    /// What type of diagram it is, written at the right of the title's line (Louis: "the type of diagram must
    /// always display"). Nothing for a picture that has none.
    /// </summary>
    public PictureBuilder Type(string? type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Which screen the picture is drawn for (F169.5). Drawn for a phone, its type stands on a line of its own under
    /// the title, so the head is never wider than the body; wide, it stands beside the title, as always.
    /// </summary>
    public PictureBuilder Fit(PictureFit fit)
    {
        _fit = fit;
        return this;
    }

    /// <summary>One of the kind's own fades, defined after the light's, in the order called.</summary>
    public PictureBuilder Paint(Fade fade)
    {
        _paints.Add(fade);
        return this;
    }

    /// <summary>The kind's own lights, for a kind that has none behind its top.</summary>
    public PictureBuilder Lights(Action<SvgWriter> draw)
    {
        _lights = draw;
        return this;
    }

    /// <summary>The body as one group, under the room the title takes, with how wide and tall it is.</summary>
    public PictureBuilder Body(string cls, SvgWriter drawn, double width, double height)
    {
        (_body, _width, _height) = ((writer, drop) => writer.Group(cls, 0, _measures.Head + drop, drawn), width,
            height);
        return this;
    }

    /// <summary>
    /// A body drawn straight into the picture, by a kind whose layout already stands it under the title, with how
    /// wide it is and how tall under the title's room.
    /// </summary>
    public PictureBuilder Draw(Action<SvgWriter> draw, double width, double height)
    {
        (_body, _width, _height) = ((writer, _) => draw(writer), width, height);
        return this;
    }

    /// <summary>Whether the type stands on its own line under the title: on a phone, when there is a type.</summary>
    private bool TypeUnder => _fit == PictureFit.Narrow && _type is not null;

    /// <summary>
    /// The key, as what lays it out for the picture's width: how tall it is, and how to draw it with its top at a
    /// height.
    /// </summary>
    public PictureBuilder Key(Func<double, (double Height, Action<SvgWriter, double> Draw)> layout)
    {
        _key = layout;
        return this;
    }

    /// <summary>The caption under everything. Nothing when the picture has none.</summary>
    public PictureBuilder Caption(string? words)
    {
        _caption = words;
        return this;
    }

    /// <summary>
    /// The picture's styles, joined in the order given: what every picture wears first, a kind's own last.
    /// </summary>
    public PictureBuilder Style(params string[] rules)
    {
        _styles.AddRange(rules);
        return this;
    }

    /// <summary>
    /// How wide a picture must be at least for its title and its type to stand side by side on one line.
    /// </summary>
    public static double HeadWidth(string title, string? type) =>
        2 * Inset + TitleFace.Width(title) + (type is null ? 0 : TypeGap + TypeFace.Width(type));

    /// <summary>The picture, as one SVG file's text.</summary>
    public string Build()
    {
        string heading = _heading ?? _title;

        // As wide as the body, or as the title's line when that is wider, so a type never stands over a title. On a
        // phone the type has a line of its own, which moves the hairline and everything under it down by that line.
        // The key is laid out for that width.
        double drop = TypeUnder ? TypeLine : 0;
        double head = TypeUnder
            ? Math.Max(HeadWidth(heading, null), 2 * Inset + TypeFace.Width(_type!))
            : HeadWidth(heading, _type);
        double width = Math.Max(_width, head);
        var key = _key?.Invoke(width);

        // The footer always follows the body: the key, its own rule its hairline, or the brand's row alone.
        double footerTop = _measures.Head + drop + _height + _measures.KeyGap;
        double under = footerTop + (key?.Height ?? PictureBrand.Room);
        double height = under + (_caption is null ? 0 : CaptionRoom);

        // The fades first, the light's before a kind's own. Then the parts, in the one order every picture has:
        // the header (its title, its type and the hairline under them), the body, the footer and the caption.
        var writer = new SvgWriter().Paint(MarqueeLook.LightFade);
        foreach (var fade in _paints) writer.Paint(fade);
        if (_lights is null) Light(writer, width);
        _lights?.Invoke(writer);
        writer.Text(Inset, TitleLine, heading, "mrq-title", "start");
        if (TypeUnder) writer.Text(Inset, TitleLine + TypeLine, _type!, "pic-group", "start");
        else if (_type is not null) writer.Text(width - Inset, TitleLine, _type, "pic-group", "end");
        Hairline(writer, _geometry, TitleLine + HeaderRule + drop, width);
        _body?.Invoke(writer, drop);
        if (key is null) Hairline(writer, _geometry, footerTop, width);
        key?.Draw(writer, footerTop);
        PictureBrand.Draw(writer, width - Inset, footerTop);
        if (_caption is not null) writer.Text(0, height - CaptionRise, _caption, "caption", "start");

        // The kind's own styles in the order given, and the brand's after them.
        return writer.Write(SvgTheme.Site, _title, width, height, string.Concat(_styles) + PictureBrand.Style);
    }

    /// <summary>A hairline across the picture, as the key's own rule is drawn, so header and footer match.</summary>
    private static void Hairline(SvgWriter writer, IGeometry geometry, double y, double width) =>
        writer.Box(geometry.Box(0, y, width, 1), 0, "mrq-rule");

    /// <summary>
    /// The light behind the top of the picture: an oval as wide as the picture, where it has faded to nothing at
    /// the sides, so everything stands on it.
    /// </summary>
    private void Light(SvgWriter writer, double width)
    {
        double reach = width / 2 + SvgWriter.Pad, y = _measures.Head + _measures.LightDrop;
        var oval = new PathWriter().Oval(width / 2, y, reach, _measures.LightDown);
        writer.Path(oval.Text, "mrq-light", false);
    }
}
