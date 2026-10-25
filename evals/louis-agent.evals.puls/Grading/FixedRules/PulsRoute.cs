using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Expectations;
using LouisAgent.Evals.Puls.Routing;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>
/// Puls's route (F18 step 5.4): the harness's <see cref="RoutedToGrader"/> reading the scenario's <c>routes_to</c>. A mode that can hand a
/// question on is expected to send it there; one that cannot (it lacks the specialists toolset) is expected to answer it itself, which is all it can
/// do. This replaces the cook's and the routing's own route graders, whose rules it holds.
/// </summary>
public static class PulsRoute
{
    public static RoutedToGrader Grader() => new(ExpectedRole, expectedKeys: new HashSet<string> { ExpectedRoute.Key });

    private static string? ExpectedRole(Trial trial) => RoutingScorer.Delegating(trial.Mode) ? ExpectedRoute.Of(trial.Scenario) : null;
}
