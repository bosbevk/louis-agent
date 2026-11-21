namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>One participant calls another: a solid arrow with its words above it.</summary>
public sealed record MessageStep(string From, string To, string Text) : SequenceStep
{
    /// <summary>The caller and the one called.</summary>
    public override IReadOnlyList<string> Names() => [From, To];
}
