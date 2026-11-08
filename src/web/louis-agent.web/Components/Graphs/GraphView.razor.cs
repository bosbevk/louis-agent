using System.Globalization;
using LouisAgent.Web.Graphs;
using LouisAgent.Web.Graphs.Placed;
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
    [Parameter, EditorRequired] public GraphSpec Spec { get; set; } = default!;

    /// <summary>What the diagram shows, in a few words: the caption, and what a screen reader says.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The width the diagram is laid out at; the SVG scales to its box from there.</summary>
    [Parameter] public int Width { get; set; } = 880;

    private PlacedGraph Placed => GraphLayout.Place(Spec, Width);

    private string ViewBox => $"{-Pad} {-Pad} {Placed.Width + 2 * Pad} {Placed.Height + 2 * Pad}";

    private static string N(double value) => value.ToString("0.#", Invariant);

    private static string Translate(PlacedNode node) => $"translate({N(node.X)} {N(node.Y)})";

    /// <summary>The label sits in the middle of a plain box, and above the note in a noted one.</summary>
    private static double LabelY(PlacedNode node) => node.Node.Note is null ? node.H / 2 + 5 : node.H / 2 - 4;

    /// <summary>The column as a CSS variable, so the styles delay each column's motion a little more.</summary>
    private static string NodeStyle(PlacedNode node) => $"--col: {node.Node.Column}";

    /// <summary>An arrow's length and the column it leaves from, for the stroke that draws it after its box rises.</summary>
    private string EdgeStyle(PlacedEdge edge) =>
        $"--len: {N(edge.Length)}; --col: {Placed.Nodes.First(node => node.Node.Id == edge.Edge.From).Node.Column}";

    /// <summary>The words on a box.</summary>
    private static MarkupString Label(PlacedNode node) => Words("label", node.W / 2, LabelY(node), node.Node.Label);

    /// <summary>The line under a box's words.</summary>
    private static MarkupString Note(PlacedNode node, string note) => Words("note", node.W / 2, node.H - 16, note);

    /// <summary>The word on an arrow, just above its middle.</summary>
    private MarkupString Word(PlacedEdge edge, string word) => Words("word", edge.MidX, edge.MidY - 6, word, EdgeStyle(edge));

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
