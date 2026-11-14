using System.Globalization;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Graphs;

/// <summary>
/// Draws a diagram from its data (F111): places the spec once per render and hands the markup the numbers as text,
/// in the invariant culture, so the SVG reads the same on every machine.
/// </summary>
/// <remarks>
/// The view is a function of the data: a page adds a diagram by declaring a <see cref="GraphSpec"/>, and the layout,
/// the drawing and the motion are written here once for every site on the framework (ADR 0084). It asks each
/// placed box for its edges and its middle, and works none of them out itself (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public partial class GraphView
{
    /// <summary>Room around the drawing, so a stroke on the edge isn't clipped.</summary>
    private const int Pad = 8;

    /// <summary>A plain box's corner, the site's listing's, and the corner of the ground behind a word.</summary>
    private const string PlainCorner = "10", GroundCorner = "4";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The geometry the diagram is laid out and drawn with: the one in use.</summary>
    private static readonly IGeometry Geometry = Geometries.InUse;

    /// <summary>The boxes and arrows to draw.</summary>
    [Parameter, EditorRequired]
    public GraphSpec Spec { get; set; } = default!;

    /// <summary>What the diagram shows, in a few words: the caption, and what a screen reader says.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The width the diagram is laid out at; the SVG scales to its box from there.</summary>
    [Parameter]
    public int Width { get; set; } = 880;

    /// <summary>
    /// How far under the middle of its line a word's letters stand, so the middle of a 10 px word is on the line.
    /// </summary>
    private const double WordDrop = 3.5;

    /// <summary>
    /// The diagram, placed by the rules a flow in the docs is (F134.8): a box level with what feeds it, each arrow
    /// into a box at a point of its own.
    /// </summary>
    private PlacedGraph Placed => GraphLayout.Place(Spec, Width, rules: FlowRules.Straight, geometry: Geometry);

    /// <summary>
    /// This diagram's own arrowhead id (F119): an id is the page's, so two diagrams on one page each need their own, or
    /// the second borrows the first's and the page holds one id twice.
    /// </summary>
    private string ArrowId { get; } = $"arrow-{Guid.NewGuid():N}"[..14] + "-graph";

    /// <summary>The arrowhead an edge ends in.</summary>
    private string ArrowRef => $"url(#{ArrowId})";

    private string ViewBox => $"{-Pad} {-Pad} {Placed.Width + 2 * Pad} {Placed.Height + 2 * Pad}";

    private static string N(double value) => value.ToString("0.#", Invariant);

    private static string Translate(PlacedNode node) => $"translate({N(node.Box.Left)} {N(node.Box.Top)})";

    /// <summary>
    /// A box as it is drawn inside its own group, which stands at the box's corner: as big as it is, at nothing
    /// across and nothing down.
    /// </summary>
    private static IBox AtItsCorner(PlacedNode node) => Geometry.Box(0, 0, node.Box.Width, node.Box.Height);

    /// <summary>
    /// A diamond's or a slanted box's outline, drawn from the box's own corner. Nothing for a plain box or a pill.
    /// </summary>
    private static string? Outline(PlacedNode node) => NodeShapes.PathOf(node.Node.Shape, AtItsCorner(node));

    /// <summary>
    /// A box's corner: the site's listing's, and half its height for a start or an end, which is a pill.
    /// </summary>
    private static string Corner(PlacedNode node) =>
        node.Node.Shape == NodeShape.Terminal ? N(node.Box.HalfHeight) : PlainCorner;

    /// <summary>The column as a CSS variable, so the styles delay each column's motion a little more.</summary>
    private static string NodeStyle(PlacedNode node) => $"--col: {node.Node.Column}";

    /// <summary>
    /// An arrow's length and the column it leaves from, for the stroke that draws it after its box rises.
    /// </summary>
    private string EdgeStyle(PlacedEdge edge) =>
        $"--len: {N(edge.Length)}; --col: {Placed.Nodes.First(node => node.Node.Id == edge.Edge.From).Node.Column}";

    /// <summary>The words on a box, in its middle across.</summary>
    private static MarkupString Label(PlacedNode node) =>
        Words("label", AtItsCorner(node).Middle.X, FlowText.LabelY(node), node.Node.Label);

    /// <summary>The line under a box's words, wrapped as the layout wraps it, one span a line.</summary>
    private static MarkupString Note(PlacedNode node, string note)
    {
        var lines = GraphLayout.NoteLines(note);
        string middle = N(AtItsCorner(node).Middle.X);
        var spans = string.Concat(lines.Select((line, at) =>
            $"<tspan x=\"{middle}\" y=\"{N(FlowText.NoteTop + FlowText.NoteStep * at)}\">" +
            $"{System.Net.WebUtility.HtmlEncode(line)}</tspan>"));
        return new MarkupString($"<text class=\"note\" text-anchor=\"middle\">{spans}</text>");
    }

    /// <summary>
    /// The ground behind an arrow's word, of the page's own colour: the room the layout keeps for the word, so the
    /// line the word stands on doesn't show through the space between two words. It carries its arrow's column,
    /// and so appears when its word does.
    /// </summary>
    private MarkupString WordGround(PlacedEdge edge)
    {
        var ground = FlowText.WordBox(edge, Geometry);
        return new MarkupString(
            $"<rect class=\"word-ground\" x=\"{N(ground.Left)}\" y=\"{N(ground.Top)}\" width=\"{N(ground.Width)}\"" +
            $" height=\"{N(ground.Height)}\" rx=\"{GroundCorner}\" style=\"{EdgeStyle(edge)}\"></rect>");
    }

    /// <summary>
    /// The word on an arrow, in the middle of the ground the layout keeps for it. That is on its own line, which
    /// its ground hides behind it.
    /// </summary>
    private MarkupString Word(PlacedEdge edge, string word) => Words("word", edge.Word.X,
        FlowText.WordBox(edge, Geometry).Middle.Y + WordDrop, word, EdgeStyle(edge));

    /// <summary>
    /// An SVG text element, centred on a point. Written as markup here because Razor keeps the <c>text</c> tag for
    /// itself and refuses attributes on it; the words are encoded, so a label can hold any character.
    /// </summary>
    private static MarkupString Words(string cls, double x, double y, string words, string? style = null)
    {
        string styled = style is null ? "" : $" style=\"{style}\"";
        return new MarkupString(
            $"<text class=\"{cls}\" x=\"{N(x)}\" y=\"{N(y)}\" text-anchor=\"middle\"{styled}>" +
            $"{System.Net.WebUtility.HtmlEncode(words)}</text>");
    }
}
