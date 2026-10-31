using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Facts;

/// <summary>
/// The trial took fewer turns than the limit: a task the agent should finish without going back and forth. The
/// reason says how many turns there were and the limit. A turn the infrastructure broke makes it Unknown.
/// </summary>
/// <remarks>
/// A run fact: it reads what happened in the run, not what the answer says. Its <see cref="Entry"/> builds it from a
/// file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class TurnsUnderCheck(int count, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "turns-under";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Counts the trial's turns and compares the number with the limit, unless a turn was broken.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);

        var turns = trial.Transcript.Turns.Count;
        var measure = new
        {
            Turns = turns,
            Limit = count
        };
        return ValueTask.FromResult(turns < count
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.TurnsUnderPass, measure))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnsOver, measure)));
    }

    /// <summary>What a file can set: the limit in turns, and whether the check must pass.</summary>
    public sealed record Settings(int Count, bool Required = true);

    /// <summary>The check as a file names it, <c>turns-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("turns-under", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a limit under one: no trial could pass.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => settings.Count >= 1
            ? new TurnsUnderCheck(settings.Count, settings.Required)
            : throw Refused(CheckCatalogMessage.BelowOne, "count");
    }
}
