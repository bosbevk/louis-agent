using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes;
using LouisAgent.Diagrams.Engine.Classes.Keys;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart in the cards' look (F134.11, the design Louis approved): its own small title on a light,
/// each link a line whose colour and ends say its kind, its word in a pill on the line, each type a glass card
/// with a badge that says what it is, and the key under it all.
/// </summary>
/// <remarks>
/// A class chart still has no layout of its own. <see cref="ClassFlow"/> turns it into a flow, and the flow's
/// layout places it by the rule a flow is drawn by, each arrow at its own place. A flow's arrow doesn't say what
/// kind of link it is, so the drawing reads that from the chart, matching a placed line to its relation by the
/// two types it joins. The look is the shared <see cref="PictureLook"/>, then <see cref="MarqueeLook"/>, then what
/// <see cref="ClassesStyle"/> adds. <see cref="TypeCards"/> draws a type, <see cref="RelationLines"/> a link and
/// <see cref="ClassesKey"/> the key.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class ClassesDrawing
{
    /// <summary>
    /// The room the picture's title takes above the types, where its words stand in it, the room between the
    /// types and the key, and the room a caption takes under the key.
    /// </summary>
    private const double Head = 44, TitleLine = 14, KeyGap = 26, CaptionRoom = 34;

    /// <summary>How wide a character of the title is taken to be, generously, since no font is measured.</summary>
    private const double TitleChar = 7;

    /// <summary>
    /// The light behind the top of the chart: how far under the title's room its middle is, and how far it
    /// reaches down. Across, it reaches the picture's sides, where it has faded to nothing.
    /// </summary>
    private const double LightDrop = 30, LightDown = 190;

    /// <summary>The class chart a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source)
    {
        var spec = source.Classes!;
        var flow = ClassFlow.Of(spec);
        var placed = GraphLayout.Place(flow, bend: GraphLayout.SoftBend, rules: FlowRules.Straight);

        // As wide as the types, or as its own title when that is wider. The key goes across that width.
        double width = Math.Max(placed.Width, source.Title.Length * TitleChar);
        var (items, keyHeight) = ClassKeyLayout.Of(spec, width);
        double keyTop = Head + placed.Height + KeyGap;
        double height = keyTop + keyHeight + (source.Caption is null ? 0 : CaptionRoom);

        // The light first, so everything stands on it. Then the types and their links, and the key under them.
        var writer = Light(Paints(new SvgWriter()), width)
            .Text(0, TitleLine, source.Title, "mrq-title", "start")
            .Group("cls-drawing", 0, Head, Types(spec, placed))
            .Group("cls-legend", 0, keyTop, ClassesKey.Of(items, width));
        if (source.Caption is { } caption) writer.Text(0, height - 8, caption, "caption", "start");

        // What every picture in the new look wears, then what the cards and a sequence wear too, then its own.
        string style = PictureLook.Of(SvgTheme.Site) + MarqueeLook.Rules + ClassesStyle.Of(SvgTheme.Site);
        return writer.Write(SvgTheme.Site, source.Title, width, height, style);
    }

    /// <summary>
    /// The types and their links, from where the layout starts, back to front: a group's frame, the links with
    /// the loosest under the structure, their words, and the cards last, over the lines' ends.
    /// </summary>
    private static SvgWriter Types(ClassesSpec spec, PlacedGraph placed)
    {
        var writer = new SvgWriter();
        foreach (var group in placed.Groups) TypeCards.Frame(writer, group);

        // The kind of each placed line. The layout may hand back a copy of an arrow, so a line is matched to its
        // relation by the two types it joins, and two relations between the same two in the order written.
        var kinds = spec.Relations.GroupBy(relation => (relation.From, relation.To)).ToDictionary(
            pair => pair.Key, pair => new Queue<RelationKind>(pair.Select(relation => relation.Kind)));
        var links = placed.Edges
            .Select(edge => (Edge: edge, Kind: kinds[(edge.Edge.From, edge.Edge.To)].Dequeue())).ToList();

        foreach (var (edge, kind) in links.OrderBy(link => RelationLines.Weight(link.Kind)))
            RelationLines.Draw(writer, edge, kind);
        foreach (var (edge, kind) in links.Where(link => link.Edge.Edge.Word is not null))
            RelationLines.Word(writer, edge, kind);

        var typeOf = spec.Types.ToDictionary(type => type.Id, StringComparer.Ordinal);
        foreach (var node in placed.Nodes) TypeCards.Draw(writer, node, typeOf[node.Node.Id]);
        return writer;
    }

    /// <summary>
    /// The look's three fades, each under the id the style fills with: the light from its middle out, a contract's
    /// top edge and the key's rule across, and the glass down.
    /// </summary>
    private static SvgWriter Paints(SvgWriter writer) => writer
        .Paint(MarqueeLook.LightPaint, PaintRun.Out, 0, 0.5, 1)
        .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
        .Paint(MarqueeLook.GlassPaint, PaintRun.Down, 0, 1);

    /// <summary>The light behind the top of the chart: an oval as wide as the picture.</summary>
    private static SvgWriter Light(SvgWriter writer, double width)
    {
        double reach = width / 2 + SvgWriter.Pad, y = Head + LightDrop;
        string across = SvgWriter.N(reach), down = SvgWriter.N(LightDown), there = SvgWriter.N(2 * reach);
        return writer.Path(
            $"M{SvgWriter.N(-SvgWriter.Pad)} {SvgWriter.N(y)}a{across} {down} 0 1 0 {there} 0" +
            $"a{across} {down} 0 1 0 -{there} 0Z", "mrq-light", false);
    }
}
