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
/// wrap at the picture's width, and that is only known once the title and the type are measured.
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
    private const double TypeGap = 28, CaptionRoom = 34, CaptionRise = 8, LightTurns = 0.5;

    /// <summary>The type's name, in its spaced capitals.</summary>
    private static readonly Face TypeFace = new(7.6);

    private readonly string _title;
    private readonly PictureMeasures _measures;
    private readonly List<(string Id, PaintRun Run, double[] Stops)> _paints = [];
    private readonly List<string> _styles = [];
    private string? _heading, _type, _caption;
    private Action<SvgWriter>? _lights, _body;
    private Func<double, (double Height, Action<SvgWriter, double> Draw)>? _key;
    private double _width, _height;

    private PictureBuilder(string title, PictureMeasures measures) => (_title, _measures) = (title, measures);

    /// <summary>
    /// A picture begun, by its title: the name it is known by, and the words in its top left corner.
    /// </summary>
    public static PictureBuilder Titled(string title, PictureMeasures? measures = null) =>
        new(title, measures ?? PictureMeasures.Usual);

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

    /// <summary>One of the kind's own fades, defined after the light's, in the order called.</summary>
    public PictureBuilder Paint(string id, PaintRun run, params double[] stops)
    {
        _paints.Add((id, run, stops));
        return this;
    }

    /// <summary>The kind's own lights, for a kind that has none behind its top.</summary>
    public PictureBuilder Lights(Action<SvgWriter> draw)
    {
        _lights = draw;
        return this;
    }

    /// <summary>The body as one group, under the room the title takes, with how wide and tall it is.</summary>
    public PictureBuilder Body(string cls, SvgWriter drawn, double width, double height) =>
        Draw(writer => writer.Group(cls, 0, _measures.Head, drawn), width, height);

    /// <summary>
    /// A body drawn straight into the picture, by a kind whose layout already stands it under the title, with how
    /// wide it is and how tall under the title's room.
    /// </summary>
    public PictureBuilder Draw(Action<SvgWriter> draw, double width, double height)
    {
        (_body, _width, _height) = (draw, width, height);
        return this;
    }

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
        TitleFace.Width(title) + (type is null ? 0 : TypeGap + TypeFace.Width(type));

    /// <summary>The picture, as one SVG file's text.</summary>
    public string Build()
    {
        string heading = _heading ?? _title;

        // As wide as the body, or as the title's line when that is wider, so a type never stands over a title.
        // The key is laid out for that width.
        double width = Math.Max(_width, HeadWidth(heading, _type));
        var key = _key?.Invoke(width);
        double keyTop = _measures.Head + _height + _measures.KeyGap;
        double under = key is null ? _measures.Head + _height : keyTop + key.Value.Height;
        double height = under + (_caption is null ? 0 : CaptionRoom);

        // The fades first, the light's before a kind's own. Then the parts, in the one order every picture has.
        var writer = new SvgWriter().Paint(MarqueeLook.LightPaint, PaintRun.Out, 0, LightTurns, 1);
        foreach (var (id, run, stops) in _paints) writer.Paint(id, run, stops);
        if (_lights is null) Light(writer, width);
        _lights?.Invoke(writer);
        writer.Text(0, TitleLine, heading, "mrq-title", "start");
        if (_type is not null) writer.Text(width, TitleLine, _type, "pic-group", "end");
        _body?.Invoke(writer);
        key?.Draw(writer, keyTop);
        if (_caption is not null) writer.Text(0, height - CaptionRise, _caption, "caption", "start");

        return writer.Write(SvgTheme.Site, _title, width, height, string.Concat(_styles));
    }

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
