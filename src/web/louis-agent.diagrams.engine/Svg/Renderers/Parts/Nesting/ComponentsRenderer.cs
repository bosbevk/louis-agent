using LouisAgent.Choices;
using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws a component diagram (F134.7): its title as one small line over a soft light, a box inside a box for what
/// holds what, pins on a box's top edge for what is handed down and said back up, a pill for the one thing a box
/// holds, the neighbours beside the root, and the key that says how to read it.
/// </summary>
/// <remarks>
/// Strategy, as every kind's renderer is: one <see cref="IDiagramRenderer"/> a kind, picked by
/// <see cref="DiagramRenderers"/>. It draws what <see cref="ComponentLayout"/> placed and decides no place itself,
/// so the look and the layout change apart. The look is the shared <see cref="PictureLook"/>, drawn with
/// <see cref="PictureParts"/>, and what <see cref="ComponentsStyle"/> adds for a component's own shapes.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/strategy">Strategy (Refactoring.Guru)</seealso>
internal sealed class ComponentsRenderer : IDiagramRenderer
{
    /// <summary>
    /// A panel's corner, and how far short of a box a line's arrow stops, so it points at the port on the box's
    /// edge.
    /// </summary>
    private const double Radius = PictureParts.Radius, Short = 5;

    /// <summary>This is the component diagram's.</summary>
    public DiagramKind Kind => DiagramKind.Components;

    /// <summary>The picture as an SVG, drawn from what the layout placed.</summary>
    public string Render(DiagramSource source)
    {
        var components = source.Components!;
        var placed = ComponentLayout.Of(components);

        // The words in the title's line: the picture's title, and after a colon what the diagram says under it.
        string heading = components.Subtitle is { } under ? $"{source.Title}: {under}" : source.Title;

        // Its layout stands every box under the title's room already, and holds the key. So the body is drawn
        // straight in, and the picture is as tall as the layout says.
        return PictureBuilder.Titled(source.Title).Heading(heading).Type(TypeNames.Of(source))
            .Draw(writer => Draw(writer, placed), placed.Width, placed.Height - PictureMeasures.Usual.Head)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, ComponentsStyle.Rules)
            .Build();
    }

    /// <summary>
    /// Everything the layout placed, back to front: the neighbours, the boxes, the lines, the pills over them,
    /// the pins, and the key.
    /// </summary>
    private static void Draw(SvgWriter writer, PlacedComponents placed)
    {
        foreach (var neighbour in placed.Neighbours) DrawNeighbour(writer, neighbour);
        foreach (var box in placed.Boxes) DrawBox(writer, box);
        foreach (var line in placed.Lines) DrawLine(writer, line);
        foreach (var box in placed.Boxes.Where(box => box.Pill is not null)) DrawPill(writer, box);
        foreach (var pin in placed.Pins) ComponentPins.Draw(writer, pin, ToneOf(placed, pin), "cmp-pin");
        ComponentsKey.Draw(writer, placed.Key);
    }

    /// <summary>
    /// A box: its panel, with its name, its chip in its tone, the words at its right and its note. A box that
    /// stands in for another is a dashed outline in its tone, and no panel.
    /// </summary>
    private static void DrawBox(SvgWriter writer, PlacedBox placed)
    {
        var box = placed.Box;
        string tone = Tone(box.Tone), ghost = $"cmp-ghost pic-edge{tone}";
        if (box.Ghost) writer.Box(placed.X + .5, placed.Y + .5, placed.Width - 1, placed.Height - 1, Radius, ghost);
        if (!box.Ghost) PictureParts.Panel(writer, placed.X, placed.Y, placed.Width, placed.Height);

        writer.Text(placed.X + 22, placed.Y + 29, box.Name, "pic-name", "start");
        if (box.Kind is { } kind)
            Chip(writer, placed.X + 22 + ComponentSizes.NameWidth(box.Name) + 8, placed.Y + 16, kind, tone);
        if (box.Aside is { } aside) writer.Text(placed.Right - 16, placed.Y + 29, aside, "pic-note", "end");
        foreach (var (line, at) in placed.NoteLines.Select((line, at) => (line, at)))
            writer.Text(placed.X + 22, placed.Y + 50 + at * ComponentSizes.NoteLine, line, "pic-note", "start");
    }

    /// <summary>
    /// A neighbour: its heading when it opens a group, then a panel with its name in the code's face, its chip, its
    /// line of code, its note and a row for each thing it gives.
    /// </summary>
    private static void DrawNeighbour(SvgWriter writer, PlacedNeighbour placed)
    {
        var neighbour = placed.Neighbour;
        string tone = Tone(neighbour.Tone);
        bool before = neighbour.Side == NeighbourSide.Before;
        if (neighbour.Heading is { } heading) writer.Text(placed.X, placed.Y - 10, heading, "pic-group", "start");
        PictureParts.Panel(writer, placed.X, placed.Y, placed.Width, placed.Height);
        writer.Text(placed.X + 22, placed.Y + 29, neighbour.Name, "cmp-code-name", "start");
        if (neighbour.Kind is { } kind) Chip(writer, placed.X + 22, placed.Y + 40, kind, tone);
        if (neighbour.Code is { } code)
            writer.Text(placed.X + 22, NeighbourLayout.CodeY(placed), code, $"cmp-code pic-ink{tone}", "start");
        foreach (var (line, at) in placed.NoteLines.Select((line, at) => (line, at)))
            writer.Text(placed.X + 22, NeighbourLayout.NoteY(placed, at), line, "pic-note", "start");

        // What it gives: a word a row, at the side the root is on, with a port on that edge.
        double edge = before ? placed.Right : placed.X, word = before ? placed.Right - 14 : placed.X + 14;
        foreach (var (given, at) in (neighbour.Gives ?? []).Select((given, at) => (given, at)))
        {
            double y = NeighbourLayout.RowY(placed, at);
            writer.Text(word, y + 3.5, given, $"cmp-pin-text pic-ink{tone}", before ? "end" : "start")
                .Circle(edge, y, 4, $"pic-port pic-edge{tone}");
        }
    }

    /// <summary>A chip: a few words in the box's tone.</summary>
    private static void Chip(SvgWriter writer, double x, double y, string words, string tone) => writer
        .Box(x, y, ComponentSizes.ChipWidth(words), 18, 9, $"pic-tint{tone}")
        .Text(x + 8, y + 12.5, words, $"cmp-chip-text pic-ink{tone}", "start");

    /// <summary>The pill of the one thing a box holds, with its dot.</summary>
    private static void DrawPill(SvgWriter writer, PlacedBox placed)
    {
        var pill = placed.Pill!;
        string tone = Tone(placed.Box.Tone);
        writer.Box(pill.X, pill.Y, pill.Width, pill.Height, pill.Height / 2, $"cmp-pill pic-tint pic-edge{tone}")
            .Circle(pill.X + 22, pill.Y + pill.Height / 2, 4.5, $"cmp-pulse pic-fill{tone}")
            .Text(pill.X + 38, pill.Y + pill.Height / 2 + 4.5, pill.Says, "cmp-pill-text", "start");
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
        var port = leaves ? line.Points[0] : line.Points[^1];
        string data = LinePath.Of(leaves || ofState ? line.Points : Shortened(line.Points));
        string? tone = line.Tone == DiagramTone.Plain ? null : line.Tone.Wire();
        writer.Path(data, $"pic-line pic-edge{Tone(line.Tone)}{(reads ? " reads" : "")}", true, tone: tone);
        if (!reads) writer.Path(data, "pic-march", false);
        if (!ofState) writer.Circle(port.X, port.Y, 4, $"pic-port pic-edge{Tone(line.Tone)}");

        // What a neighbour gives is written in the neighbour. Any other word stands over its line.
        string? word = reads ? DiagramText.Format(DrawingMessage.ComponentReads) : line.Word;
        if (line.Kind == LineKind.Gives || word is null) return;
        writer.Text((line.Points[0].X + line.Points[^1].X) / 2, line.Points[0].Y - 7, word, "pic-word");
    }

    /// <summary>A line's points with the last pulled back a little, so its arrow stops at the port's rim.</summary>
    private static IReadOnlyList<LinePoint> Shortened(IReadOnlyList<LinePoint> points)
    {
        var last = points[^1];
        var before = points[^2];
        double x = last.X - Short * Math.Sign(last.X - before.X), y = last.Y - Short * Math.Sign(last.Y - before.Y);
        return [.. points.Take(points.Count - 1), new LinePoint(x, y)];
    }

    /// <summary>The tone of the box a pin stands on, as a shape's class.</summary>
    private static string ToneOf(PlacedComponents placed, PlacedPin pin) =>
        Tone(placed.Boxes.First(box => box.Box.Id == pin.Box).Box.Tone);

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    private static string Tone(DiagramTone tone) => PictureParts.Tone(tone);
}
