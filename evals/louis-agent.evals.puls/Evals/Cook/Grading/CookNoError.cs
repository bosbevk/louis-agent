using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// No turn of the conversation failed: an errored turn is never right (F31). Required. A turn that failed for the
/// infrastructure makes the whole grade Unknown first, before any error is counted.
/// </summary>
/// <remarks>
/// The cook's own, not the general <c>no-error</c>: that one fails on a streamed error before it looks for a broken
/// turn, and gives other reasons, so the cook's stored grades would read differently under the same name (Louis's
/// pick, F49.2 step 7). Its <see cref="Entry"/> lets an eval file name it, <c>cook-no-error</c> (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class CookNoError : ICheck
{
    public string Name => "cook-no-error";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    /// <summary>Unknown when a turn broke; else a pass, or a fail counting the turns that errored.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // A turn that failed for the infrastructure is not the agent's: Unknown, never a fail.
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);

        var errors = trial.Transcript.Turns.Count(EngineEvents.StreamedError);
        return ValueTask.FromResult(errors == 0
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(GradeMessage.NoTurnFailed))
            : new Grade(Name, Version, 0, GradeOutcome.Fail,
                MessageText.Format(GradeMessage.TurnsFailed, new { Count = errors })));
    }

    /// <summary>What a file can set: nothing yet.</summary>
    public sealed record Settings;

    /// <summary>The check as a file names it, <c>cook-no-error</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("cook-no-error", CheckKind.FixedRule)
    {
        /// <summary>Builds the check.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new CookNoError();
    }
}
