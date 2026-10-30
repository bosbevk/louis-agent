using System.Collections.Frozen;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;

/// <summary>
/// The conversation gives estimates and says where to buy, and looks up no shop's prices (Louis, 2026-10-25: "keep
/// cook but just rework it to not lookup prices but just focus on local recipes and where to shop"). Whatever shop the
/// scenario names (or <c>any</c>), no shop tool was called, no shop's page was read (ReadRenderedPage or FetchUrl), no
/// price links to a shop or is given as fact, and the answer says the prices are estimates. A scenario that names a
/// shop also needs the answer to name that shop's branch under Where to buy; <c>no-branch</c> needs the answer to say
/// there is none.
/// </summary>
/// <remarks>
/// A fixed rule over the calls and the answer. Renamed from <c>priced-at-shop</c>, which said the opposite of what it
/// now checks (see <see cref="PricedAtRetailer"/>). Its <see cref="Entry"/> lets an eval file name it,
/// <c>estimates-at-shop</c> (Factory Method), as the general checks' entries do; it takes no settings. It moves to the
/// shops pack in F49.3.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class EstimatesAtShop : ICheck
{
    /// <inheritdoc />
    public string Name => "estimates-at-shop";

    /// <inheritdoc />
    public int Version => 2;

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

    /// <summary>Reads the calls of every turn and the first turn's answer.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a broken turn or nothing to read; a pass with no shop set; a fail for a shop looked up, a price as
    /// fact or no word of estimates; then the branch the scenario's shop needs.
    /// </summary>
    /// <remarks>Guard clauses, then a switch expression on the shop (ADR 0051).</remarks>
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
        // An estimate is `± R65` with no link: Prices counts it neither priced nor guessed, so anything it does count
        // is a price given as fact.
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

    /// <summary>
    /// The branch under Where to buy: named, or the case it is not (no section, no branch with a place, only a nearest
    /// one), so a fail says why.
    /// </summary>
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

    /// <summary>
    /// A page of a shop's site read, by either tool that reads a page: the cook looks up no shop now.
    /// </summary>
    private static bool ReadsAShop(ToolCall call) =>
        call.Name is Shops.PageTool or "FetchUrl" && Text(call.Input, "url") is { } url &&
        Shops.SiteOf(url) is not null;

    /// <summary>A string field of a tool's input, or null when it is absent or isn't a string.</summary>
    private static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>What a file can set: nothing yet; the shop and the town are the scenario's.</summary>
    public sealed record Settings;

    /// <summary>The check as a file names it, <c>estimates-at-shop</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("estimates-at-shop", CheckKind.FixedRule)
    {
        /// <summary>Builds the check.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new EstimatesAtShop();
    }
}
