using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Composites;

namespace LouisAgent.Diagrams.Composite;

/// <summary>
/// Checks a composite structure diagram before it is drawn (F134.13): it has a part, no two parts share an id, and
/// every port and connector names a part the class has.
/// </summary>
/// <remarks>
/// Guard clauses, one refusal a rule, each with what to change (ADR 0051). The reader runs it on every composite,
/// so a drawing never meets a port with no part to pass to.
/// </remarks>
internal static class CompositeCheck
{
    /// <summary>Refuses a composite that could not be drawn as meant, saying what to change.</summary>
    public static void Check(CompositeSpec composite)
    {
        if (composite.Parts.Count == 0)
        {
            var empty = new CompositeNoPartsHoles(composite.Name);
            throw Refused(DiagramMessage.CompositeNoParts, empty);
        }

        // An id names one part.
        string? twice = composite.Parts.GroupBy(part => part.Id, StringComparer.Ordinal)
            .FirstOrDefault(ids => ids.Count() > 1)?.Key;
        if (twice is not null)
        {
            var duplicate = new DuplicatePartHoles(twice);
            throw Refused(DiagramMessage.DuplicatePart, duplicate);
        }

        // A port and a connector name parts the class has. A connector is named by its end that is a part.
        var ids = composite.Parts.Select(part => part.Id).ToHashSet(StringComparer.Ordinal);
        var astray = (composite.Ports ?? []).Where(port => !ids.Contains(port.Part))
            .Select(port => new UnknownPartHoles(port.Name, port.Part))
            .Concat((composite.Connectors ?? []).Where(link => !ids.Contains(link.From) || !ids.Contains(link.To))
                .Select(link => ids.Contains(link.From)
                    ? new UnknownPartHoles(link.From, link.To)
                    : new UnknownPartHoles(link.To, link.From)))
            .FirstOrDefault();
        if (astray is not null) throw Refused(DiagramMessage.UnknownPart, astray);
    }

    /// <summary>The refusal, with its message filled.</summary>
    private static InvalidDataException Refused(DiagramMessage message, object holes) =>
        new(DiagramText.Format(message, holes));
}
