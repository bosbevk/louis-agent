using System.Collections.Frozen;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Shared.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// The shop the user chose priced the conversation (F18 step 5.4: the scenario's <c>shop</c> and <c>town</c>), as the cook's prompt says it works. With
/// Checkers: its shop tool was called, with the user's town as <c>area</c> in every call, and the list's prices link to its site. With <c>any</c> (the
/// user chose no shop, or asked for the recipe only): no shop tool was called, no price is given as fact (a link to a shop, or an amount without ±),
/// and the answer says the prices are estimates. Required where a scenario names a shop. A fixed rule over the calls and the answer, so it needs no model.
/// </summary>
public sealed class PricedAtShop : IGrader
{
    public string Name => "priced-at-shop";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "shop", "town" }.ToFrozenSet();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        if (CookExpectations.ShopOf(trial.Scenario) is not { } shop)
            return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopNotSet));

        var views = turns.Select(TurnView.From).ToList();
        if (views.Count == 0 || views[0].Error is not null || views[0].Answer.Length == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.ShopNothingToRead, new { Turn = 1 }));

        var calls = views.SelectMany(view => view.Calls.Concat(view.SpecialistCalls)).Where(call => Shops.ShopTools.Contains(call.Name)).ToList();
        return shop == ShopExpectation.Any
            ? ForAny(calls, views[0].Answer)
            : ForShop(shop.ToString().ToLowerInvariant(), CookExpectations.TownOf(trial.Scenario)!, calls, views[0].Answer);
    }

    private Grade ForShop(string shop, string town, IReadOnlyList<ToolCall> shopCalls, string answer)
    {
        var calls = shopCalls.Where(call => string.Equals(Text(call.Input, "shop")?.Trim(), shop, StringComparison.OrdinalIgnoreCase)).ToList();
        if (calls.Count == 0) return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopNotCalled, new { Shop = shop }));
        // The cook's prompt: area is the user's town in every call, so the shop's store there is the one searched.
        if (calls.Any(call => !InTown(Text(call.Input, "area"), town)))
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopWrongArea, new { Shop = shop, Town = town }));

        string host = Shops.Hosts.First(known => known.Name == shop).Host;
        return Prices.PriceLinkHosts(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPricesNotLinked, new { Shop = shop }));
    }

    private Grade ForAny(IReadOnlyList<ToolCall> shopCalls, string answer)
    {
        if (shopCalls.Count > 0) return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopCalledForAny));
        // An estimate is `± R65` with no link: Prices counts it neither priced nor guessed, so anything it does count is a price given as fact.
        var priced = Prices.Priced(answer);
        if ((priced?.Priced ?? 0) + (priced?.Guessed ?? 0) > 0 || Prices.PriceLinkHosts(answer).Count > 0)
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPriceForAny));
        return Shops.EstimatesLine(answer)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopAnyRight))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopEstimatesNotSaid));
    }

    /// <summary>Whether a tool's <c>area</c> names the town: either contains the other, case ignored ("malmesbury, western cape" is Malmesbury). No area names none.</summary>
    private static bool InTown(string? area, string town) =>
        !string.IsNullOrWhiteSpace(area)
        && (area.Contains(town, StringComparison.OrdinalIgnoreCase) || town.Contains(area.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
