
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Shared.Expectations;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>What the datasets expect of a scenario (F18 step 5.4), read from the real files, so the scorer tests and the data cannot drift apart.</summary>
internal static class Dev
{
    private static readonly CookEval CookEval = new();
    private static readonly RoutingEval RoutingEval = new();

    public static CookExpectations Cook(string id) => CookExpectations.Of(CookEval.Scenarios.Single(s => s.Id == id));

    public static string? Route(string id) => ExpectedRoute.Of(RoutingEval.Scenarios.Single(s => s.Id == id));
}
