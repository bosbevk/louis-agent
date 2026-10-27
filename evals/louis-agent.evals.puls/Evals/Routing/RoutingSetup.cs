using LouisAgent.Evals.Puls.Shared;
namespace LouisAgent.Evals.Puls.Evals.Routing;

/// <summary>
/// The routing check's setup (F5, F21, F29): the toolsets that decide the modes (which role a question goes to is the scenario's `routes_to`). The twelve
/// questions are the <c>puls-routing</c> dataset.
/// </summary>
public static class RoutingSetup
{
    /// <summary>Puls as built (F29): the delegated mode's toolsets.</summary>
    public static readonly IReadOnlyList<string> Delegated = [PulsToolset.Specialists.Name(), PulsToolset.Web.Name()];

    /// <summary>The toolsets that let an agent hand a question on.</summary>
    public static readonly IReadOnlyList<string> DelegatingToolsets = [PulsToolset.Specialists.Name(), PulsToolset.Minions.Name()];
}
