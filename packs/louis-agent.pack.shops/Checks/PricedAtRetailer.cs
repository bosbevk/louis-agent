using System.Collections.Frozen;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Markdown;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Trials;
using LouisAgent.Packs.Shops.Checks.Reading;
using LouisAgent.Packs.Shops.Messages;

namespace LouisAgent.Packs.Shops.Checks;

/// <summary>
/// Checks that the shop the user chose really priced the conversation, the way the cook used to work. For Checkers,
/// its shop tool was called for the user's town every time and the prices link to its site; for PnP or Woolworths,
/// which have no catalogue, a page of the site was read with ReadRenderedPage and the prices link to it. A scenario
/// with no priced shop (none, <c>any</c>, <c>no-branch</c>) passes: those follow the estimates rules.
/// </summary>
/// <remarks>
/// Kept, with its tests, for when a retailer gives access and the cook prices again (emails to PnP, Woolworths and
/// Checkers went out on 2026-10-25); the cook's eval file doesn't name it now, <see cref="EstimatesAtShop"/> is in its
/// place, and it has no catalogue entry. It keeps its old name and version, so the runs it graded still read. The
/// shops pack's own since F49.3 step 4, beside the tools it proves: which sites the prices link to is read by the
/// <see cref="IPriceReader"/> it's given (a Strategy), and the turn from the engine's events.
/// </remarks>
public sealed class PricedAtRetailer(IPriceReader prices) : ICheck
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
        ScenarioShop.Key,
        ScenarioShop.TownKey
    }.ToFrozenSet();

    /// <summary>Reads the calls of every turn and the first turn's answer.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a broken turn or nothing to read; a pass with no priced shop; then Checkers by its tool, PnP or
    /// Woolworths by their page.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        if (ScenarioShop.Of(trial.Scenario) is not { } shop || shop is ShopExpectation.Any or ShopExpectation.NoBranch)
            return Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopNotSet));

        if (turns.Count == 0 || EngineEvents.StreamedError(turns[0]) || EngineEvents.AnswerOf(turns[0]).Length == 0)
            return Verdict.Unknown(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopNothingToRead, new { Turn = 1 }));

        var answer = EngineEvents.AnswerOf(turns[0]);
        return shop == ShopExpectation.Checkers
            ? ForShop("checkers", ScenarioShop.TownOf(trial.Scenario)!, turns, answer)
            : ForPage(shop.Wire(), turns, answer);
    }

    /// <summary>
    /// A shop with no catalogue: a page of its site was read in the browser, and the prices link to its site.
    /// </summary>
    private Grade ForPage(string shop, IReadOnlyList<TurnTranscript> turns, string answer)
    {
        string host = ShopSites.Hosts.First(known => known.Name == shop).Host;
        bool read = turns.SelectMany(turn => EngineEvents.ToolInputsOf(turn, ShopSites.RenderedPageTool))
            .Any(input => EngineEvents.TextOf(input, "url") is { } url && Links.IsHost(url, host));
        if (!read)
            return Verdict.Fail(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopPageNotRead, new { Shop = shop }));
        return prices.LinkedSites(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopPageRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopPricesNotLinked, new { Shop = shop }));
    }

    /// <summary>
    /// A shop with a catalogue tool: it was called for this shop, every call named the user's town as its area, and
    /// the prices link to the shop's site.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade ForShop(string shop, string town, IReadOnlyList<TurnTranscript> turns, string answer)
    {
        var calls = turns
            .SelectMany(turn => ShopSites.ShopTools.SelectMany(tool => EngineEvents.ToolInputsOf(turn, tool)))
            .Where(input => string.Equals(EngineEvents.TextOf(input, "shop")?.Trim(), shop,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (calls.Count == 0)
            return Verdict.Fail(Name, Version, MessageText.Format(ShopCheckMessage.ShopNotCalled, new { Shop = shop }));
        // The cook's prompt: area is the user's town in every call, so the shop's store there is the one searched.
        if (calls.Any(input => !InTown(EngineEvents.TextOf(input, "area"), town)))
            return Verdict.Fail(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopWrongArea, new
                {
                    Shop = shop,
                    Town = town
                }));

        string host = ShopSites.Hosts.First(known => known.Name == shop).Host;
        return prices.LinkedSites(answer).Contains(host)
            ? Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopRight, new { Shop = shop }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopPricesNotLinked, new { Shop = shop }));
    }

    /// <summary>
    /// Whether a call's area is the user's town: one holds the other, case ignored ("malmesbury, western cape").
    /// </summary>
    private static bool InTown(string? area, string town) =>
        !string.IsNullOrWhiteSpace(area)
        && (area.Contains(town, StringComparison.OrdinalIgnoreCase) ||
            town.Contains(area.Trim(), StringComparison.OrdinalIgnoreCase));
}
