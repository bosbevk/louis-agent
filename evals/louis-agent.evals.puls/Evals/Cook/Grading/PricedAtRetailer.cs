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
/// The shop the user chose priced the conversation, as the cook's prompt worked until F18 step 5.7 (Checkers: its shop tool was called, with the user's town as
/// <c>area</c> in every call, and the list's prices link to its site; PnP or Woolworths, which have no catalogue: a page of the site was read with ReadRenderedPage,
/// and the prices link to it). Kept, with its tests, for when a retailer gives access and the cook prices again (emails to PnP, Woolworths and Checkers
/// went out on 2026-10-25); it is not in <see cref="CookEval"/>'s graders now, <see cref="EstimatesAtShop"/> is. It keeps its old name and version, so the runs
/// it graded still read. A scenario that names no shop, or one with no priced shop (<c>any</c>, <c>no-branch</c>), passes: those are the estimates rules.
/// </summary>
public sealed class PricedAtRetailer : IGrader
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
        if (CookExpectations.ShopOf(trial.Scenario) is not { } shop || shop is ShopExpectation.Any or ShopExpectation.NoBranch)
            return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopNotSet));

        var views = turns.Select(TurnView.From).ToList();
        if (views.Count == 0 || views[0].Error is not null || views[0].Answer.Length == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.ShopNothingToRead, new { Turn = 1 }));

        var all = views.SelectMany(view => view.Calls.Concat(view.SpecialistCalls)).ToList();
        var calls = all.Where(call => Shops.ShopTools.Contains(call.Name)).ToList();
        return shop == ShopExpectation.Checkers
            ? ForShop("checkers", CookExpectations.TownOf(trial.Scenario)!, calls, views[0].Answer)
            : ForPage(shop.ToString().ToLowerInvariant(), all, views[0].Answer);
    }

    /// <summary>A shop with no catalogue (PnP, Woolworths): a page of its site was read with ReadRenderedPage, and the prices link to its site. No area: a page has none.</summary>
    private Grade ForPage(string shop, IReadOnlyList<ToolCall> allCalls, string answer)
    {
        string host = Shops.Hosts.First(known => known.Name == shop).Host;
        bool read = allCalls.Any(call => call.Name == Shops.PageTool && Text(call.Input, "url") is { } url && Links.IsHost(url, host));
        if (!read) return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPageNotRead, new { Shop = shop }));
        return Prices.PriceLinkHosts(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopPageRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPricesNotLinked, new { Shop = shop }));
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

    /// <summary>Whether a tool's <c>area</c> names the town: either contains the other, case ignored ("malmesbury, western cape" is Malmesbury). No area names none.</summary>
    private static bool InTown(string? area, string town) =>
        !string.IsNullOrWhiteSpace(area)
        && (area.Contains(town, StringComparison.OrdinalIgnoreCase) || town.Contains(area.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
