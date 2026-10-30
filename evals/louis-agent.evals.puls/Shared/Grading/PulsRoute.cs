using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Puls.Shared.Expectations;

namespace LouisAgent.Evals.Puls.Shared.Grading;

/// <summary>
/// Puls's route (F18 step 5.4): the harness's <see cref="RoutedToCheck"/> reading the scenario's <c>routes_to</c>. A mode that can hand a
/// question on is expected to send it there; one that cannot (it lacks the specialists toolset) is expected to answer it itself, which is all it can
/// do. This replaces the cook's and the routing's own route graders, whose rules it holds.
/// </summary>
public static class PulsRoute
{
    public static RoutedToCheck Grader() => new(ExpectedRole, expectedKeys: new HashSet<string> { ExpectedRoute.Key });

    private static string? ExpectedRole(Trial trial) =>
        PulsModes.CanDelegate(trial.Mode) ? ExpectedRoute.Of(trial.Scenario) : null;
}
