using System.ComponentModel;

namespace LouisAgent.Diagrams.Messages;

/// <summary>
/// This library's own texts (ADR 0009: a text is a member of a message enum, its <c>[Description]</c> a template with
/// named holes). Its own, since it references no part of the engine and so can't use core's <c>MessageText</c>
/// (Louis's pick, CL60.1 step 2); <see cref="DiagramText"/> fills the holes.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum DiagramMessage
{
    [Description("The diagram's file can't be read: {Reason}")]
    Unreadable,

    [Description("'{Value}' is not a kind of diagram. The kinds are {Kinds}.")]
    UnknownKind,

    [Description("'{Value}' is not a shape a box can have. The shapes are {Shapes}.")]
    UnknownShape,

    [Description("'{Value}' is not a type of diagram. The types are {Types}.")]
    UnknownDiagramType,

    [Description("A {Kind} picture can't be a '{Type}' diagram. Its \"type\" may be: {Types}.")]
    WrongDiagramType,

    [Description("A {Kind} is not a diagram of any type. Take its \"type\" out.")]
    NoDiagramType,

    [Description("A {Kind} picture must say what type of diagram it is. Its \"type\" may be: {Types}.")]
    MissingDiagramType,

    [Description("A {Kind} diagram needs its \"{Kind}\" part.")]
    MissingPart,

    [Description("A diagram needs its \"title\": the words a reader of the picture is told it shows.")]
    MissingTitle,

    [Description("The arrow from '{From}' to '{To}' names '{Id}', which no box in the diagram has.")]
    UnknownBox,

    [Description("The box '{Id}' stands in the group '{Group}', which the diagram doesn't declare in its \"groups\".")]
    UnknownGroup,

    [Description("The link from '{From}' to '{To}' names '{Id}', which no card in the diagram has.")]
    UnknownCard,

    [Description("The link from '{From}' to '{To}' doesn't join two neighbouring rows. A link runs from a card to " +
                 "one in the row just above or just under it.")]
    LinkNotBetweenRows,

    [Description("A step names '{Id}', which no participant in the diagram has: \"{Step}\".")]
    UnknownParticipant,

    [Description("A note stands over the lifelines it names, so it names one at least: \"{Text}\".")]
    NoteOverNoOne,

    [Description("The {Kind} \"{Label}\" holds no steps, in it or in one of its cases.")]
    EmptyBlock,

    [Description("A sequence needs at least one step: a message, a reply, a note or a block.")]
    NoSteps,

    [Description("The relation from '{From}' to '{To}' names '{Id}', which no type in the diagram has.")]
    UnknownType,

    [Description("A ticket needs its \"title\": the headline on its band.")]
    TicketWithoutTitle,

    [Description("A ticket shows one to six rows; this one has {Count}.")]
    TicketRowCount,

    [Description("Row {Number} of the ticket needs both its \"label\" and its \"value\".")]
    TicketHalfRow,

    [Description("Row {Number}'s icon '{Icon}' isn't one the library draws; use one of {Icons}.")]
    UnknownIcon,

    [Description("The status's tone '{Tone}' isn't one there is; use one of {Tones}.")]
    UnknownTone,

    [Description("The id '{Id}' is used twice in the nested diagram. Every box and every neighbour needs an id " +
                 "of its own.")]
    DuplicateNestedId,

    [Description("The state of '{Holder}' names '{Pin}', which is no pin it can reach. Write it as box.Pin, where " +
                 "the box is one that '{Holder}' holds and the pin is in that box's \"{List}\".")]
    UnknownPin,

    [Description("The neighbour '{Neighbour}' stands by '{To}', which no box in the nested diagram has.")]
    UnknownNestedBox,

    [Description("The class '{Name}' has no part. A composite structure diagram shows what a class is built " +
                 "from, so give it one part at least.")]
    CompositeNoParts,

    [Description("The id '{Id}' is used twice in the composite structure diagram. Every part needs an id of its " +
                 "own.")]
    DuplicatePart,

    [Description("'{By}' names the part '{Part}', which the class doesn't have. Name a part by its \"id\".")]
    UnknownPart
}
