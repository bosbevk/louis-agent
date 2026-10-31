using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Packs.Shops.Messages;

namespace LouisAgent.Packs.Shops.Checks.Reading;

/// <summary>
/// The shop a scenario says the user chose, read from its <c>shop</c>, and the town they shop in, from its
/// <c>town</c>; none when it names none. A value that isn't one of the shops, or a town that isn't a name, is
/// refused, naming the scenario.
/// </summary>
/// <remarks>Parsed once with <see cref="SettingChoice"/> (ADR 0024), as Puls's cook read it before F49.3.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class ScenarioShop
{
    /// <summary>The scenario key that names the shop.</summary>
    public const string Key = "shop";

    /// <summary>The scenario key that names the town the user shops in.</summary>
    public const string TownKey = "town";

    /// <summary>The names a scenario may write besides the members' own: <c>no-branch</c>.</summary>
    private static readonly IReadOnlyDictionary<string, ShopExpectation> Aliases =
        new Dictionary<string, ShopExpectation> { ["no-branch"] = ShopExpectation.NoBranch };

    /// <summary>The shop the user chose; null when the scenario names none.</summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public static ShopExpectation? Of(EvalScenario scenario)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object ||
            !scenario.Expected.TryGetProperty(Key, out var shop))
            return null;
        if (shop.ValueKind == JsonValueKind.String &&
            SettingChoice.TryParse(shop.GetString(), out ShopExpectation? parsed, Aliases) && parsed is not null)
            return parsed;
        throw new InvalidDataException(MessageText.Format(ShopCheckMessage.BadShop, new
        {
            scenario.Id,
            Supported = string.Join(", ", SettingChoice.Names<ShopExpectation>())
        }));
    }

    /// <summary>
    /// The town the user shops in, from the scenario's <c>town</c>; none when it names none, unless its shop is
    /// Checkers, whose tool searches the town's store, so a Checkers scenario without a town is refused, as is a town
    /// that isn't a name.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    public static string? TownOf(EvalScenario scenario)
    {
        var town = default(JsonElement);
        var hasTown = scenario.Expected.ValueKind == JsonValueKind.Object &&
                      scenario.Expected.TryGetProperty(TownKey, out town);
        if (!hasTown && Of(scenario) == ShopExpectation.Checkers)
            throw new InvalidDataException(MessageText.Format(ShopCheckMessage.NoTown, new { scenario.Id }));
        if (!hasTown) return null;
        if (town.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(town.GetString()))
            throw new InvalidDataException(MessageText.Format(ShopCheckMessage.BadTown, new { scenario.Id }));
        return town.GetString()!.Trim();
    }
}
