using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// What the cook's dataset expects of a scenario, read from the real files, so the scorer tests and the data cannot
/// drift apart.
/// </summary>
internal static class Dev
{
    /// <summary>The cook scenario's expectations, by its id.</summary>
    public static CookExpectations Cook(string id) =>
        CookExpectations.Of(CookFile.Eval.Scenarios.Single(s => s.Id == id));
}
