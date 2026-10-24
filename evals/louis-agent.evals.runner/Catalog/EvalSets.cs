using LouisAgent.Evals.Puls;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>The set each kind names: the one place the runner builds a set (ADR 0027).</summary>
public static class EvalSets
{
    public static IEvalSet Create(EvalSetKind kind) => kind switch
    {
        EvalSetKind.Puls => new PulsEvalSet(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
