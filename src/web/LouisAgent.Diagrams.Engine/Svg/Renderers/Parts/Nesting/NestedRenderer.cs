using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Nested;
using LouisAgent.Diagrams.Engine.Nested.Placed;
using LouisAgent.Diagrams.Engine.Nested.Placed.Lines;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Nested.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws a nested picture (F134.7): its title as one small line over a soft light, a box inside a box for what
/// holds what, pins on a box's top edge for what is handed down and said back up, a pill for the one thing a box
/// holds, the neighbours beside the root, and the key that says how to read it.
/// </summary>
/// <remarks>
/// Strategy, as every kind's renderer is: one <see cref="IDiagramRenderer"/> a kind, picked by
/// <see cref="DiagramRenderers"/>. It draws what <see cref="NestedLayout"/> placed and decides no place itself,
/// so the look and the layout change apart. The look is the shared <see cref="PictureLook"/>, drawn with
/// <see cref="PictureParts"/>, and what <see cref="NestedStyle"/> adds for a component's own shapes.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/strategy">Strategy (Refactoring.Guru)</seealso>
internal sealed class NestedRenderer : IDiagramRenderer
{
    /// <summary>
    /// A panel's corner, and how far short of a box a line's arrow stops, so it points at the port on the box's
    /// edge.
    /// </summary>
    private const double Radius = PictureParts.Radius, Short = 5;

    /// <summary>
    /// Where a box's name stands under its top, and its chip's top, the first line of its note, and how much a
    /// stand-in's dashed outline is grown: half a pixel inwards, so it is drawn inside the box's edge.
    /// </summary>
    private const double NameLine = 29, ChipTop = NameLine - NestedSizes.ChipRise, NoteTop = 50, GhostGrown = -.5;

    /// <summary>
    /// A neighbour: how far above it its heading stands, where its chip's top is, how far in from the side the
    /// root is on a thing it gives is written, how far under a row's middle those words stand, and a port's size.
    /// </summary>
    private const double HeadingRise = 10, NeighbourChip = 40, GivenIn = 14, GivenDrop = 3.5, Port = 4;

    /// <summary>
    /// A pill: where its dot stands from its left, the dot's size, where its words start, and how far under its
    /// middle their line is.
    /// </summary>
    private const double DotIn = 22, Dot = 4.5, PillWordsIn = 38, PillWordDrop = 4.5;

    /// <summary>How far above a line its word stands.</summary>
    private const double WordRise = 7;

    /// <summary>This is the nested picture's.</summary>
    public DiagramKind Kind => DiagramKind.Nested;

    /// <summary>The picture as an SVG, drawn from what the layout placed.</summary>
    public string Render(DiagramSource source, IGeometry geometry)
    {
        var components = source.Nested!;
        var placed = NestedLayout.Of(components, geometry);

        // The words in the title's line: the picture's title, and after a colon what the diagram says under it.
        string heading = components.Subtitle is { } under ? $"{source.Title}: {under}" : source.Title;

        // Its layout stands every box under the title's room already, and holds the key. So the body is drawn
        // straight in, and the picture is as tall as the layout says.
        return PictureBuilder.Titled(source.Title, geometry).Heading(heading).Type(TypeNames.Of(source))
            .Draw(writer => Draw(writer, placed, geometry), placed.Width, placed.Height - PictureMeasures.Usual.Head)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, NestedStyle.Rules)
            .Build();
    }

    /// <summary>
    /// Everything the layout placed, back to front: the neighbours, the boxes, the lines, the pills over them,
    /// the pins, and the key.
    /// </summary>
    private static void Draw(SvgWriter writer, PlacedNested placed, IGeometry geometry)
    {
        foreach (var neighbour in placed.Neighbours) DrawNeighbour(writer, neighbour, geometry);
        foreach (var box in placed.Boxes) DrawBox(writer, box, geometry);
        foreach (var line in placed.Lines) DrawLine(writer, line);
        foreach (var box in placed.Boxes.Where(box => box.Pill is not null)) DrawPill(writer, box);
        foreach (var pin in placed.Pins) NestedPins.Draw(writer, pin, ToneOf(placed, pin), "cmp-pin");
        NestedKey.Draw(writer, placed.Key, geometry);
    }

    /// <summary>
    /// A box: its panel, with its name, its chip in its tone, the words at its right and its note. A box that
    /// stands in for another is a dashed outline in its tone, and no panel.
    /// </summary>
    private static void DrawBox(SvgWriter writer, PlacedBox placed, IGeometry geometry)
    {
        var box = placed.Component;
        var at = placed.Box;
        string tone = Tone(box.Tone);
        double words = at.Left + NestedSizes.NameIn, name = at.Top + NameLine;
        if (box.Ghost) writer.Box(at.Grown(GhostGrown), Radius, $"cmp-ghost pic-edge{tone}");
        if (!box.Ghost) PictureParts.Panel(writer, at);

        writer.Text(words, name, box.Name, "pic-name", "start");
        if (box.Kind is { } kind)
        {
            double after = words + NestedSizes.NameWidth(box.Name) + NestedSizes.ChipGap;
            NestedChips.Draw(writer, geometry, after, at.Top + ChipTop, kind, tone);
        }

        if (box.Aside is { } aside) writer.Text(at.Right - NestedSizes.AsideIn, name, aside, "pic-note", "end");
        foreach (var (line, row) in placed.NoteLines.Select((line, row) => (line, row)))
            writer.Text(words, at.Top + NoteTop + row * NestedSizes.NoteLine, line, "pic-note", "start");
    }

    /// <summary>
    /// A neighbour: its heading when it opens a group, then a panel with its name in the code's face, its chip, its
    /// line of code, its note and a row for each thing it gives.
    /// </summary>
    private static void DrawNeighbour(SvgWriter writer, PlacedNeighbour placed, IGeometry geometry)
    {
        var neighbour = placed.Neighbour;
        var at = placed.Box;
        string tone = Tone(neighbour.Tone);
        bool before = neighbour.Side == NeighbourSide.Before;
        double words = at.Left + NeighbourLayout.WordsIn;
        if (neighbour.Heading is { } heading)
            writer.Text(at.Left, at.Top - HeadingRise, heading, "pic-group", "start");
        PictureParts.Panel(writer, at).Text(words, at.Top + NameLine, neighbour.Name, "cmp-code-name", "start");
        if (neighbour.Kind is { } kind)
            NestedChips.Draw(writer, geometry, words, at.Top + NeighbourChip, kind, tone);
        if (neighbour.Code is { } code)
            writer.Text(words, NeighbourLayout.CodeY(placed), code, $"cmp-code pic-ink{tone}", "start");
        foreach (var (line, row) in placed.NoteLines.Select((line, row) => (line, row)))
            writer.Text(words, NeighbourLayout.NoteY(placed, row), line, "pic-note", "start");

        // What it gives: a word a row, at the side the root is on, with a port on that edge.
        double edge = before ? at.Right : at.Left, word = before ? at.Right - GivenIn : at.Left + GivenIn;
        foreach (var (given, row) in (neighbour.Gives ?? []).Select((given, row) => (given, row)))
        {
            double y = NeighbourLayout.RowY(placed, row);
            writer.Text(word, y + GivenDrop, given, $"cmp-pin-text pic-ink{tone}", before ? "end" : "start")
                .Circle(edge, y, Port, $"pic-port pic-edge{tone}");
        }
    }

    /// <summary>The pill of the one thing a box holds, with its dot.</summary>
    private static void DrawPill(SvgWriter writer, PlacedBox placed)
    {
        var pill = placed.Pill!.Box;
        string tone = Tone(placed.Component.Tone);
        writer.Box(pill, pill.HalfHeight, $"cmp-pill pic-tint pic-edge{tone}")
            .Circle(pill.Left + DotIn, pill.Middle.Y, Dot, $"cmp-pulse pic-fill{tone}")
            .Text(pill.Left + PillWordsIn, pill.Middle.Y + PillWordDrop, placed.Pill.Says, "cmp-pill-text", "start");
    }

    /// <summary>
    /// A line with its arrow, the dots that march along it unless it is only a read, its word, and a port on the
    /// box's edge where it leaves the box or arrives at it.
    /// </summary>
    private static void DrawLine(SvgWriter writer, PlacedLine line)
    {
        // A state's line runs from its pill to a pin, and has no port. A neighbour's line has one on the box: a
        // call leaves the box, so its port is where it starts, and every other line arrives, so its port is its end.
        bool ofState = line.Kind is LineKind.Down or LineKind.Up;
        bool leaves = line.Kind == LineKind.Calls, reads = line.Kind == LineKind.Reads;
        IPoint start = line.Points[0], end = line.Points[^1], port = leaves ? start : end;
        string data = LinePath.Of(leaves || ofState ? line.Points : Shortened(line.Points));
        string? tone = line.Tone == DiagramTone.Plain ? null : line.Tone.Wire();
        writer.Path(data, $"pic-line pic-edge{Tone(line.Tone)}{(reads ? " reads" : "")}", true, tone: tone);
        if (!reads) writer.Path(data, "pic-march", false);
        if (!ofState) writer.Circle(port.X, port.Y, Port, $"pic-port pic-edge{Tone(line.Tone)}");

        // What a neighbour gives is written in the neighbour. Any other word stands over its line.
        string? word = reads ? DiagramText.Format(DrawingMessage.NestedReads) : line.Word;
        if (line.Kind == LineKind.Gives || word is null) return;
        writer.Text((start.X + end.X) / 2, start.Y - WordRise, word, "pic-word");
    }

    /// <summary>A line's points with the last pulled back a little, so its arrow stops at the port's rim.</summary>
    private static IReadOnlyList<IPoint> Shortened(IReadOnlyList<IPoint> points) =>
        [.. points.Take(points.Count - 1), points[^1].Toward(points[^2], Short)];

    /// <summary>The tone of the box a pin stands on, as a shape's class.</summary>
    private static string ToneOf(PlacedNested placed, PlacedPin pin) =>
        Tone(placed.Boxes.First(box => box.Component.Id == pin.On).Component.Tone);

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    private static string Tone(DiagramTone tone) => PictureParts.Tone(tone);
}
