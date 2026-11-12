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

    [Description("There is no renderer for a {Kind} diagram yet.")]
    NoRenderer,

    // What each tone means in the docs' legend (CL60.2): the role a box of that colour plays in every chart.
    [Description("starts it")] LegendBlue,
    [Description("this project")] LegendGreen,
    [Description("the framework")] LegendViolet,
    [Description("contracts and state")] LegendTeal,
    [Description("outside the process")] LegendRed,
    [Description("files and data")] LegendAmber
}
