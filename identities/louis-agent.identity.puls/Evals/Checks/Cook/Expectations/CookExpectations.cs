using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Expectations;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;

/// <summary>
/// What a cook scenario asks for, read from its <c>expected</c>: where the first turn goes, the user's town, how many
/// people each turn serves, and the rand budget of the first turn's basket.
/// </summary>
/// <remarks>
/// Each key is read by a named check. A key that's missing isn't asked for; one of the wrong kind is refused, naming
/// the scenario, so a typo in the dataset fails loudly instead of grading nothing. The shop the user chose is the
/// shops pack's to read (its <c>ScenarioShop</c>, F49.3, Louis's pick): no rule of Puls's reads it now.
/// </remarks>
public sealed record CookExpectations(
    string? RoutesTo,
    string? Town,
    IReadOnlyList<int> Serves,
    decimal? Budget)
{
    /// <summary>Reads every key of a scenario at once.</summary>
    public static CookExpectations Of(EvalScenario scenario) =>
        new(ExpectedRoute.Of(scenario), TownOf(scenario), ServesOf(scenario), BudgetOf(scenario));

    /// <summary>
    /// The user's town, which the cook's judge is told; none when the scenario names none. A scenario whose shop needs
    /// a town is the shops pack's to refuse.
    /// </summary>
    public static string? TownOf(EvalScenario scenario)
    {
        if (!TryGet(scenario, "town", out var town)) return null;
        if (town.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(town.GetString()))
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadTown, new { scenario.Id }));
        return town.GetString()!.Trim();
    }

    /// <summary>
    /// The people each turn serves: one number for the first turn, or one per turn; none when the scenario asks for no
    /// number.
    /// </summary>
    public static IReadOnlyList<int> ServesOf(EvalScenario scenario)
    {
        string id = scenario.Id;
        if (!TryGet(scenario, "serves", out var serves)) return [];
        var counts = serves.ValueKind switch
        {
            JsonValueKind.Number => [serves],
            JsonValueKind.Array => serves.EnumerateArray().ToList(),
            _ => [],
        };
        if (counts.Count == 0 ||
            counts.Any(c => c.ValueKind != JsonValueKind.Number || !c.TryGetInt32(out int n) || n < 1))
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadServes, new { Id = id }));
        return counts.Select(c => c.GetInt32()).ToList();
    }

    /// <summary>The rand budget of the first turn's basket; null when the scenario sets none.</summary>
    public static decimal? BudgetOf(EvalScenario scenario)
    {
        string id = scenario.Id;
        if (!TryGet(scenario, "budget", out var budget)) return null;
        if (budget.ValueKind != JsonValueKind.Number || !budget.TryGetDecimal(out decimal rand) || rand <= 0)
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadBudget, new { Id = id }));
        return rand;
    }

    /// <summary>Finds a key in the scenario's <c>expected</c>, if it has one.</summary>
    private static bool TryGet(EvalScenario scenario, string key, out JsonElement value)
    {
        value = default;
        return scenario.Expected.ValueKind == JsonValueKind.Object && scenario.Expected.TryGetProperty(key, out value);
    }
}
