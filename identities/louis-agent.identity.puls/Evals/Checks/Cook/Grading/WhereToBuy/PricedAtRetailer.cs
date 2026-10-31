using System.Collections.Frozen;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Markdown;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;

/// <summary>
/// Checks that the shop the user chose really priced the conversation, the way the cook used to work. For Checkers,
/// its shop tool was called for the user's town every time and the prices link to its site; for PnP or Woolworths,
/// which have no catalogue, a page of the site was read with ReadRenderedPage and the prices link to it. A scenario
/// with no priced shop (none, <c>any</c>, <c>no-branch</c>) passes: those follow the estimates rules.
/// </summary>
/// <remarks>
/// Kept, with its tests, for when a retailer gives access and the cook prices again (emails to PnP, Woolworths and
/// Checkers went out on 2026-10-25); the cook's eval file doesn't name it now, <see cref="EstimatesAtShop"/> is in its
/// place, and it has no catalogue entry. It keeps its old name and version, so the runs it graded still read.
/// </remarks>
public sealed class PricedAtRetailer : ICheck
{
    /// <summary>Its old name, kept so the runs it graded still read.</summary>
    public string Name => "priced-at-shop";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => true;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>
    {
        "shop",
        "town"
    }.ToFrozenSet();

    /// <summary>Checks the chosen shop priced the conversation, by its tool or its page.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a broken turn; a pass with no priced shop; else the shop's own rule, by its catalogue or its page.
    /// </summary>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        if (CookExpectations.ShopOf(trial.Scenario) is not { } shop ||
            shop is ShopExpectation.Any or ShopExpectation.NoBranch)
            return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopNotSet));

        var views = turns.Select(TurnView.From).ToList();
        if (views.Count == 0 || views[0].Error is not null || views[0].Answer.Length == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.ShopNothingToRead, new { Turn = 1 }));

        var all = views.SelectMany(view => view.Calls.Concat(view.SpecialistCalls)).ToList();
        var calls = all.Where(call => Shops.ShopTools.Contains(call.Name)).ToList();
        return shop == ShopExpectation.Checkers
            ? ForShop("checkers", CookExpectations.TownOf(trial.Scenario)!, calls, views[0].Answer)
            : ForPage(shop.Wire(), all, views[0].Answer);
    }

    /// <summary>
    /// A shop with no catalogue (PnP, Woolworths): a page of its site was read with ReadRenderedPage, and the prices
    /// link to its site. No area: a page has none.
    /// </summary>
    private Grade ForPage(string shop, IReadOnlyList<ToolCall> allCalls, string answer)
    {
        string host = Shops.Hosts.First(known => known.Name == shop).Host;
        bool read = allCalls.Any(call =>
            call.Name == Shops.PageTool && Text(call.Input, "url") is { } url && Links.IsHost(url, host));
        if (!read)
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPageNotRead, new { Shop = shop }));
        return Prices.PriceLinkHosts(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopPageRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPricesNotLinked, new { Shop = shop }));
    }

    /// <summary>
    /// Checkers, with a catalogue: its shop tool was called, for the user's town in every call, and the prices link
    /// to its site.
    /// </summary>
    private Grade ForShop(string shop, string town, IReadOnlyList<ToolCall> shopCalls, string answer)
    {
        var calls = shopCalls.Where(call =>
            string.Equals(Text(call.Input, "shop")?.Trim(), shop, StringComparison.OrdinalIgnoreCase)).ToList();
        if (calls.Count == 0)
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopNotCalled, new { Shop = shop }));
        // The cook's prompt: area is the user's town in every call, so the shop's store there is the one searched.
        if (calls.Any(call => !InTown(Text(call.Input, "area"), town)))
            return Verdict.Fail(Name, Version,
                MessageText.Format(GradeMessage.ShopWrongArea, new
                {
                    Shop = shop,
                    Town = town
                }));

        string host = Shops.Hosts.First(known => known.Name == shop).Host;
        return Prices.PriceLinkHosts(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPricesNotLinked, new { Shop = shop }));
    }

    /// <summary>
    /// Whether a tool's <c>area</c> names the town: either contains the other, case ignored ("malmesbury, western
    /// cape" is Malmesbury). No area names none.
    /// </summary>
    private static bool InTown(string? area, string town) =>
        !string.IsNullOrWhiteSpace(area)
        && (area.Contains(town, StringComparison.OrdinalIgnoreCase) ||
            town.Contains(area.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>A string field of a tool's input, or null when it is absent or isn't a string.</summary>
    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
