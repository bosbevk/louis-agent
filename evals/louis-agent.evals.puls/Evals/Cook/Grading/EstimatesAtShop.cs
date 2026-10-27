using LouisAgent.Choices;
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
/// The conversation gives estimates and says where to buy, and looks up no shop's prices (F18 step 5.7; Louis, 2026-10-25: "keep cook but just rework it to not
/// lookup prices but just focus on local recipes and where to shop"; renamed from <c>priced-at-shop</c>, which said the opposite of what it now checks, see
/// <see cref="PricedAtRetailer"/>). Whatever shop the scenario names (or <c>any</c>), no shop tool was called, no shop's page was read (ReadRenderedPage or
/// FetchUrl), no price links to a shop or is given as fact, and the answer says the prices are estimates. A scenario that names a shop also needs the
/// answer to name that shop's branch under Where to buy; <c>no-branch</c> needs the answer to say there is none. A fixed rule over the calls and the answer.
/// </summary>
public sealed class EstimatesAtShop : IGrader
{
    public string Name => "estimates-at-shop";
    public int Version => 2;
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

        var all = views.SelectMany(view => view.Calls.Concat(view.SpecialistCalls)).ToList();
        string answer = views[0].Answer;
        if (all.Any(call => Shops.ShopTools.Contains(call.Name)) || all.Any(ReadsAShop))
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopCalledForAny));
        // An estimate is `± R65` with no link: Prices counts it neither priced nor guessed, so anything it does count is a price given as fact.
        var priced = Prices.Priced(answer);
        if ((priced?.Priced ?? 0) + (priced?.Guessed ?? 0) > 0 || Prices.PriceLinkHosts(answer).Count > 0)
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopPriceForAny));
        if (!Shops.EstimatesLine(answer))
            return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopEstimatesNotSaid));

        return shop switch
        {
            ShopExpectation.Any => Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopAnyRight)),
            ShopExpectation.NoBranch => Shops.NoBranchLine(answer)
                ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ShopAnyRight))
                : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopNoBranchNotSaid)),
            _ => BranchVerdict(answer, shop.Wire()),
        };
    }

    /// <summary>The branch under Where to buy: named, or the case it is not (no section, no branch with a place, only a nearest one), so a fail says why.</summary>
    private Grade BranchVerdict(string answer, string shop) => BranchReading.Of(answer, shop) switch
    {
        BranchFinding.Named => Verdict.Pass(Name, Version,
            MessageText.Format(GradeMessage.ShopEstimatesRight, new { Shop = shop })),
        BranchFinding.NoSection => Verdict.Fail(Name, Version,
            MessageText.Format(GradeMessage.ShopNoWhereToBuySection)),
        BranchFinding.OnlyNearest => Verdict.Fail(Name, Version,
            MessageText.Format(GradeMessage.ShopOnlyNearestBranch, new { Shop = shop })),
        _ => Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.ShopNoBranchWithPlace, new { Shop = shop })),
    };

    /// <summary>A page of a shop's site read, by either tool that reads a page: the cook looks up no shop now.</summary>
    private static bool ReadsAShop(ToolCall call) =>
        call.Name is Shops.PageTool or "FetchUrl" && Text(call.Input, "url") is { } url &&
        Shops.SiteOf(url) is not null;

    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
