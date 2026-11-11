namespace LouisAgent.Diagrams.Sequence.Spec;

/// <summary>
/// A sequence chart: who takes part, and what passes between them, in order. <c>Numbered</c> puts a number before
/// each message, as Mermaid's <c>autonumber</c> does.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
public sealed record SequenceSpec(
    IReadOnlyList<Participant> Participants,
    IReadOnlyList<SequenceStep> Steps,
    bool Numbered = false);
