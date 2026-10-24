using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's first turn went to a role (F18 step 4.2): "went to role X", the way Anthropic's tool-call grader checks which tool
/// was called. A turn that answered itself did not go to the role.
/// </summary>
public sealed class RoutedToGrader(string role, bool required = true) : IGrader
{
    public string Name => "routed-to";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // The route is decided by the first turn; a later turn's hand-over is no reason for its miss.
        var first = trial.Transcript.Turns.FirstOrDefault();
        if (first is null) return ValueTask.FromResult(Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn)));
        string? actual = EngineEvents.RoutedRole(first);
        if (actual == role) return ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.RoutedPass, new { Role = role })));
        return ValueTask.FromResult(actual is null
            ? Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.AnsweredItself, new { Role = role }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.RoutedElsewhere, new { Actual = actual, Role = role })));
    }
}
