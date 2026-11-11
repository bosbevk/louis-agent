namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>
/// A frame round steps of its own: a loop that repeats them, an opt that may run them, an alt whose other cases
/// follow in <c>Else</c>, each with its own steps.
/// </summary>
public sealed record BlockStep(
    BlockKind Block,
    string Label,
    IReadOnlyList<SequenceStep> Steps,
    IReadOnlyList<ElseCase>? Else = null) : SequenceStep
{
    /// <summary>Everyone the steps inside it name, in every case.</summary>
    public override IReadOnlyList<string> Names() => Inside().SelectMany(step => step.Names()).ToList();

    /// <summary>The block, then every step inside it, each case's after the first's.</summary>
    public override IEnumerable<SequenceStep> All() => [this, .. Inside().SelectMany(step => step.All())];

    /// <summary>The steps of every case, in order.</summary>
    private IEnumerable<SequenceStep> Inside() => Steps.Concat((Else ?? []).SelectMany(@case => @case.Steps));
}
