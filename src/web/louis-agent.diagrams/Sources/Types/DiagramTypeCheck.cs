using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Types;

namespace LouisAgent.Diagrams.Sources.Types;

/// <summary>
/// Holds a picture's type to its kind (CL65.3): a file that wrote none gets its kind's own, and one that wrote a
/// type its kind can't be is refused with what the kind may be.
/// </summary>
/// <remarks>
/// A default where there is one answer, and a refusal where there are several. It stands apart from the reader as
/// the components' and the composite's checks do, so the reader keeps the order of the checks and each rule has
/// its own file.
/// </remarks>
public static class DiagramTypeCheck
{
    /// <summary>The type a picture of a kind has, from the one its file wrote.</summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public static DiagramType? Of(DiagramKind kind, DiagramType? written)
    {
        if (written is not { } type) return DiagramTypes.Own(kind);

        var allowed = DiagramTypes.MayBe(kind);
        if (allowed.Contains(type)) return type;

        // A kind with no type at all is told to take the field out, where a list of none would say nothing.
        if (allowed.Count == 0)
        {
            var none = new NoDiagramTypeHoles(kind.Wire());
            throw new InvalidDataException(DiagramText.Format(DiagramMessage.NoDiagramType, none));
        }

        var wrong = new WrongDiagramTypeHoles(kind.Wire(), type.Wire(),
            string.Join(", ", allowed.Select(each => each.Wire())));
        throw new InvalidDataException(DiagramText.Format(DiagramMessage.WrongDiagramType, wrong));
    }
}
