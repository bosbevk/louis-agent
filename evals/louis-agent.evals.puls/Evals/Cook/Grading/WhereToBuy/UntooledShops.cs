using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Shared.Scoring;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.WhereToBuy;

/// <summary>
/// The cook never puts a price column in its list for a shop it can't read, such as Shoprite. It can read Checkers
/// through its tools, and PnP and Woolworths as a rendered page; anything else would be a made-up price. Required.
/// </summary>
public sealed class UntooledShops : IGrader
{
    /// <inheritdoc />
    public string Name => "untooled-shops";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public GraderKind Kind => GraderKind.FixedRule;

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

        var shops = turns.SelectMany(turn => Shops.UntooledShops(EngineEvents.AnswerOf(turn)))
            .Distinct(StringComparer.Ordinal).ToList();
        if (shops.Count > 0)
            return Verdict.Fail(Name, Version,
                MessageText.Format(GradeMessage.UntooledShopColumn, new { Shops = string.Join(", ", shops) }));

        return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.UntooledShopsNone));
    }
}
