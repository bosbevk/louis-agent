using LouisAgent.Choices;

namespace LouisAgent.Evals.Puls;

/// <summary>
/// Puls's evals in C#: none are left. The routing eval and the cook are folders of files beside the identity (F49.2
/// steps 6 and 8b); the set stays one more step, empty, so <c>EVALS=puls</c> still reads, and goes with the runner's
/// sets in step 8c.
/// </summary>
public sealed class PulsEvalSet : IEvalSet
{
    /// <inheritdoc />
    public string Identity => PulsEvalIdentity.Puls.Wire();

    /// <inheritdoc />
    public IReadOnlyList<IEval> Evals { get; } = [];
}

/// <summary>
/// The identity this set proves (ADR 0043: each eval set keeps its own enum of the names it uses). The set owns it: an
/// eval belongs to exactly one set, so the catalogue lists each eval under its set's identity.
/// </summary>
public enum PulsEvalIdentity
{
    [WireName("puls")] Puls
}
