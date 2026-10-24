using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals;

/// <summary>
/// An eval: a real task, its scenarios and its graders (F18 step 2). The harness runs it; the eval says what its task is,
/// which modes it runs in and how a trial's transcript scores.
/// </summary>
public interface IEval
{
    /// <summary>The eval's name, as the runner's verb names it ("cook").</summary>
    string Name { get; }

    /// <summary>The identity the eval proves ("puls"); null for an eval that proves none.</summary>
    string? Identity { get; }

    /// <summary>Bumped by any change to a scenario, a mode or a grader.</summary>
    int Version { get; }

    /// <summary>One sentence, shown in the portal.</summary>
    string Proves { get; }

    /// <summary>The modes the task runs in (Puls alone, or Puls with its cook). Some evals read them from the instance.</summary>
    ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken);

    IReadOnlyList<EvalScenario> Scenarios { get; }

    IReadOnlyList<IGrader> Graders { get; }

    /// <summary>The eval's scores for one trial, in F31's scores document (its keys unchanged).</summary>
    EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades);

    /// <summary>What the protocol records about one turn: who answered, its calls, its hosts and its error (F18 step 4.1).</summary>
    TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode);
}
