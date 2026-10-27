using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Trials;

/// <summary>An eval that is only its graders; the trial comes from the builder.</summary>
internal sealed class GradedEval(params IGrader[] graders) : IEval
{
    public string Name => "stub-cook";
    public int Version => 1;
    public string Proves => "The grading step.";

    public ValueTask<IReadOnlyList<EvalMode>>
        ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>([Mode]);

    public IReadOnlyList<EvalScenario> Scenarios { get; } = [];
    public IReadOnlyList<IGrader> Graders { get; } = graders;
    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotSupportedException();
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotSupportedException();
}
