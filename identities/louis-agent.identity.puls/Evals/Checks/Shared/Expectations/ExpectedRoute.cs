using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Expectations;

/// <summary>
/// Where a scenario's first turn should go: the role Puls hands the question to, or nobody when Puls answers it itself.
/// Both Puls datasets say it in <c>routes_to</c>, and the route check reads it there, never from the scenario's name.
/// </summary>
public static class ExpectedRoute
{
    /// <summary>The expected key a scenario states its route under.</summary>
    public const string Key = "routes_to";

    /// <summary>
    /// The scenario's route. A scenario that states none, or one that is neither a role's name nor null, is refused.
    /// </summary>
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
