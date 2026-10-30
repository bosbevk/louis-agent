using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's first turn went where the eval expects for this trial: "went to role X", the way Anthropic's tool-call
/// check reads which tool was called. The expected role is the eval's, per trial; null means the agent should answer
/// itself. A turn that errored is not right, and a specialist's own hand-over is not the turn's route. An eval whose
/// route is a scenario's <c>expected</c> key names it in <paramref name="expectedKeys"/>, so the loader checks it is
/// read and fed.
/// </summary>
/// <remarks>
/// The only general check with no catalogue entry: its expected role is a function of the trial, which a file can't
/// hold; F49.2's eval file settles how a route is stated, and the entry comes with it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class RoutedToCheck(
    Func<Trial, string?> expectedRole,
    bool required = true,
    IReadOnlySet<string>? expectedKeys = null) : ICheck
{
    public string Name => "routed-to";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = expectedKeys ?? new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // The route is decided by the first turn; a later turn's hand-over is no reason for its miss.
        var first = trial.Transcript.Turns.FirstOrDefault();
        if (first is null)
            return ValueTask.FromResult(Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn)));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return ValueTask.FromResult(unknown);
        if (EngineEvents.StreamedError(first))
            return ValueTask.FromResult(Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed)));

        string? expected = expectedRole(trial);
        string? actual = EngineEvents.RoutedRole(first);
        if (actual == expected)
            return ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(
                expected is null ? CommonGradeMessage.AnsweredAsExpected : CommonGradeMessage.RoutedPass,
                new { Role = expected })));
        if (expected is null)
            return ValueTask.FromResult(Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.HandedAway, new { Actual = actual })));
        return ValueTask.FromResult(actual is null
            ? Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.AnsweredItself, new { Role = expected }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.RoutedElsewhere, new { Actual = actual, Role = expected })));
    }
}
