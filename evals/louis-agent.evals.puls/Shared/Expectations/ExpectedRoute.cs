using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Puls.Messages;

namespace LouisAgent.Evals.Puls.Shared.Expectations;

/// <summary>
/// Where a scenario's first turn should go (F18 step 5.4): its <c>routes_to</c>, the role Puls hands the question to, or null when Puls answers
/// it itself. Both Puls datasets state it, and the route grader reads it; it is not a rule on the scenario's name.
/// </summary>
public static class ExpectedRoute
{
    public const string Key = "routes_to";

    /// <summary>The scenario's route; a scenario with none stated, or one that is neither a role's name nor null, is refused.</summary>
    public static string? Of(EvalScenario scenario)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object ||
            !scenario.Expected.TryGetProperty(Key, out var route))
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.NoRoute, new { scenario.Id }));
        return route.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String when !string.IsNullOrWhiteSpace(route.GetString()) => route.GetString(),
            _ => throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadRoute, new { scenario.Id })),
        };
    }
}
