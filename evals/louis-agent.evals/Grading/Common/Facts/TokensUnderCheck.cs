using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Facts;

/// <summary>
/// The trial's tokens stay under a limit: every turn's input, cache-write, cache-read and output counts summed, as the
/// stream reports them. A turn that reported no usage, or not all four counts, is Unknown, never a pass: the total
/// would be short. The reason says the total and the limit.
/// </summary>
/// <remarks>
/// A run fact, like <c>cost-under</c>, but in tokens rather than money: a token budget holds when prices change, and
/// it measures how much context the agent pulled in, which a cost hides once caching makes tokens cheap. Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class TokensUnderCheck(long count, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "tokens-under";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Adds up the turns' tokens and compares the total with the limit, or says it can't tell.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);

        long total = 0;
        foreach (var turn in trial.Transcript.Turns)
        {
            if (!EngineEvents.HasUsage(turn))
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.NoUsage, new { turn.Turn })));
            if (EngineEvents.TokensOf(turn) is not { } tokens)
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.NoTokenCounts, new { turn.Turn })));
            total += tokens;
        }

        var measure = new
        {
            Total = total,
            Limit = count
        };
        return ValueTask.FromResult(total < count
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.TokensUnderPass, measure))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TokensOver, measure)));
    }

    /// <summary>What a file can set: the limit in tokens, and whether the check must pass.</summary>
    public sealed record Settings(long Count, bool Required = true);

    /// <summary>The check as a file names it, <c>tokens-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tokens-under", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a limit under one: no trial could pass.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => settings.Count >= 1
            ? new TokensUnderCheck(settings.Count, settings.Required)
            : throw Refused(CheckCatalogMessage.BelowOne, "count");
    }
}
