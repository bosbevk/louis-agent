using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Sources.Types;

/// <summary>
/// What type of diagram a picture is, by UML's name for it (CL65.3, Louis: "our graph types must match", "the
/// type of diagram must always display"). A kind says how a picture is laid out, and this says what it shows: a
/// <c>flow</c> is boxes and arrows, and it may be an activity, a component or an object diagram. The one name
/// that is not UML's is <see cref="Overview"/>, for a project's overview in cards.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramType
{
    [WireName("class")] Class,
    [WireName("composite-structure")] CompositeStructure,
    [WireName("object")] Object,
    [WireName("component")] Component,
    [WireName("deployment")] Deployment,
    [WireName("package")] Package,
    [WireName("state-machine")] StateMachine,
    [WireName("activity")] Activity,
    [WireName("use-case")] UseCase,
    [WireName("sequence")] Sequence,
    [WireName("communication")] Communication,
    [WireName("interaction-overview")] InteractionOverview,
    [WireName("timing")] Timing,
    [WireName("overview")] Overview
}
