using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Routing;

namespace LouisAgent.Evals.Puls;

/// <summary>Puls's evals (F18 step 5): the set the runner's catalogue lists under <c>EVALS=puls</c>, the cook's and the routing's.</summary>
public sealed class PulsEvalSet : IEvalSet
{
    public string Identity => PulsEvalIdentity.Puls.Wire();

    public IReadOnlyList<IEval> Evals { get; } = [new CookEval(), new RoutingEval()];
}

/// <summary>The identity this set proves (ADR 0043: each eval set keeps its own enum of the names it uses). The set owns it: an eval belongs to exactly one set, so the catalogue lists each eval under its set's identity.</summary>
public enum PulsEvalIdentity
{
    [WireName("puls")] Puls,
}
