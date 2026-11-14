using LouisAgent.Diagrams.Components;
using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Composite;

/// <summary>
/// A composite structure diagram as a flow (F134.13), so the flow's layout places it: every part and every port a
/// box in one group, which is the class, and every connector and port an arrow.
/// </summary>
/// <remarks>
/// Adapter (Refactoring.Guru, Gang of Four), Louis's pick of three, as a class chart has in <c>ClassFlow</c>. A
/// composite gets no layout of its own, so every fix the flow's layout and routing get reaches it too. A layout
/// of its own would have been a second router to write and to keep. What is asked of the class stands in the
/// first column, the parts after it by which uses which, and what the class needs in the last, so the picture
/// reads from what comes in to what goes out. A box's label is what the layout measures, so it is padded to the
/// room the drawing needs for the two things it draws there.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/adapter">Adapter (Refactoring.Guru)</seealso>
public static class CompositeFlow
{
    /// <summary>The id of the one group every box of a composite is in: the class.</summary>
    public const string Class = "class";

    /// <summary>
    /// How far in from a box's side its words start, the room between two things drawn side by side in a box, and
    /// how wide a character of a port's words is taken to be.
    /// </summary>
    public const double Inset = 16, Beside = 10, WordChar = 6.6;

    /// <summary>The id a port's box has in the flow: its name, marked so no part can share it.</summary>
    public static string PortId(string name) => "port:" + name;

    /// <summary>The flow of a composite.</summary>
    public static GraphSpec Of(CompositeSpec spec)
    {
        var links = (spec.Connectors ?? []).Select(link => new GraphEdge(link.From, link.To, link.Word)).ToList();

        // A part stands one past the furthest part that uses it, and one more for what is asked, which is first.
        var alone = new GraphSpec([.. spec.Parts.Select(part => new GraphNode(part.Id, part.Type))], links);
        var rank = Ranks.Of(alone);
        int last = rank.Values.Max() + 2;

        var ports = spec.Ports ?? [];
        var parts = spec.Parts.Select(part =>
            new GraphNode(part.Id, PartLabel(part), part.Note, rank[part.Id] + 1, Class, Tone: spec.Tone));
        var pins = ports.Select(port => new GraphNode(PortId(port.Name), PortLabel(port),
            Column: port.Side == PortSide.In ? 0 : last, Group: Class, Tone: ToneOf(spec, port)));

        // What is asked runs from its port to the part that does it, and what is needed from the part to its port.
        var passed = ports.Select(port => port.Side == PortSide.In
            ? new GraphEdge(PortId(port.Name), port.Part)
            : new GraphEdge(port.Part, PortId(port.Name)));
        return new GraphSpec([.. parts, .. pins], [.. links, .. passed], FlowDirection.Right,
            [new GraphGroup(Class, spec.Name)]);
    }

    /// <summary>A port's colour: its own, or the class's when it has none.</summary>
    public static DiagramTone ToneOf(CompositeSpec spec, CompositePort port) =>
        port.Tone == DiagramTone.Plain ? spec.Tone : port.Tone;

    /// <summary>
    /// A part's label: its type and its role, as wide as the type large and the role's chip beside it.
    /// </summary>
    private static string PartLabel(CompositePart part)
    {
        double chip = part.Role is { } role ? Beside + ComponentSizes.ChipWidth(role) : 0;
        return Measured($"{part.Type}   {part.Role}", 2 * Inset + ComponentSizes.NameWidth(part.Type) + chip);
    }

    /// <summary>A port's label: its words and its name, as wide as the words with the pin beside them.</summary>
    private static string PortLabel(CompositePort port)
    {
        double words = (port.Label?.Length ?? 0) * WordChar;
        return Measured($"{port.Label}   {port.Name}", words + Beside + ComponentSizes.PinWidth(port.Name) + Beside);
    }

    /// <summary>
    /// Words padded until the layout, which measures a box by its label's length, gives the box at least the
    /// width asked for.
    /// </summary>
    private static string Measured(string words, double width)
    {
        int length = (int)Math.Ceiling((width - 2 * GraphLayout.LabelPad) / GraphLayout.LabelCharWidth);
        return words.TrimEnd().PadRight(Math.Max(words.TrimEnd().Length, length));
    }
}
