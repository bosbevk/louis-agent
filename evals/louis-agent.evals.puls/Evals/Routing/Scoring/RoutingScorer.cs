namespace LouisAgent.Evals.Puls.Evals.Routing.Scoring;

/// <summary>
/// The routing check's baseline: Puls with everything this instance has, except the toolsets it could hand a question
/// on with.
/// </summary>
public static class RoutingScorer
{
    /// <summary>One agent with every toolset this instance has, but none it could delegate with.</summary>
    public static IReadOnlyList<string> Baseline(IEnumerable<string> available) =>
        available.Where(t => !RoutingSetup.DelegatingToolsets.Contains(t)).ToList();
}
