using LouisAgent.Diagrams.Components.Spec;

namespace LouisAgent.Diagrams.Components;

/// <summary>
/// How much room each part of a component diagram needs (F134.7): a box from its own words and pins and from the
/// boxes it holds, a pin and a pill from their words.
/// </summary>
/// <remarks>
/// The picture is drawn with no browser to measure text, so a width is counted from the number of letters, as the
/// other kinds count theirs. Every number of the layout is here, in one place, so the look can be tuned without
/// reading the placing code.
/// </remarks>
internal static class ComponentSizes
{
    /// <summary>The room inside a box's sides, and under the last box it holds.</summary>
    public const double Pad = 20;

    /// <summary>From a box's top edge to under its name.</summary>
    public const double Header = 44;

    /// <summary>One line of a box's note.</summary>
    public const double NoteLine = 18;

    /// <summary>Between two boxes one holder holds, with room for the pins on the lower one's edge.</summary>
    public const double Gap = 26;

    /// <summary>
    /// A pin's height, the gap between two pins, and how far the last pin stands from its box's side.
    /// </summary>
    public const double PinHeight = 18, PinGap = 8, PinInset = 24;

    /// <summary>A pill's height, and the room its row takes under the box's name.</summary>
    public const double PillHeight = 36, PillRow = 52;

    /// <summary>The room under a box that holds nothing.</summary>
    private const double Foot = 12;

    /// <summary>No box is narrower than this, however short its name.</summary>
    private const double Narrowest = 220;

    /// <summary>How wide one letter is taken to be: in a name, a chip, a note, a pin and a pill.</summary>
    private const double NameChar = 9.0, ChipChar = 6.1, NoteChar = 5.9, PinChar = 6.1, PillChar = 7.1;

    /// <summary>How many letters of a note fit a line.</summary>
    private const int NoteWrap = 64;

    /// <summary>A note, a line at a time. No lines for no note.</summary>
    public static IReadOnlyList<string> NoteLines(string? note) => note is null ? [] : TextWrap.Lines(note, NoteWrap);

    /// <summary>How wide a pin is, with its arrow.</summary>
    public static double PinWidth(string name) => name.Length * PinChar + 30;

    /// <summary>How wide a pill is, with its dot.</summary>
    public static double PillWidth(string says) => says.Length * PillChar + 56;

    /// <summary>How wide a chip is.</summary>
    public static double ChipWidth(string kind) => kind.Length * ChipChar + 16;

    /// <summary>How wide a name is.</summary>
    public static double NameWidth(string name) => name.Length * NameChar;

    /// <summary>A box's pins in the order they stand: what is handed down, then what is said back up.</summary>
    public static IReadOnlyList<string> Pins(ComponentBox box) => [.. box.In ?? [], .. box.Out ?? []];

    /// <summary>How wide a box's pins are together, with the gaps between them. Nothing for no pins.</summary>
    public static double PinsWidth(ComponentBox box)
    {
        var pins = Pins(box);
        return pins.Count == 0 ? 0 : pins.Sum(PinWidth) + PinGap * (pins.Count - 1);
    }

    /// <summary>
    /// How wide a box must be: for its name and chip, its note, its pins, its pill, and the widest box it holds
    /// with room at both sides.
    /// </summary>
    public static double Width(ComponentBox box)
    {
        double name = 22 + NameWidth(box.Name) + (box.Kind is { } kind ? 8 + ChipWidth(kind) : 0) + Pad;
        double note = NoteLines(box.Note).Select(line => 22 + line.Length * NoteChar + Pad).DefaultIfEmpty(0).Max();
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
