using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's first turn went where the eval expects for this trial (F18 step 4.2): "went to role X", the way Anthropic's tool-call
/// grader checks which tool was called. The expected role is the eval's, per trial; null means the agent should answer itself. As the
/// Python check: a turn that errored is not right, and a specialist's own hand-over is not the turn's route. An eval whose route is a
/// scenario's `expected` key names it in <paramref name="expectedKeys"/>, so the loader checks it is read and fed (F18 step 5.4).
/// </summary>
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
