using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Nested;
using LouisAgent.Diagrams.Nested.Spec;

namespace LouisAgent.Diagrams.Nested;

/// <summary>
/// Refuses a nested picture that could not be drawn as meant (F134.7), saying what to change: an id used twice,
/// a state that names a pin its box can't reach, a neighbour beside a box that isn't there.
/// </summary>
/// <remarks>
/// The reader's checks for this kind, in a class of their own so the reader stays a list of kinds. Every check is a
/// guard clause: the first thing wrong is refused, by name, before anything is placed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals</seealso>
internal static class NestedCheck
{
    /// <summary>The words that name a pin handed down, and one said back up, as the JSON's lists are called.</summary>
    private const string Down = "in", Up = "out";

    /// <summary>Checks the whole picture, and throws at the first thing wrong.</summary>
    public static void Check(NestedSpec components)
    {
        var boxes = components.Root.Within().ToList();
        var around = components.Around ?? [];

        // An id names one thing, among the boxes and the neighbours together.
        string? twice = boxes.Select(box => box.Id).Concat(around.Select(neighbour => neighbour.Id))
            .GroupBy(id => id, StringComparer.Ordinal).FirstOrDefault(ids => ids.Count() > 1)?.Key;
        if (twice is not null)
        {
            var duplicate = new DuplicateNestedIdHoles(twice);
            throw Refused(DiagramMessage.DuplicateNestedId, duplicate);
        }

        foreach (var holder in boxes.Where(box => box.State is not null)) CheckState(holder);

        var ids = boxes.Select(box => box.Id).ToHashSet(StringComparer.Ordinal);
        var astray = around.FirstOrDefault(neighbour => neighbour.To is { } to && !ids.Contains(to));
        if (astray is null) return;

        var noBox = new UnknownNestedBoxHoles(astray.Name, astray.To!);
        throw Refused(DiagramMessage.UnknownNestedBox, noBox);
    }

    /// <summary>
    /// A state's pins are each on a box its holder holds: one handed down in that box's "in", one said back up in
    /// its "out".
    /// </summary>
    private static void CheckState(NestedBox holder)
    {
        var held = holder.Within().Skip(1).ToList();
        var state = holder.State!;

        string? down = (state.Down ?? []).FirstOrDefault(pin => !IsPinOf(held, pin, box => box.In));
        if (down is not null) throw NoPin(holder, down, Down);

        string? up = (state.Up ?? []).FirstOrDefault(pin => !IsPinOf(held, pin, box => box.Out));
        if (up is not null) throw NoPin(holder, up, Up);
    }

    /// <summary>The refusal of a pin a state names and can't reach, with the list the pin should be in.</summary>
    private static InvalidDataException NoPin(NestedBox holder, string pin, string list)
    {
        var noPin = new UnknownPinHoles(holder.Id, pin, list);
        return Refused(DiagramMessage.UnknownPin, noPin);
    }

    /// <summary>
    /// Whether a name written <c>box.Pin</c> is a pin of one of these boxes, in the list of pins that is asked for.
    /// </summary>
    private static bool IsPinOf(List<NestedBox> boxes, string name,
        Func<NestedBox, IReadOnlyList<string>?> pins)
    {
        int dot = name.IndexOf('.');
        if (dot < 0) return false;

        var box = boxes.FirstOrDefault(candidate => candidate.Id == name[..dot]);
        return box is not null && (pins(box) ?? []).Contains(name[(dot + 1)..]);
    }

    /// <summary>The refusal, its text from the message with its holes filled.</summary>
    private static InvalidDataException Refused(DiagramMessage message, object holes) =>
        new(DiagramText.Format(message, holes));
}
