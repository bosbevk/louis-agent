namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>A word to the reader, on a panel over the lifelines it names: one, or a span of them.</summary>
public sealed record NoteStep(IReadOnlyList<string> Over, string Text) : SequenceStep
{
    /// <summary>The lifelines it stands over.</summary>
    public override IReadOnlyList<string> Names() => Over;
}
