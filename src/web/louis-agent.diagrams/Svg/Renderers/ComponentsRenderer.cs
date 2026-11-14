using LouisAgent.Choices;
using LouisAgent.Diagrams.Components;
using LouisAgent.Diagrams.Components.Placed;
using LouisAgent.Diagrams.Components.Placed.Lines;
using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a component diagram (F134.7): a box inside a box for what holds what, pins on a box's top edge for what
/// is handed down and said back up, a pill for the one thing a box holds, and the neighbours beside the root.
/// </summary>
/// <remarks>
/// Strategy, as every kind's renderer is: one <see cref="IDiagramRenderer"/> a kind, picked by
/// <see cref="DiagramRenderers"/>. It draws what <see cref="ComponentLayout"/> placed and decides no place itself,
/// so the look and the layout change apart. The look is <see cref="ComponentsStyle"/>'s.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/strategy">Strategy (Refactoring.Guru)</seealso>
internal sealed class ComponentsRenderer : IDiagramRenderer
{
    /// <summary>
    /// A panel's corner, the room the key takes under the drawing, and how far apart the ground's dots are.
    /// </summary>
    private const double Radius = 14, KeyRow = 44, DotStep = 22;

    /// <summary>This is the component diagram's.</summary>
    public DiagramKind Kind => DiagramKind.Components;

    /// <summary>The picture as an SVG, drawn from what the layout placed.</summary>
    public string Render(DiagramSource source)
    {
        var placed = ComponentLayout.Of(source.Components!);
        double height = placed.Height + KeyRow;
        var writer = new SvgWriter().Path(Dots(placed.Width, height), "cmp-dots", false);

        foreach (var neighbour in placed.Neighbours) DrawNeighbour(writer, neighbour);
        foreach (var box in placed.Boxes) DrawBox(writer, box);
        foreach (var line in placed.Lines) DrawLine(writer, line);
        foreach (var box in placed.Boxes.Where(box => box.Pill is not null)) DrawPill(writer, box);
        foreach (var pin in placed.Pins) DrawPin(writer, pin, ToneOf(placed, pin), "cmp-pin");
        DrawKey(writer, placed.Height + KeyRow - ComponentSizes.PinHeight);

        return writer.Write(SvgTheme.Site, source.Title, placed.Width, height, ComponentsStyle.Of(SvgTheme.Site));
    }

    /// <summary>A box: its panel by its depth, a marker in its tone, its name, its chip and its note.</summary>
    private static void DrawBox(SvgWriter writer, PlacedBox placed)
    {
        var box = placed.Box;
        string tone = Tone(box.Tone);
        int depth = Math.Min(placed.Depth, ComponentsStyle.Deepest);
        string ghost = $"cmp-ghost cmp-edge{tone}";
        if (box.Ghost) writer.Box(placed.X + .5, placed.Y + .5, placed.Width - 1, placed.Height - 1, Radius, ghost);
        if (!box.Ghost) Panel(writer, placed.X, placed.Y, placed.Width, placed.Height, depth, tone);

        writer.Text(placed.X + 22, placed.Y + 29, box.Name, "cmp-name", "start");
        if (box.Kind is { } kind)
            Chip(writer, placed.X + 22 + ComponentSizes.NameWidth(box.Name) + 8, placed.Y + 16, kind, tone);
        foreach (var (line, at) in placed.NoteLines.Select((line, at) => (line, at)))
            writer.Text(placed.X + 22, placed.Y + 50 + at * ComponentSizes.NoteLine, line, "cmp-note", "start");
    }

    /// <summary>
    /// A neighbour: a panel with its name in the code's face, its chip, its note and a row for each thing it gives.
    /// </summary>
    private static void DrawNeighbour(SvgWriter writer, PlacedNeighbour placed)
    {
        var neighbour = placed.Neighbour;
        string tone = Tone(neighbour.Tone);
        bool before = neighbour.Side == NeighbourSide.Before;
        Panel(writer, placed.X, placed.Y, placed.Width, placed.Height, 1, tone);
        writer.Text(placed.X + 22, placed.Y + 29, neighbour.Name, "cmp-code-name", "start");
        if (neighbour.Kind is { } kind) Chip(writer, placed.X + 22, placed.Y + 40, kind, tone);
        foreach (var (line, at) in placed.NoteLines.Select((line, at) => (line, at)))
            writer.Text(placed.X + 22, NeighbourLayout.NoteY(placed, at), line, "cmp-note", "start");

        // What it gives: a word a row, at the side the root is on, with a port on that edge.
        double edge = before ? placed.Right : placed.X, word = before ? placed.Right - 14 : placed.X + 14;
        foreach (var (given, at) in (neighbour.Gives ?? []).Select((given, at) => (given, at)))
        {
            double y = NeighbourLayout.RowY(placed, at);
            writer.Text(word, y + 3.5, given, $"cmp-pin-text cmp-ink{tone}", before ? "end" : "start")
                .Circle(edge, y, 4, $"cmp-port cmp-edge{tone}");
        }
    }

    /// <summary>A panel with its marker: the shape a box and a neighbour share.</summary>
    private static void Panel(SvgWriter writer, double x, double y, double width, double height, int depth,
        string tone) => writer
        .Box(x, y, width, height, Radius, $"cmp-panel depth-{depth} cmp-edge{tone}")
        .Box(x + 7, y + 14, 4, Math.Min(height - 28, 22), 2, $"cmp-fill{tone}");

    /// <summary>A chip: a few words in the box's tone.</summary>
    private static void Chip(SvgWriter writer, double x, double y, string words, string tone) => writer
        .Box(x, y, ComponentSizes.ChipWidth(words), 18, 9, $"cmp-tint{tone}")
        .Text(x + 8, y + 12.5, words, $"cmp-chip-text cmp-ink{tone}", "start");

    /// <summary>The pill of the one thing a box holds, with its dot.</summary>
    private static void DrawPill(SvgWriter writer, PlacedBox placed)
    {
        var pill = placed.Pill!;
        string tone = Tone(placed.Box.Tone);
        writer.Box(pill.X, pill.Y, pill.Width, pill.Height, pill.Height / 2, $"cmp-pill cmp-tint cmp-edge{tone}")
            .Circle(pill.X + 22, pill.Y + pill.Height / 2, 4.5, $"cmp-pulse cmp-fill{tone}")
            .Text(pill.X + 38, pill.Y + pill.Height / 2 + 4.5, pill.Says, "cmp-pill-text", "start");
    }

    /// <summary>
    /// A pin: filled with an arrow down for what is handed down, hollow with an arrow up for what is said back up.
    /// </summary>
    private static void DrawPin(SvgWriter writer, PlacedPin pin, string tone, string cls)
    {
        double half = ComponentSizes.PinHeight / 2, arrow = pin.X + 12;
        string look = pin.Up ? $"{cls} cmp-edge{tone} up" : $"{cls} cmp-edge cmp-tint{tone}";
        string tip = pin.Up
            ? $"M{N(arrow - 4)} {N(pin.Y + 2.5)}L{N(arrow)} {N(pin.Y - 3.5)}L{N(arrow + 4)} {N(pin.Y + 2.5)}Z"
            : $"M{N(arrow - 4)} {N(pin.Y - 3)}L{N(arrow)} {N(pin.Y + 3)}L{N(arrow + 4)} {N(pin.Y - 3)}Z";
        writer.Box(pin.X, pin.Y - half, pin.Width, ComponentSizes.PinHeight, half, look)
            .Path(tip, $"cmp-fill{tone}", false)
            .Text(arrow + 9, pin.Y + 3.5, pin.Name, $"cmp-pin-text cmp-ink{tone}", "start");
    }

    /// <summary>A line with its arrow, the dots that march along it unless it is only a read, and its word.</summary>
    private static void DrawLine(SvgWriter writer, PlacedLine line)
    {
        string data = LinePath.Of(line.Points);
        bool reads = line.Kind == LineKind.Reads;
        string? tone = line.Tone == DiagramTone.Plain ? null : line.Tone.Wire();
        writer.Path(data, $"cmp-line cmp-edge{Tone(line.Tone)}{(reads ? " reads" : "")}", true, tone: tone);
        if (!reads) writer.Path(data, "cmp-march", false);

        // What a neighbour gives is written in the neighbour. Any other word stands over its line.
        string? word = reads ? DiagramText.Format(DiagramMessage.ComponentReads) : line.Word;
        if (line.Kind == LineKind.Gives || word is null) return;
        writer.Text((line.Points[0].X + line.Points[^1].X) / 2, line.Points[0].Y - 7, word, "cmp-word");
    }

    /// <summary>The key under the drawing: what a pin's arrow means, each way.</summary>
    private static void DrawKey(SvgWriter writer, double y)
    {
        string down = DiagramText.Format(DiagramMessage.ComponentHandedDown);
        string up = DiagramText.Format(DiagramMessage.ComponentSaidBackUp);
        var handedDown = new PlacedPin("", down, false, 0, y, ComponentSizes.PinWidth(down));
        var saidBackUp = new PlacedPin("", up, true, handedDown.Width + 12, y, ComponentSizes.PinWidth(up));
        DrawPin(writer, handedDown, "", "cmp-key cmp-pin");
        DrawPin(writer, saidBackUp, "", "cmp-key cmp-pin");
    }

    /// <summary>The ground's dots: a row of them every so far, each row one dashed line with round ends.</summary>
    private static string Dots(double width, double height) => string.Concat(
        Enumerable.Range(0, (int)(height / DotStep) + 1)
            .Select(row => $"M0 {N(row * DotStep + DotStep / 2)}H{N(width)}"));

    /// <summary>The tone of the box a pin stands on, as a shape's class.</summary>
    private static string ToneOf(PlacedComponents placed, PlacedPin pin) =>
        Tone(placed.Boxes.First(box => box.Box.Id == pin.Box).Box.Tone);

    /// <summary>
    /// A tone as a shape's class, with the space before it. Nothing for plain, which the look draws grey.
    /// </summary>
    private static string Tone(DiagramTone tone) => tone == DiagramTone.Plain ? "" : $" tone-{tone.Wire()}";

    /// <summary>A number as the SVG writes it.</summary>
    private static string N(double value) => SvgWriter.N(value);
}
