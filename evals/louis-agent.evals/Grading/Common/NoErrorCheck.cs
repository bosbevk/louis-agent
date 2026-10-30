using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// No turn of the trial raised an error: the agent's error in the stream fails it, with its message in the reason. A
/// specialist's error is not the turn's, and a turn that failed for the infrastructure is Unknown, not a fail.
/// </summary>
/// <remarks>
/// Its <see cref="Entry"/> builds it from a file's settings (Factory Method), nested here so the check and the way
/// it's named from a file sit in one place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class NoErrorCheck(bool required = true) : ICheck
{
    public string Name => "no-error";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Fails on the first turn whose stream reported an error; passes when none did.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var errored = trial.Transcript.Turns.FirstOrDefault(EngineEvents.StreamedError);
        if (errored is not null)
        {
            string? message = EngineEvents.ErrorMessageOf(errored);
            string reason = message is null
                ? MessageText.Format(CommonGradeMessage.ErrorInTurn, new { errored.Turn })
                : MessageText.Format(CommonGradeMessage.ErrorInTurnWithMessage,
                    new
                    {
                        errored.Turn,
                        Message = message
                    });
            return ValueTask.FromResult(Verdict.Fail(Name, Version, reason));
        }

        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        return ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.NoErrorPass)));
    }

    /// <summary>What a file can set: only whether the check must pass.</summary>
    public sealed record Settings(bool Required = true);

    /// <summary>The check as a file names it, <c>no-error</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("no-error", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings) => new NoErrorCheck(settings.Required);
    }
}
