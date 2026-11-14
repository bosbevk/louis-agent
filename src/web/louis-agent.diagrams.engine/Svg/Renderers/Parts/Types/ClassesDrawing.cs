using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes;
using LouisAgent.Diagrams.Engine.Classes.Keys;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Types;
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
    /// <summary>The class chart a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var spec = source.Classes!;
        var placed = GraphLayout.Place(ClassFlow.Of(spec), bend: GraphLayout.SoftBend, rules: FlowRules.Straight,
            geometry: geometry);

        // The types and their links are the body, and the key goes across the picture's width under them. Two
        // of the look's fades are a class chart's to ask for: a contract's top edge with the key's rule, and the
        // glass.
        return PictureBuilder.Titled(source.Title)
            .Type(TypeNames.Of(source))
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Paint(MarqueeLook.GlassPaint, PaintRun.Down, 0, 1)
            .Body("cls-drawing", Types(spec, placed, geometry), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = ClassKeyLayout.Of(spec, width);
                return (height, (writer, top) => writer.Group("cls-legend", 0, top, ClassesKey.Of(items, width)));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, ClassesStyle.Of(SvgTheme.Site))
            .Build();
    }

    /// <summary>
    /// The types and their links, from where the layout starts, back to front: a group's frame, the links with
    /// the loosest under the structure, their words, and the cards last, over the lines' ends.
    /// </summary>
    private static SvgWriter Types(ClassesSpec spec, PlacedGraph placed, IGeometry geometry)
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
            RelationLines.Word(writer, edge, kind, geometry);

        var typeOf = spec.Types.ToDictionary(type => type.Id, StringComparer.Ordinal);
        foreach (var node in placed.Nodes) TypeCards.Draw(writer, node, typeOf[node.Node.Id], geometry);
        return writer;
    }
}
