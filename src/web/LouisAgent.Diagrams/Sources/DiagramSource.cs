using System.Text.Json.Serialization;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Nested.Spec;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sources.Types;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// One diagram as its JSON file says it: what kind it is, its title and caption, and the part its kind draws (the
/// boxes and arrows of a flow, the rows of cards, a sequence's participants and steps, a class chart's types, a
/// ticket's band and rows, a nested picture's root and what stands around it, or a composite's class and its
/// parts).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record DiagramSource(
    // Written always, though flow is the first kind: the reader reads the kind first.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never), JsonPropertyOrder(-2)]
    DiagramKind Kind,
    string Title,
    string? Caption = null,
    GraphSpec? Flow = null,
    CardsSpec? Cards = null,
    SequenceSpec? Sequence = null,
    ClassesSpec? Classes = null,
    TicketSpec? Ticket = null,
    NestedSpec? Nested = null,
    CompositeSpec? Composite = null,
    // What type of diagram it is, by UML's name. A kind that is one type need not write it. It is written
    // straight after the kind, where a person would write it, though it is the last here.
    [property: JsonPropertyOrder(-1)]
    DiagramType? Type = null);
