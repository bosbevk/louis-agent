using System.Collections.Frozen;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Trials;
using LouisAgent.Packs.Shops.Checks.Reading;
using LouisAgent.Packs.Shops.Messages;

namespace LouisAgent.Packs.Shops.Checks;

/// <summary>
/// The conversation gives estimates and says where to buy, and looks up no shop's prices (Louis, 2026-10-25: "keep
/// cook but just rework it to not lookup prices but just focus on local recipes and where to shop"). Whatever shop the
/// scenario names (or <c>any</c>), no shop tool was called, no shop's page was read (ReadRenderedPage or FetchUrl), no
/// price links to a shop or is given as fact, and the answer says the prices are estimates. A scenario that names a
/// shop also needs the answer to name that shop's branch under Where to buy; <c>no-branch</c> needs the answer to say
/// there is none.
/// </summary>
/// <remarks>
/// A fixed rule over the calls and the answer, the shops pack's own (F49.3), listed in <see cref="ShopsChecks.All"/>.
/// Whether a price is given as fact is read by an <see cref="IPriceReader"/> the eval file names (a Strategy, Louis's
/// picks on finding M and in step 3), since how an answer writes a price is its identity's knowledge, never the
/// pack's. The turn is read from the engine's events (<see cref="EngineEvents"/>). Its <see cref="Entry"/> builds it
/// from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0059-an-identity-ships-readers-not-checks.md">ADR 0059: an identity ships readers, not checks</seealso>
public sealed class EstimatesAtShop(IPriceReader prices) : ICheck
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
        ScenarioShop.Key,
        ScenarioShop.TownKey
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
        if (ScenarioShop.Of(trial.Scenario) is not { } shop)
            return Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopNotSet));

        if (turns.Count == 0 || EngineEvents.StreamedError(turns[0]) || EngineEvents.AnswerOf(turns[0]).Length == 0)
            return Verdict.Unknown(Name, Version,
                MessageText.Format(ShopCheckMessage.ShopNothingToRead, new { Turn = 1 }));

        var answer = EngineEvents.AnswerOf(turns[0]);
        if (turns.Any(CallsAShopTool) || turns.Any(ReadsAShopPage))
            return Verdict.Fail(Name, Version, MessageText.Format(ShopCheckMessage.ShopCalledForAny));
        if (prices.AsFact(answer) > 0 || prices.LinkedSites(answer).Count > 0)
            return Verdict.Fail(Name, Version, MessageText.Format(ShopCheckMessage.ShopPriceForAny));
        if (!ShopLines.EstimatesLine(answer))
            return Verdict.Fail(Name, Version, MessageText.Format(ShopCheckMessage.ShopEstimatesNotSaid));

        return shop switch
        {
            ShopExpectation.Any => Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopAnyRight)),
            ShopExpectation.NoBranch => ShopLines.NoBranchLine(answer)
                ? Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.ShopAnyRight))
                : Verdict.Fail(Name, Version, MessageText.Format(ShopCheckMessage.ShopNoBranchNotSaid)),
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
            MessageText.Format(ShopCheckMessage.ShopEstimatesRight, new { Shop = shop })),
        BranchFinding.NoSection => Verdict.Fail(Name, Version,
            MessageText.Format(ShopCheckMessage.ShopNoWhereToBuySection)),
        BranchFinding.OnlyNearest => Verdict.Fail(Name, Version,
            MessageText.Format(ShopCheckMessage.ShopOnlyNearestBranch, new { Shop = shop })),
        _ => Verdict.Fail(Name, Version,
            MessageText.Format(ShopCheckMessage.ShopNoBranchWithPlace, new { Shop = shop })),
    };

    /// <summary>A shop tool called in the turn, by the agent or a specialist.</summary>
    private static bool CallsAShopTool(TurnTranscript turn) =>
        EngineEvents.ToolsCalled(turn).Concat(EngineEvents.SpecialistToolsCalled(turn))
            .Any(ShopSites.ShopTools.Contains);

    /// <summary>
    /// A page of a shop's site read in the turn, by either tool that reads a page: the cook looks up no shop now.
    /// </summary>
    private static bool ReadsAShopPage(TurnTranscript turn) =>
        ShopSites.PageTools.SelectMany(tool => EngineEvents.ToolInputsOf(turn, tool))
            .Any(input => UrlOf(input) is { } url && ShopSites.SiteOf(url) is not null);

    /// <summary>A page tool's <c>url</c>, or null when the input has none.</summary>
    private static string? UrlOf(JsonElement input) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty("url", out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>What a file can set: the reader that tells whether the answer gives prices as fact.</summary>
    public sealed record Settings(string Reader);

    /// <summary>The check as a file names it, <c>estimates-at-shop</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("estimates-at-shop", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check through the price reader the file names; a blank reader is refused, as is one the
        /// catalogue doesn't have or one that reads no prices.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            !string.IsNullOrWhiteSpace(settings.Reader)
                ? new EstimatesAtShop(context.Readers.Get<IPriceReader>(settings.Reader))
                : throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.NoText, new
                {
                    Type,
                    Field = "reader"
                }));
    }
}
