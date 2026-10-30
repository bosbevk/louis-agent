using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;

namespace LouisAgent.Evals.Puls;

/// <summary>
/// Puls's evals in C#: the set the runner's catalogue lists under <c>EVALS=puls</c>. The cook is the one left; the
/// routing eval is a folder of files beside the identity (F49.2 step 6), and the cook follows in step 8.
/// </summary>
public sealed class PulsEvalSet : IEvalSet
{
    /// <inheritdoc />
    public string Identity => PulsEvalIdentity.Puls.Wire();

    /// <inheritdoc />
    public IReadOnlyList<IEval> Evals { get; } = [new CookEval()];
}

/// <summary>
/// The identity this set proves (ADR 0043: each eval set keeps its own enum of the names it uses). The set owns it: an
/// eval belongs to exactly one set, so the catalogue lists each eval under its set's identity.
/// </summary>
public enum PulsEvalIdentity
{
    [WireName("puls")] Puls
}
