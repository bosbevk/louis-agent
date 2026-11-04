using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Packs.WhereToBuy.Checks.Reading;
using LouisAgent.Packs.WhereToBuy.Messages;

namespace LouisAgent.Packs.WhereToBuy.Checks;

/// <summary>
/// The cook never puts a price column in its list for a shop: no shop's prices are read (ADR 0069), so a "Checkers"
/// or "Shoprite" column could only hold made-up prices. Required.
/// </summary>
/// <remarks>
/// Its <see cref="Entry"/> lets an eval file name it, <c>untooled-shops</c> (Factory Method), as the general checks'
/// entries do; it takes no settings. The where-to-buy pack's own (F49.3, F50.2), listed in
/// <see cref="WhereToBuyChecks.All"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class UntooledShops : ICheck
{
    /// <inheritdoc />
    public string Name => "untooled-shops";

    /// <inheritdoc />
    public int Version => 2;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => true;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    /// <summary>
    /// Reads every turn's list, the follow-ups too, and fails naming the shops it shouldn't have priced at.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>Unknown when a turn never got its answer; otherwise a pass, or a fail with the shops' sites.</summary>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;

        var shops = turns.SelectMany(turn => ShopSites.UntooledShops(EngineEvents.AnswerOf(turn)))
            .Distinct(StringComparer.Ordinal).ToList();
        if (shops.Count > 0)
            return Verdict.Fail(Name, Version,
                MessageText.Format(ShopCheckMessage.UntooledShopColumn, new { Shops = string.Join(", ", shops) }));

        return Verdict.Pass(Name, Version, MessageText.Format(ShopCheckMessage.UntooledShopsNone));
    }

    /// <summary>What a file can set: nothing yet.</summary>
    public sealed record Settings;

    /// <summary>The check as a file names it, <c>untooled-shops</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("untooled-shops", CheckKind.FixedRule)
    {
        /// <summary>Builds the check.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new UntooledShops();
    }
}
