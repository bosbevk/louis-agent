using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Stub;

/// <summary>The stub's one required fixed-rule grader: its run is fixed, so it passes, and it reads no expected key.</summary>
public sealed class StubGrader : IGrader
{
    public string Name => "stub-run-fixed";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(RunnerMessage.StubRunIsFixed)));
}
