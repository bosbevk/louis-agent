using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// What the cook's dataset expects of a scenario, read from the real files, so the scorer tests and the data cannot
/// drift apart.
/// </summary>
internal static class Dev
{
    /// <summary>The cook's eval, whose scenarios are read.</summary>
    private static readonly CookEval CookEval = new();

    /// <summary>The cook scenario's expectations, by its id.</summary>
    public static CookExpectations Cook(string id) => CookExpectations.Of(CookEval.Scenarios.Single(s => s.Id == id));
}
