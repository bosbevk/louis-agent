using LouisAgent.Choices;
using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Composite;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Arrows;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Engine.Types;
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

    /// <summary>
    /// How far in from the frame's left its name starts, the room between the name and its chip, how far above
    /// the name's line a chip's top is, and how far in from the frame's right what stands aside ends.
    /// </summary>
    private const double NameIn = 22, ChipGap = 8, ChipRise = 13, AsideIn = 16;

    /// <summary>
    /// Where a chip's words start in it, across and down, and how far under a pin's middle its words' line is.
    /// </summary>
    private const double ChipTextIn = 8, ChipTextLine = 12.5, LabelDrop = 4;

    /// <summary>The composite a source holds, as one SVG file's text, drawn with a geometry.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var spec = source.Composite!;
        var placed = GraphLayout.Place(CompositeFlow.Of(spec), bend: GraphLayout.SoftBend, rules: FlowRules.Straight,
            geometry: geometry);
        string tone = PictureParts.Tone(spec.Tone);

        // The class is the body, and the key goes across under it, under a rule the accent's edge fills. Its
        // styles are the looks it borrows, a flow's word and a component's pin and chip, and last its own.
        return PictureBuilder.Titled(source.Title)
            .Type(TypeNames.Of(source))
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Body("cps", Class(spec, placed, geometry), placed.Width, placed.Height)
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
    private static SvgWriter Class(CompositeSpec spec, PlacedGraph placed, IGeometry geometry)
    {
        var writer = new SvgWriter();
        var nodeOf = placed.Nodes.ToDictionary(node => node.Node.Id, StringComparer.Ordinal);
        var ports = (spec.Ports ?? []).Select(port => (Port: port, Node: nodeOf[CompositeFlow.PortId(port.Name)]))
            .ToList();
        var parts = spec.Parts.Select(part => (Part: part, Node: nodeOf[part.Id])).ToList();
        var asked = ports.Where(port => port.Port.Side == PortSide.In).ToList();
        var needed = ports.Where(port => port.Port.Side == PortSide.Out).ToList();

        // The frame's left edge runs through the middle of the widest pin of what is asked, which ends where the
        // ports' boxes end, and its right edge through the middle of the widest pin of what is needed.
        double left = asked.Count == 0
            ? parts.Min(part => part.Node.Box.Left) - Clear
            : asked.Max(port => port.Node.Box.Right) - asked.Max(port => PinWidth(port.Port)) / 2;
        double right = needed.Count == 0
            ? parts.Max(part => part.Node.Box.Right) + Clear
            : needed.Min(port => port.Node.Box.Left) + needed.Max(port => PinWidth(port.Port)) / 2;
        var around = placed.Groups[0].Box;
        Frame(writer, spec, geometry.Box(left, around.Top, right - left, around.Height), geometry);

        var sideOf = ports.ToDictionary(port => port.Node.Node.Id, port => port, StringComparer.Ordinal);
        foreach (var edge in placed.Edges) Line(writer, spec, edge, sideOf, left, right);

        // Every line first, then the words: two lines that leave one part share their first stretch, so a word
        // drawn with its own line would have the next line across it.
        foreach (var edge in placed.Edges.Where(edge => edge.Edge.Word is not null))
            FlowDrawing.Word(writer, edge, "", geometry);
        foreach (var (part, node) in parts) Part(writer, part, node, PictureParts.Tone(spec.Tone), geometry);
        foreach (var (port, node) in ports)
            Pin(writer, spec, port, node.Box, port.Side == PortSide.In ? left : right);
        return writer;
    }

    /// <summary>
    /// The class's frame: a panel as tall as the layout's own frame, between the two edges, with the class's name,
    /// the chip of what it is, and where it is at the right.
    /// </summary>
    private static void Frame(SvgWriter writer, CompositeSpec spec, IBox frame, IGeometry geometry)
    {
        string tone = PictureParts.Tone(spec.Tone);
        double line = frame.Top + NameLine;
        PictureParts.Panel(writer, frame).Text(frame.Left + NameIn, line, spec.Name, "pic-name", "start");
        if (spec.Kind is { } kind)
        {
            double after = frame.Left + NameIn + ComponentSizes.NameWidth(spec.Name) + ChipGap;
            Chip(writer, geometry.Box(after, line - ChipRise, ComponentSizes.ChipWidth(kind), ChipHeight), kind, tone);
        }

        if (spec.Aside is { } aside) writer.Text(frame.Right - AsideIn, line, aside, "pic-note", "end");
    }

    /// <summary>
    /// A line. Between two parts it is plain. From a pin it starts at the pin's end, and to a pin it ends there:
    /// those run, with an arrowhead and dots, in the pin's colour.
    /// </summary>
    /// <remarks>Guard clauses, one kind of line each (ADR 0051).</remarks>
    private static void Line(SvgWriter writer, CompositeSpec spec, PlacedEdge edge,
        Dictionary<string, (CompositePort Port, PlacedNode Node)> ports, double left, double right)
    {
        if (ports.TryGetValue(edge.Edge.From, out var from))
        {
            double start = left + PinWidth(from.Port) / 2;
            var way = ArrowPath.From(start, from.Node.Box.Middle.Y).Then(edge.Way);
            Passed(writer, way, CompositeFlow.ToneOf(spec, from.Port));
            return;
        }

        if (ports.TryGetValue(edge.Edge.To, out var to))
        {
            double end = right - PinWidth(to.Port) / 2;
            Passed(writer, edge.Way.To(end, to.Node.Box.Middle.Y), CompositeFlow.ToneOf(spec, to.Port));
            return;
        }

        writer.Path(edge.Way.Text, "cps-wire", arrow: false);
    }

    /// <summary>What is passed on: a line with its arrowhead, and the dots that run along it.</summary>
    private static void Passed(SvgWriter writer, ArrowPath way, DiagramTone tone)
    {
        string? name = tone == DiagramTone.Plain ? null : tone.Wire();
        writer.Path(way.Text, $"pic-line pic-edge{PictureParts.Tone(tone)}", arrow: true, tone: name)
            .Path(way.Text, "pic-march", arrow: false);
    }

    /// <summary>
    /// A part: a panel with its type large at the left, its role in a chip at the right, and under them a line of
    /// what it does.
    /// </summary>
    private static void Part(SvgWriter writer, CompositePart part, PlacedNode node, string tone, IGeometry geometry)
    {
        var box = node.Box;
        double line = box.Top + FlowText.LabelY(node), words = box.Left + CompositeFlow.Inset;
        writer.Box(box, Corner, "pic-panel").Text(words, line, part.Type, "pic-name", "start");
        if (part.Role is { } role)
        {
            double wide = ComponentSizes.ChipWidth(role);
            double chipLeft = box.Right - CompositeFlow.Inset - wide;
            Chip(writer, geometry.Box(chipLeft, line - ChipRise, wide, ChipHeight), role, tone);
        }

        var note = part.Note is { } said ? GraphLayout.NoteLines(said) : [];
        for (int at = 0; at < note.Count; at++)
            writer.Text(words, box.Top + FlowText.NoteY(node) + FlowText.NoteStep * at, note[at], "pic-note",
                "start");
    }

    /// <summary>
    /// A port: its pin, its middle on the frame's edge, down for what is asked and up for what is needed, and
    /// outside the frame what is asked or needed in the code's words.
    /// </summary>
    private static void Pin(SvgWriter writer, CompositeSpec spec, CompositePort port, IBox box, double edge)
    {
        bool needs = port.Side == PortSide.Out;
        string tone = PictureParts.Tone(CompositeFlow.ToneOf(spec, port));
        double wide = PinWidth(port), y = box.Middle.Y;
        var pin = new PlacedPin("", port.Name, needs, edge - wide / 2, y, wide);
        ComponentPins.Draw(writer, pin, tone, "cmp-pin");
        if (port.Label is not { } label) return;

        double beside = needs ? pin.X + wide + CompositeFlow.Beside : pin.X - CompositeFlow.Beside;
        writer.Text(beside, y + LabelDrop, label, $"cps-words pic-ink{tone}", needs ? "start" : "end");
    }

    /// <summary>A chip: a few words in the class's tone, in a box as round as it is tall.</summary>
    private static void Chip(SvgWriter writer, IBox chip, string words, string tone) => writer
        .Box(chip, chip.HalfHeight, $"pic-tint{tone}")
        .Text(chip.Left + ChipTextIn, chip.Top + ChipTextLine, words, $"cmp-chip-text pic-ink{tone}", "start");

    /// <summary>How wide a port's pin is.</summary>
    private static double PinWidth(CompositePort port) => ComponentSizes.PinWidth(port.Name);
}
