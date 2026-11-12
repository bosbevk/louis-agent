using System.Text.Json.Serialization;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// One diagram as its JSON file says it: what kind it is, its title and caption, and the part its kind draws (the
/// boxes and arrows of a flow, the rows of cards, a sequence's participants and steps, a class chart's types, or a
/// ticket's band and rows).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record DiagramSource(
    // Written always, though flow is the first kind: the reader reads the kind first.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    DiagramKind Kind,
    string Title,
    string? Caption = null,
    GraphSpec? Flow = null,
    CardsSpec? Cards = null,
    SequenceSpec? Sequence = null,
    ClassesSpec? Classes = null,
    TicketSpec? Ticket = null);
