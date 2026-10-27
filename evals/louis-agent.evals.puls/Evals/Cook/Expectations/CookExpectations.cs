using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Shared.Expectations;
using LouisAgent.Choices;

namespace LouisAgent.Evals.Puls.Evals.Cook.Expectations;

/// <summary>
/// What a cook scenario asks for (F18 step 5.4), read once from its <c>expected</c>: where the first turn goes, the user's town, how many people each turn serves
/// (<c>serves</c>: a number for the first turn, or one per turn), the rand budget of the first turn's basket, and the shop the user chose.
/// Each key is read by a named grader. A key that is missing is not asked for; one of the wrong kind is refused, naming the scenario.
/// </summary>
public sealed record CookExpectations(
    string? RoutesTo,
    string? Town,
    IReadOnlyList<int> Serves,
    decimal? Budget,
    ShopExpectation? Shop)
{
    public static CookExpectations Of(EvalScenario scenario) =>
        new(ExpectedRoute.Of(scenario), TownOf(scenario), ServesOf(scenario), BudgetOf(scenario), ShopOf(scenario));

    /// <summary>
    /// The user's town, which the cook names as <c>area</c> in every shop call; null when the scenario names none. A scenario that expects a shop to
    /// price (Checkers) needs it, since the shop's store is the town's.
    /// </summary>
    public static string? TownOf(EvalScenario scenario)
    {
        if (!TryGet(scenario, "town", out var town))
            return ShopOf(scenario) == ShopExpectation.Checkers
                ? throw new InvalidDataException(MessageText.Format(ExpectationMessage.NoTown, new { scenario.Id }))
                : null;
        if (town.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(town.GetString()))
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadTown, new { scenario.Id }));
        return town.GetString()!.Trim();
    }

    /// <summary>The people each turn serves; none when the scenario asks for no number.</summary>
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

    private static readonly IReadOnlyDictionary<string, ShopExpectation> Aliases =
        new Dictionary<string, ShopExpectation> { ["no-branch"] = ShopExpectation.NoBranch };

    /// <summary>The shop the user chose; null when the scenario names none.</summary>
    public static ShopExpectation? ShopOf(EvalScenario scenario)
    {
        string id = scenario.Id;
        if (!TryGet(scenario, "shop", out var shop)) return null;
        if (shop.ValueKind != JsonValueKind.String ||
            !SettingChoice.TryParse(shop.GetString(), out ShopExpectation? parsed, Aliases) || parsed is null)
            throw new InvalidDataException(MessageText.Format(ExpectationMessage.BadShop,
                new { Id = id, Supported = string.Join(", ", SettingChoice.Names<ShopExpectation>()) }));
        return parsed;
    }

    private static bool TryGet(EvalScenario scenario, string key, out JsonElement value)
    {
        value = default;
        return scenario.Expected.ValueKind == JsonValueKind.Object && scenario.Expected.TryGetProperty(key, out value);
    }
}
