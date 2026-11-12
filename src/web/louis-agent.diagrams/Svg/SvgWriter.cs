using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Svg;

/// <summary>
/// Collects a drawing's shapes and writes them as one SVG file: a rounded ground, the theme's style, an arrowhead, and
/// the shapes in the order they were added, every number in the invariant culture and every line ending a plain
/// <c>\n</c>, so the same drawing is the same bytes on every machine.
/// </summary>
/// <remarks>
/// Builder (Refactoring.Guru): a renderer adds boxes, paths and words one by one and asks for the file at the end,
/// so no renderer writes markup or escapes text by hand, and the frame, the style and the arrowhead every picture
/// needs are written in one place.
/// </remarks>
internal sealed class SvgWriter
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>Room around the drawing, inside the ground, so nothing touches its edge.</summary>
    internal const double Pad = 24;

    /// <summary>The arrowhead's id, one per file.</summary>
    private const string ArrowId = "arrow";

    private readonly List<XElement> _shapes = [];

    /// <summary>The tones whose arrowheads the drawing uses, each written once, in the order first used.</summary>
    private readonly List<string> _tones = [];

    /// <summary>A rounded box.</summary>
    public SvgWriter Box(double x, double y, double width, double height, double radius, string cls)
    {
        _shapes.Add(new XElement(Svg + "rect", new XAttribute("class", cls), new XAttribute("x", N(x)),
            new XAttribute("y", N(y)), new XAttribute("width", N(width)), new XAttribute("height", N(height)),
            new XAttribute("rx", N(radius))));
        return this;
    }

    /// <summary>A circle round a point.</summary>
    public SvgWriter Circle(double x, double y, double radius, string cls)
    {
        _shapes.Add(new XElement(Svg + "circle", new XAttribute("class", cls), new XAttribute("cx", N(x)),
            new XAttribute("cy", N(y)), new XAttribute("r", N(radius))));
        return this;
    }

    /// <summary>
    /// A path, ending in the arrowhead when it is an arrow, and starting in one too when it points back as well; a
    /// toned arrow's head wears its tone (CL60.2).
    /// </summary>
    public SvgWriter Path(string data, string cls, bool arrow, bool arrowAtStart = false, string? tone = null)
    {
        string head = tone is null ? ArrowId : $"{ArrowId}-{tone}";
        if (tone is not null && (arrow || arrowAtStart)) _tones.Add(tone);
        var path = new XElement(Svg + "path", new XAttribute("class", cls), new XAttribute("d", data));
        if (arrowAtStart) path.Add(new XAttribute("marker-start", $"url(#{head})"));
        if (arrow) path.Add(new XAttribute("marker-end", $"url(#{head})"));
        _shapes.Add(path);
        return this;
    }

    /// <summary>Words at a point, centred on it unless told to start there.</summary>
    public SvgWriter Text(double x, double y, string words, string cls, string anchor = "middle")
    {
        _shapes.Add(new XElement(Svg + "text", new XAttribute("class", cls), new XAttribute("x", N(x)),
            new XAttribute("y", N(y)), new XAttribute("text-anchor", anchor), words));
        return this;
    }

    /// <summary>
    /// An icon, its top-left corner at a point, drawn at a size: its paths in Tabler's 24-unit box, moved and scaled
    /// as one group, which the style strokes.
    /// </summary>
    public SvgWriter Icon(double x, double y, double size, DiagramIcon icon, string cls)
    {
        string scale = (size / 24).ToString("0.####", CultureInfo.InvariantCulture);
        _shapes.Add(new XElement(Svg + "g", new XAttribute("class", cls),
            new XAttribute("transform", $"translate({N(x)} {N(y)}) scale({scale})"),
            IconPaths.Of(icon).Select(data => new XElement(Svg + "path", new XAttribute("d", data)))));
        return this;
    }

    /// <summary>
    /// The file: the drawing's box plus its padding, titled for a screen reader, the shapes on the ground; a kind with
    /// a look of its own (a ticket) adds its rules after the theme's.
    /// </summary>
    public string Write(SvgTheme theme, string title, double width, double height, string? style = null)
    {
        double outerWidth = width + 2 * Pad, outerHeight = height + 2 * Pad;
        var root = new XElement(Svg + "svg",
            new XAttribute("viewBox", $"{N(-Pad)} {N(-Pad)} {N(outerWidth)} {N(outerHeight)}"),
            new XAttribute("width", N(outerWidth)), new XAttribute("height", N(outerHeight)),
            new XAttribute("role", "img"), new XAttribute("aria-label", title),
            new XElement(Svg + "title", title),
            new XElement(Svg + "style", SvgStyle.Of(theme)),
            style is null ? null : new XElement(Svg + "style", style),
            new XElement(Svg + "defs", Arrowhead(ArrowId, "arrow"),
                _tones.Distinct().Select(tone => Arrowhead($"{ArrowId}-{tone}", $"arrow tone-{tone}"))),
            new XElement(Svg + "rect", new XAttribute("class", "ground"), new XAttribute("x", N(-Pad)),
                new XAttribute("y", N(-Pad)), new XAttribute("width", N(outerWidth)),
                new XAttribute("height", N(outerHeight)), new XAttribute("rx", "14")),
            _shapes);

        return Serialize(root);
    }

    /// <summary>
    /// Words at a point in spans of their own classes, as a title's bold head and its plain rest; the space between
    /// two spans is the line break the writer puts between them, which a reader's screen shows as one space.
    /// </summary>
    public SvgWriter Spans(double x, double y, string cls, IEnumerable<(string Words, string Cls)> spans)
    {
        _shapes.Add(new XElement(Svg + "text", new XAttribute("class", cls), new XAttribute("x", N(x)),
            new XAttribute("y", N(y)),
            spans.Select(span => new XElement(Svg + "tspan", new XAttribute("class", span.Cls), span.Words))));
        return this;
    }

    /// <summary>Shapes drawn elsewhere, moved together by a distance.</summary>
    public SvgWriter Group(string cls, double dx, double dy, IEnumerable<XElement> shapes)
    {
        _shapes.Add(new XElement(Svg + "g", new XAttribute("class", cls),
            new XAttribute("transform", $"translate({N(dx)} {N(dy)})"), shapes));
        return this;
    }

    /// <summary>The shapes added so far, for a frame to put round another picture's.</summary>
    public IReadOnlyList<XElement> Shapes => _shapes;

    /// <summary>A picture's root as the file's text, the same bytes on every machine.</summary>
    internal static string Serialize(XElement root)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };
        var text = new StringBuilder();
        using (var writer = XmlWriter.Create(text, settings)) root.Save(writer);
        return text.Append('\n').ToString();
    }

    /// <summary>An arrowhead an arrow ends in, plain or in a tone.</summary>
    private static XElement Arrowhead(string id, string cls) =>
        new(Svg + "marker", new XAttribute("id", id), new XAttribute("viewBox", "0 0 10 10"),
            new XAttribute("refX", "10"), new XAttribute("refY", "5"), new XAttribute("markerWidth", "8"),
            new XAttribute("markerHeight", "8"), new XAttribute("markerUnits", "userSpaceOnUse"),
            new XAttribute("orient", "auto-start-reverse"),
            new XElement(Svg + "path", new XAttribute("class", cls), new XAttribute("d", "M 0 0 L 10 5 L 0 10 z")));

    /// <summary>A number as the file writes it: invariant, at most one decimal.</summary>
    internal static string N(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
