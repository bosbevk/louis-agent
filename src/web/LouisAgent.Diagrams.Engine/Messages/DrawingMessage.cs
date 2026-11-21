using System.ComponentModel;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Engine.Messages;

/// <summary>
/// The words a drawing writes (CL65.1): a key's heading and what each of its items means, what a tone means, a
/// picture's type, and the one refusal that is a drawing's own, a kind nothing draws yet. They were members of
/// <see cref="DiagramMessage"/> until what builds a diagram became a project of its own: the reader's refusals say
/// what a diagram is, and these say how one is drawn. <see cref="DiagramText"/> fills either.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum DrawingMessage
{
    [Description("There is no renderer for a {Kind} diagram yet.")]
    NoRenderer,

    // The heading over a picture's own key, in any kind that has one.
    [Description("How to read it")] KeyHeading,

    // What type of diagram a picture is, at the right of its title's line: UML's names, and our own overview.
    [Description("Class diagram")] TypeClass,
    [Description("Composite structure diagram")] TypeCompositeStructure,
    [Description("Object diagram")] TypeObject,
    [Description("Component diagram")] TypeComponent,
    [Description("Deployment diagram")] TypeDeployment,
    [Description("Package diagram")] TypePackage,
    [Description("State machine diagram")] TypeStateMachine,
    [Description("Activity diagram")] TypeActivity,
    [Description("Use case diagram")] TypeUseCase,
    [Description("Sequence diagram")] TypeSequence,
    [Description("Communication diagram")] TypeCommunication,
    [Description("Interaction overview diagram")] TypeInteractionOverview,
    [Description("Timing diagram")] TypeTiming,
    [Description("Overview")] TypeOverview,

    // A composite's key.
    [Description("a port")] CompositePort,
    [Description("asked of the class")] CompositeAsked,
    [Description("what the class needs")] CompositeNeeded,
    [Description("two parts work together")] CompositeTogether,
    [Description("passed on")] CompositePassedOn,

    // The words a flow's key says of a shape and of a line (F134.8).
    [Description("a decision")] FlowDecision,
    [Description("data in or out")] FlowData,
    [Description("a start or an end")] FlowTerminal,
    [Description("a line has the colour of the box it leaves")] FlowLine,

    // The words a sequence's key says of a call, an answer, a note and each kind of block (F134.10).
    [Description("a call")] SequenceCall,
    [Description("an answer")] SequenceAnswer,
    [Description("a note")] SequenceNote,
    [Description("repeats")] SequenceLoop,
    [Description("only if")] SequenceOpt,
    [Description("one case or the other")] SequenceAlt,

    // The words a class chart's key says of a badge's letter and of each kind of link (F134.11).
    [Description("an interface")] ClassInterface,
    [Description("a record")] ClassRecord,
    [Description("an enum")] ClassEnum,
    [Description("abstract")] ClassAbstract,
    [Description("static")] ClassStatic,
    [Description("a class")] ClassPlain,
    [Description("holds")] LinkHolds,
    [Description("is a")] LinkInherits,
    [Description("implements")] LinkImplements,
    [Description("creates")] LinkCreates,
    [Description("uses")] LinkUses,

    // The words a nested picture draws itself (F134.7): on a line to a service, and in its key.
    [Description("reads")] NestedReads,
    [Description("handed down")] NestedHandedDown,
    [Description("a parameter")] NestedParameter,
    [Description("said back up")] NestedSaidBackUp,
    [Description("a callback")] NestedCallback,
    [Description("reads a service")] NestedReadsAService,
    [Description("the one thing it holds")] NestedHolds,
    [Description("A box inside a box is held by it.")] NestedHeldBy,

    // What each tone means in a picture's key (CL60.2): the role a box of that colour plays in every chart.
    [Description("starts it")] LegendBlue,
    [Description("this project")] LegendGreen,
    [Description("the framework")] LegendViolet,
    [Description("contracts and state")] LegendTeal,
    [Description("outside the process")] LegendRed,
    [Description("files and data")] LegendAmber
}
