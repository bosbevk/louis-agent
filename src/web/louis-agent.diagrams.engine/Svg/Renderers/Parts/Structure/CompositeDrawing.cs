using LouisAgent.Choices;
using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Composite;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;

/// <summary>
/// Draws a composite structure diagram (F134.13, Louis's pick "Pins"): the class as a frame with its name, its
/// parts inside it, a pin on the frame's edge for each port with what is asked or needed written outside it, a
/// plain line between two parts that work together, a line that runs for what is passed on, and the key.
/// </summary>
/// <remarks>
/// It draws what <see cref="GraphLayout"/> placed for the flow <see cref="CompositeFlow"/> made of the class, and
/// decides one place itself: the frame. The layout's frame goes round every box, the ports' too. This one goes
/// round the parts, and its left and right edges run through the middle of the pins, since a port stands on the
/// class's edge. A pin is the component diagram's own (<see cref="ComponentPins"/>), a line's word the flow's.
/// </remarks>
internal static class CompositeDrawing
{
    /// <summary>
    /// A part's corner, where the class's name stands under the frame's top, a chip's height, and how far the
    /// frame stands from its parts where it has no pins.
    /// </summary>
    private const double Corner = 10, NameLine = 23, ChipHeight = 18, Clear = 12;

    /// <summary>The composite a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source)
    {
        var spec = source.Composite!;
        var placed = GraphLayout.Place(CompositeFlow.Of(spec), bend: GraphLayout.SoftBend, rules: FlowRules.Straight);
        string tone = PictureParts.Tone(spec.Tone);

        // The class is the body, and the key goes across under it, under a rule the accent's edge fills. Its
        // styles are the looks it borrows, a flow's word and a component's pin and chip, and last its own.
        return PictureBuilder.Titled(source.Title)
            .Type(DiagramText.Format(DrawingMessage.CompositeType))
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Body("cps", Class(spec, placed), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = CompositeKey.Of(spec, width);
                return (height, (writer, top) => CompositeKey.Draw(writer, items, tone, top, width));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, FlowStyle.Rules, ComponentsStyle.Rules,
                CompositeStyle.Rules)
            .Build();
    }

    /// <summary>
    /// The class, from where the layout starts, back to front: its frame and name, the lines, their words in a
    /// flow's pill, the parts over the lines' ends, and last the pins on the frame's edge.
    /// </summary>
    private static SvgWriter Class(CompositeSpec spec, PlacedGraph placed)
    {
        var writer = new SvgWriter();
        var boxOf = placed.Nodes.ToDictionary(node => node.Node.Id, StringComparer.Ordinal);
        var ports = (spec.Ports ?? []).Select(port => (Port: port, Box: boxOf[CompositeFlow.PortId(port.Name)]))
            .ToList();
        var parts = spec.Parts.Select(part => (Part: part, Box: boxOf[part.Id])).ToList();
        var asked = ports.Where(port => port.Port.Side == PortSide.In).ToList();
        var needed = ports.Where(port => port.Port.Side == PortSide.Out).ToList();

        // The frame's left edge runs through the middle of the widest pin of what is asked, which ends where the
        // ports' boxes end, and its right edge through the middle of the widest pin of what is needed.
        double left = asked.Count == 0
            ? parts.Min(part => part.Box.X) - Clear
            : asked.Max(port => port.Box.X + port.Box.W) - asked.Max(port => PinWidth(port.Port)) / 2;
        double right = needed.Count == 0
            ? parts.Max(part => part.Box.X + part.Box.W) + Clear
            : needed.Min(port => port.Box.X) + needed.Max(port => PinWidth(port.Port)) / 2;
        Frame(writer, spec, placed.Groups[0], left, right);

        var sideOf = ports.ToDictionary(port => port.Box.Node.Id, port => port, StringComparer.Ordinal);
        foreach (var edge in placed.Edges) Line(writer, spec, edge, sideOf, left, right);

        // Every line first, then the words: two lines that leave one part share their first stretch, so a word
        // drawn with its own line would have the next line across it.
        foreach (var edge in placed.Edges.Where(edge => edge.Edge.Word is not null))
            FlowDrawing.Word(writer, edge, "");
        foreach (var (part, box) in parts) Part(writer, part, box, PictureParts.Tone(spec.Tone));
        foreach (var (port, box) in ports) Pin(writer, spec, port, box, port.Side == PortSide.In ? left : right);
        return writer;
    }

    /// <summary>
    /// The class's frame: a panel as tall as the layout's own frame, between the two edges, with the class's name,
    /// the chip of what it is, and where it is at the right.
    /// </summary>
    private static void Frame(SvgWriter writer, CompositeSpec spec, PlacedGroup group, double left, double right)
    {
        string tone = PictureParts.Tone(spec.Tone);
        PictureParts.Panel(writer, left, group.Y, right - left, group.H)
            .Text(left + 22, group.Y + NameLine, spec.Name, "pic-name", "start");
        if (spec.Kind is { } kind)
            Chip(writer, left + 22 + ComponentSizes.NameWidth(spec.Name) + 8, group.Y + NameLine - 13, kind, tone);
        if (spec.Aside is { } aside) writer.Text(right - 16, group.Y + NameLine, aside, "pic-note", "end");
    }

    /// <summary>
    /// A line. Between two parts it is plain. From a pin it starts at the pin's end, and to a pin it ends there:
    /// those run, with an arrowhead and dots, in the pin's colour.
    /// </summary>
    /// <remarks>Guard clauses, one kind of line each (ADR 0051).</remarks>
    private static void Line(SvgWriter writer, CompositeSpec spec, PlacedEdge edge,
        Dictionary<string, (CompositePort Port, PlacedNode Box)> ports, double left, double right)
    {
        if (ports.TryGetValue(edge.Edge.From, out var from))
        {
            double y = from.Box.Y + from.Box.H / 2, start = left + PinWidth(from.Port) / 2;
            Passed(writer, $"M {N(start)} {N(y)} L{edge.Path[1..]}", CompositeFlow.ToneOf(spec, from.Port));
            return;
        }

        if (ports.TryGetValue(edge.Edge.To, out var to))
        {
            double y = to.Box.Y + to.Box.H / 2, end = right - PinWidth(to.Port) / 2;
            Passed(writer, $"{edge.Path} L {N(end)} {N(y)}", CompositeFlow.ToneOf(spec, to.Port));
            return;
        }

        writer.Path(edge.Path, "cps-wire", arrow: false);
    }

    /// <summary>What is passed on: a line with its arrowhead, and the dots that run along it.</summary>
    private static void Passed(SvgWriter writer, string path, DiagramTone tone)
    {
        string? name = tone == DiagramTone.Plain ? null : tone.Wire();
        writer.Path(path, $"pic-line pic-edge{PictureParts.Tone(tone)}", arrow: true, tone: name)
            .Path(path, "pic-march", arrow: false);
    }

    /// <summary>
    /// A part: a panel with its type large at the left, its role in a chip at the right, and under them a line of
    /// what it does.
    /// </summary>
    private static void Part(SvgWriter writer, CompositePart part, PlacedNode box, string tone)
    {
        double line = box.Y + FlowText.LabelY(box);
        writer.Box(box.X, box.Y, box.W, box.H, Corner, "pic-panel")
            .Text(box.X + CompositeFlow.Inset, line, part.Type, "pic-name", "start");
        if (part.Role is { } role)
            Chip(writer, box.X + box.W - CompositeFlow.Inset - ComponentSizes.ChipWidth(role), line - 13, role, tone);

        var note = part.Note is { } words ? GraphLayout.NoteLines(words) : [];
        for (int at = 0; at < note.Count; at++)
            writer.Text(box.X + CompositeFlow.Inset, box.Y + FlowText.NoteY(box) + FlowText.NoteStep * at, note[at],
                "pic-note", "start");
    }

    /// <summary>
    /// A port: its pin, its middle on the frame's edge, down for what is asked and up for what is needed, and
    /// outside the frame what is asked or needed in the code's words.
    /// </summary>
    private static void Pin(SvgWriter writer, CompositeSpec spec, CompositePort port, PlacedNode box, double edge)
    {
        bool needs = port.Side == PortSide.Out;
        string tone = PictureParts.Tone(CompositeFlow.ToneOf(spec, port));
        double wide = PinWidth(port), y = box.Y + box.H / 2;
        var pin = new PlacedPin("", port.Name, needs, edge - wide / 2, y, wide);
        ComponentPins.Draw(writer, pin, tone, "cmp-pin");
        if (port.Label is not { } label) return;

        double beside = needs ? pin.X + wide + CompositeFlow.Beside : pin.X - CompositeFlow.Beside;
        writer.Text(beside, y + 4, label, $"cps-words pic-ink{tone}", needs ? "start" : "end");
    }

    /// <summary>A chip: a few words in the class's tone.</summary>
    private static void Chip(SvgWriter writer, double x, double y, string words, string tone) => writer
        .Box(x, y, ComponentSizes.ChipWidth(words), ChipHeight, ChipHeight / 2, $"pic-tint{tone}")
        .Text(x + 8, y + 12.5, words, $"cmp-chip-text pic-ink{tone}", "start");

    /// <summary>How wide a port's pin is.</summary>
    private static double PinWidth(CompositePort port) => ComponentSizes.PinWidth(port.Name);

    /// <summary>A number as the SVG writes it.</summary>
    private static string N(double value) => SvgWriter.N(value);
}
