using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>Every turn of the trial was answered within a time limit, by the harness's clock.</summary>
/// <remarks>
/// Its <see cref="Entry"/> builds it from a file's settings (Factory Method), nested here so the check and the way
/// it's named from a file sit in one place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class WithinTimeCheck(TimeSpan limit, bool required = true) : ICheck
{
    public string Name => "within-time";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>
    /// Fails on the first turn that took longer than the limit; a turn the infrastructure broke is Unknown.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        var slow = trial.Transcript.Turns.FirstOrDefault(turn => turn.Duration > limit);
        return ValueTask.FromResult(slow is null
            ? Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.WithinTimePass, new { Limit = limit.TotalSeconds }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.SlowTurn,
                new
                {
                    slow.Turn,
                    Seconds = slow.Duration.TotalSeconds,
                    Limit = limit.TotalSeconds
                })));
    }

    /// <summary>What a file can set: the limit in seconds, and whether the check must pass.</summary>
    public sealed record Settings(int Seconds, bool Required = true);

    /// <summary>The check as a file names it, <c>within-time</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("within-time", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings) =>
            new WithinTimeCheck(TimeSpan.FromSeconds(settings.Seconds), settings.Required);
    }
}
