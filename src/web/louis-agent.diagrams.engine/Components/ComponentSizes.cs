using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Engine.Components;

/// <summary>
/// How much room each part of a component diagram needs (F134.7): a box from its own words and pins and from the
/// boxes it holds, a pin and a pill from their words.
/// </summary>
/// <remarks>
/// The picture is drawn with no browser to measure text, so a width is asked of the face its words are in, as the
/// other kinds ask theirs. Every number of the layout is here, in one place, so the look can be tuned without
/// reading the placing code.
/// </remarks>
internal static class ComponentSizes
{
    /// <summary>
    /// The room inside a box's sides, and under the last box it holds. At the left it holds the box's marker and the
    /// two lanes a state's lines run in.
    /// </summary>
    public const double Pad = 24;

    /// <summary>From a box's top edge to under its name.</summary>
    public const double Header = 44;

    /// <summary>One line of a box's note.</summary>
    public const double NoteLine = 18;

    /// <summary>Between two boxes one holder holds, with room for the pins on the lower one's edge.</summary>
    public const double Gap = 26;

    /// <summary>The gap between two pins, and how far the last pin stands from its box's side.</summary>
    public const double PinGap = 8, PinInset = 24;

    /// <summary>A pill's height, and the room its row takes under the box's name.</summary>
    public const double PillHeight = 36, PillRow = 52;

    /// <summary>
    /// How far in from a box's left its name and its note start, the room between its name and its chip, how far
    /// above the name's line a chip's top is, and how far in from a box's right the words at its right end.
    /// </summary>
    public const double NameIn = 22, ChipGap = 8, ChipRise = 13, AsideIn = 16;

    /// <summary>A pin's height.</summary>
    private const double PinHeight = 18;

    /// <summary>The room under a box that holds nothing.</summary>
    private const double Foot = 12;

    /// <summary>No box is narrower than this, however short its name.</summary>
    private const double Narrowest = 220;

    /// <summary>
    /// The room a pin has beside its name, for its arrow and its round ends, a pill for its dot and its ends, a
    /// chip at both ends of its words, and the room between a box's name and the words at its right.
    /// </summary>
    private const double PinRoom = 30, PillRoom = 56, ChipRoom = 16, AsideGap = 24;

    /// <summary>How many letters of a note fit a line.</summary>
    private const int NoteWrap = 64;

    /// <summary>The words of a box's name, a chip, a note, a pin and a pill.</summary>
    private static readonly Face NameFace = new(9.0), ChipFace = new(6.1), NoteFace = new(5.9), PinFace = new(6.1),
        PillFace = new(7.1);

    /// <summary>A note, a line at a time. No lines for no note.</summary>
    public static IReadOnlyList<string> NoteLines(string? note) => note is null ? [] : TextWrap.Lines(note, NoteWrap);

    /// <summary>How wide a pin is, with its arrow.</summary>
    public static double PinWidth(string name) => PinFace.Width(name) + PinRoom;

    /// <summary>How wide a pill is, with its dot.</summary>
    public static double PillWidth(string says) => PillFace.Width(says) + PillRoom;

    /// <summary>How wide a chip is.</summary>
    public static double ChipWidth(string kind) => ChipFace.Width(kind) + ChipRoom;

    /// <summary>How wide a few words in a note's face are.</summary>
    public static double WordsWidth(string words) => NoteFace.Width(words);

    /// <summary>How wide a name is.</summary>
    public static double NameWidth(string name) => NameFace.Width(name);

    /// <summary>The room a chip takes after a name, with the gap before it. Nothing for no chip.</summary>
    public static double ChipAfterName(string? kind) => kind is null ? 0 : ChipGap + ChipWidth(kind);

    /// <summary>
    /// A pin by its name, from its left, its middle on the edge it stands on: as wide as its name needs.
    /// </summary>
    public static PlacedPin Pin(IGeometry geometry, string on, string name, bool up, double left, double edge)
    {
        var box = geometry.Box(left, 0, PinWidth(name), PinHeight).OnLine(edge);
        return new PlacedPin(on, name, up, box);
    }

    /// <summary>A box's pins in the order they stand: what is handed down, then what is said back up.</summary>
    public static IReadOnlyList<string> Pins(ComponentBox box) => [.. box.In ?? [], .. box.Out ?? []];

    /// <summary>How wide a box's pins are together, with the gaps between them. Nothing for no pins.</summary>
    public static double PinsWidth(ComponentBox box)
    {
        var pins = Pins(box);
        return pins.Count == 0 ? 0 : pins.Sum(PinWidth) + PinGap * (pins.Count - 1);
    }

    /// <summary>
    /// How wide a box must be: for its name, its chip and the words at its right, its note, its pins, its pill,
    /// and the widest box it holds with room at both sides.
    /// </summary>
    public static double Width(ComponentBox box)
    {
        double aside = box.Aside is { } words ? AsideGap + WordsWidth(words) : 0;
        double name = NameIn + NameWidth(box.Name) + ChipAfterName(box.Kind) + aside + Pad;
        double note = NoteLines(box.Note).Select(line => NameIn + WordsWidth(line) + Pad).DefaultIfEmpty(0).Max();
        double pins = PinsWidth(box) + 2 * PinInset;
        double pill = box.State is { } state ? PillWidth(state.Says) + 2 * Pad : 0;
        double held = (box.Holds ?? []).Select(inner => Width(inner) + 2 * Pad).DefaultIfEmpty(0).Max();

        return new[] { Narrowest, name, note, pins, pill, held }.Max();
    }

    /// <summary>
    /// How tall a box is: its name, its note, its pill, then each box it holds under a gap, and room at its foot.
    /// </summary>
    public static double Height(ComponentBox box)
    {
        var held = box.Holds ?? [];
        double own = Header + NoteLines(box.Note).Count * NoteLine + (box.State is null ? 0 : PillRow);

        return own + held.Sum(inner => Gap + Height(inner)) + (held.Count == 0 ? Foot : Pad);
    }
}
