using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Types;

namespace LouisAgent.Diagrams.Sources.Types;

/// <summary>
/// Holds a picture's type to its kind (CL65.3): a file that wrote none gets its kind's own, a flow that wrote
/// none is refused, and so is a type the kind can't be, each with what the kind may be.
/// </summary>
/// <remarks>
/// A default where there is one answer, and a refusal where there are several. It stands apart from the reader as
/// the nested picture's and the composite's checks do, so the reader keeps the order of the checks and each rule has
/// its own file.
/// </remarks>
public static class DiagramTypeCheck
{
    /// <summary>The type a picture of a kind has, from the one its file wrote.</summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public static DiagramType? Of(DiagramKind kind, DiagramType? written)
    {
        var allowed = DiagramTypes.MayBe(kind);
        if (written is not { } type) return Unsaid(kind, allowed);
        if (allowed.Contains(type)) return type;

        // A kind with no type at all is told to take the field out, where a list of none would say nothing.
        if (allowed.Count == 0)
        {
            var none = new NoDiagramTypeHoles(kind.Wire());
            throw new InvalidDataException(DiagramText.Format(DiagramMessage.NoDiagramType, none));
        }

        var wrong = new WrongDiagramTypeHoles(kind.Wire(), type.Wire(), Names(allowed));
        throw new InvalidDataException(DiagramText.Format(DiagramMessage.WrongDiagramType, wrong));
    }

    /// <summary>
    /// The type of a picture that said none: its kind's own, or none for a kind that has none. A kind that may
    /// be several, which is a flow, must say, so it is refused with what it may be.
    /// </summary>
    private static DiagramType? Unsaid(DiagramKind kind, IReadOnlyList<DiagramType> allowed)
    {
        if (DiagramTypes.Own(kind) is { } own) return own;
        if (allowed.Count == 0) return null;

        var missing = new MissingDiagramTypeHoles(kind.Wire(), Names(allowed));
        throw new InvalidDataException(DiagramText.Format(DiagramMessage.MissingDiagramType, missing));
    }

    /// <summary>The types by their names, as a refusal lists them.</summary>
    private static string Names(IReadOnlyList<DiagramType> types) =>
        string.Join(", ", types.Select(each => each.Wire()));
}
