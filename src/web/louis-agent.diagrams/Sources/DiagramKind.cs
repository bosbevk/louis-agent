using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// What a diagram draws. A flow is the site's boxes and arrows; cards are the overview pictures in Louis's figure;
/// a sequence is who calls whom in order; classes are an interface design; a ticket is a read-only card an agent
/// shows in a chat (F127.4.1).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramKind
{
    [WireName("flow")] Flow,
    [WireName("cards")] Cards,
    [WireName("sequence")] Sequence,
    [WireName("classes")] Classes,
    [WireName("ticket")] Ticket
}
