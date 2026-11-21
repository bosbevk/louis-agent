namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>The answer coming back: a dashed arrow, in the tone of whoever answers.</summary>
public sealed record ReplyStep(string From, string To, string Text) : SequenceStep
{
    /// <summary>The one answering and the one answered.</summary>
    public override IReadOnlyList<string> Names() => [From, To];
}
