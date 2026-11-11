using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Graphs;

/// <summary>
/// Draws a diagram from its data (F111): places the spec once per render and hands the markup the numbers as text,
/// in the invariant culture, so the SVG reads the same on every machine.
/// </summary>
/// <remarks>
/// The view is a function of the data: a page adds a diagram by declaring a <see cref="GraphSpec"/>, and the layout,
/// the drawing and the motion are written here once for every site on the framework (ADR 0084).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class GraphView
{
    /// <summary>Room around the drawing, so a stroke on the edge isn't clipped.</summary>
    private const int Pad = 8;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The boxes and arrows to draw.</summary>
    [Parameter, EditorRequired]
    public GraphSpec Spec { get; set; } = default!;

    /// <summary>What the diagram shows, in a few words: the caption, and what a screen reader says.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The width the diagram is laid out at; the SVG scales to its box from there.</summary>
    [Parameter]
    public int Width { get; set; } = 880;

    private PlacedGraph Placed => GraphLayout.Place(Spec, Width);

    /// <summary>
    /// This diagram's own arrowhead id (F119): an id is the page's, so two diagrams on one page each need their own, or
    /// the second borrows the first's and the page holds one id twice.
    /// </summary>
    private string ArrowId { get; } = $"arrow-{Guid.NewGuid():N}"[..14] + "-graph";

    /// <summary>The arrowhead an edge ends in.</summary>
    private string ArrowRef => $"url(#{ArrowId})";

    private string ViewBox => $"{-Pad} {-Pad} {Placed.Width + 2 * Pad} {Placed.Height + 2 * Pad}";

    private static string N(double value) => value.ToString("0.#", Invariant);

    private static string Translate(PlacedNode node) => $"translate({N(node.X)} {N(node.Y)})";

    /// <summary>The label sits in the middle of a plain box; in a noted one it stands above the note's lines, which start under it.</summary>
    private static double LabelY(PlacedNode node) => node.Node.Note is null ? node.H / 2 + 5 : 28;

    /// <summary>Where the note's first line sits, each line one step lower.</summary>
    private const double NoteTop = 44, NoteStep = 12;

    /// <summary>The column as a CSS variable, so the styles delay each column's motion a little more.</summary>
    private static string NodeStyle(PlacedNode node) => $"--col: {node.Node.Column}";

    /// <summary>An arrow's length and the column it leaves from, for the stroke that draws it after its box rises.</summary>
    private string EdgeStyle(PlacedEdge edge) =>
        $"--len: {N(edge.Length)}; --col: {Placed.Nodes.First(node => node.Node.Id == edge.Edge.From).Node.Column}";

    /// <summary>The words on a box.</summary>
    private static MarkupString Label(PlacedNode node) => Words("label", node.W / 2, LabelY(node), node.Node.Label);

    /// <summary>The line under a box's words, wrapped as the layout wraps it, one span a line.</summary>
    private static MarkupString Note(PlacedNode node, string note)
    {
        var lines = GraphLayout.NoteLines(note);
        var spans = string.Concat(lines.Select((line, at) =>
            $"<tspan x=\"{N(node.W / 2)}\" y=\"{N(NoteTop + NoteStep * at)}\">{System.Net.WebUtility.HtmlEncode(line)}</tspan>"));
        return new MarkupString($"<text class=\"note\" text-anchor=\"middle\">{spans}</text>");
    }

    /// <summary>The word on an arrow, just above its middle.</summary>
    private MarkupString Word(PlacedEdge edge, string word) =>
        Words("word", edge.WordX, edge.WordY - 7, word, EdgeStyle(edge));

    /// <summary>
    /// An SVG text element, centred on a point. Written as markup here because Razor keeps the <c>text</c> tag for
    /// itself and refuses attributes on it; the words are encoded, so a label can hold any character.
    /// </summary>
    private static MarkupString Words(string cls, double x, double y, string words, string? style = null)
    {
        string styled = style is null ? "" : $" style=\"{style}\"";
        return new MarkupString(
            $"<text class=\"{cls}\" x=\"{N(x)}\" y=\"{N(y)}\" text-anchor=\"middle\"{styled}>{System.Net.WebUtility.HtmlEncode(words)}</text>");
    }
}
