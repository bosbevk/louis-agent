using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Puls.Routing;

namespace LouisAgent.Evals.Puls;

/// <summary>Puls's evals (F18 step 5): the set the runner's catalogue lists under <c>EVALS=puls</c>, the cook's and the routing's.</summary>
public sealed class PulsEvalSet : IEvalSet
{
    public string Identity => "puls";

    public IReadOnlyList<IEval> Evals { get; } = [new CookEval(), new RoutingEval()];
}
