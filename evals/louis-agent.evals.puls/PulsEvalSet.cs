namespace LouisAgent.Evals.Puls;

/// <summary>
/// Puls's evals (F18 step 4): the set the runner's catalogue lists under <c>EVALS=puls</c>. It is empty until step 5 adds the
/// cook's and the routing's evals; until then the set adds nothing to the catalogue.
/// </summary>
public sealed class PulsEvalSet : IEvalSet
{
    public string Identity => "puls";

    public IReadOnlyList<IEval> Evals { get; } = [];
}
