using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Svg.Ends;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart's link: a line whose colour, dashes and ends say what kind of link it is, the dots that
/// run along the ones that move, and its word in a pill on the line.
/// </summary>
/// <remarks>
/// The ends are a UML chart's: a diamond where a holder holds, a hollow triangle at a base type or an interface,
/// an open arrowhead for a use. The colour and the dashes are the look's, by the kind's name as a class. A holds,
/// an is a and a creates carry dots, and an implements and a uses stand still (Louis's pick): what moves is what
/// is built of what, and what makes what. The key draws its samples with <see cref="Line"/>, so a sample can't
/// look other than the real thing.
/// </remarks>
internal static class RelationLines
{
    /// <summary>The room a diamond takes at a line's start: its length, and a little clear after it.</summary>
    private const double DiamondRoom = 18;

    /// <summary>The tone of the structure's ends, and of a creates' arrowhead.</summary>
    private static readonly string Structure = DiagramTone.Violet.Wire(), Made = DiagramTone.Blue.Wire();

    /// <summary>
    /// How late a kind's lines are drawn: the loosest first, so where two cross, the structure lies on top.
    /// </summary>
    /// <remarks>A switch on the kind, one arm a kind (ADR 0051).</remarks>
    public static int Weight(RelationKind kind) => kind switch
    {
        RelationKind.Uses => 0,
        RelationKind.Creates => 1,
        RelationKind.Implements => 2,
        RelationKind.Inherits => 3,
        _ => 4
    };

    /// <summary>A link where the layout routed it: its line, and its dots when its kind moves.</summary>
    public static void Draw(SvgWriter writer, PlacedEdge edge, RelationKind kind)
    {
        Line(writer, edge.Path, kind);
        if (kind is RelationKind.Holds or RelationKind.Inherits or RelationKind.Creates)
            writer.Path(edge.Path, "pic-march", false);
    }

    /// <summary>A line of a kind along a path, with the ends the kind wears.</summary>
    /// <remarks>A switch on the kind, one arm a kind (ADR 0051).</remarks>
    public static void Line(SvgWriter writer, string path, RelationKind kind)
    {
        (LineEnd start, LineEnd end, string? tone) = kind switch
        {
            RelationKind.Holds => (LineEnd.Diamond, LineEnd.Arrow, Structure),
            RelationKind.Inherits => (LineEnd.None, LineEnd.Triangle, Structure),
            RelationKind.Implements => (LineEnd.None, LineEnd.Triangle, Structure),
            RelationKind.Creates => (LineEnd.None, LineEnd.Arrow, Made),
            _ => (LineEnd.None, LineEnd.Open, null)
        };
        writer.Line(path, $"cls-line {kind.Wire()}", start, end, tone);
    }

    /// <summary>
    /// A link's word, in a pill on its line where the layout kept the room clear: a flow's pill, by a flow's
    /// measures, edged in the line's colour. A use's word is quiet, as its line is.
    /// </summary>
    public static void Word(SvgWriter writer, PlacedEdge edge, RelationKind kind)
    {
        var (x, y, w, h) = FlowText.WordBox(edge);
        // A holds' line starts in a diamond, just where a flow stands a word, so its word moves past the diamond.
        var (dx, dy) = kind == RelationKind.Holds ? PastDiamond(edge.Path, x, y, w, h) : (0, 0);
        writer.Box(x + dx, y + dy, w, h, 9, $"cls-chip {kind.Wire()}");
        string look = kind == RelationKind.Uses ? "cls-word quiet" : "cls-word";
        var lines = FlowText.WordLines(edge.Edge.Word!);
        for (int at = 0; at < lines.Count; at++)
            writer.Text(edge.WordX + dx, edge.WordY + dy - FlowText.WordRise + FlowText.WordStep * at, lines[at],
                look);
    }

    /// <summary>
    /// How far a pill moves along its line to stand clear of the diamond the line starts in. Nothing, when the
    /// pill isn't on the line's first piece, or is clear already.
    /// </summary>
    /// <remarks>
    /// The one place this drawing moves something the layout placed. The layout knows a word's room but not that
    /// a line of this kind starts in a diamond, so the pill is moved the least that clears it, along the line,
    /// into room the line itself runs through.
    /// </remarks>
    private static (double X, double Y) PastDiamond(string path, double x, double y, double w, double h)
    {
        // Where the line starts, and the next point it runs to: level with it, or straight above or under.
        string[] parts = path.Split(' ');
        double startX = Number(parts[1]), startY = Number(parts[2]);
        double nextX = Number(parts[4]), nextY = Number(parts[5]);

        bool level = nextY == startY;
        bool onLine = level ? y <= startY && startY <= y + h : x <= startX && startX <= x + w;
        if (!onLine) return (0, 0);
        return level ? (Along(startX, nextX, x, w), 0) : (0, Along(startY, nextY, y, h));
    }

    /// <summary>
    /// How far along one direction a pill moves to clear the diamond: forward when the line runs forward, back
    /// when it runs back, and not at all when it is clear.
    /// </summary>
    private static double Along(double start, double next, double low, double size)
    {
        bool forward = next > start;
        double clear = forward ? start + DiamondRoom - low : start - DiamondRoom - (low + size);
        return forward ? Math.Max(0, clear) : Math.Min(0, clear);
    }

    /// <summary>A number of a path, as the layout wrote it.</summary>
    private static double Number(string part) => double.Parse(part, CultureInfo.InvariantCulture);
}
