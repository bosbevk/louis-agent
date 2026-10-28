using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals;

/// <summary>
/// An eval: a real task, its scenarios and its graders (F18 step 2). The harness runs it; the eval says what its task is,
/// which modes it runs in, how its graders judge a trial and what a turn did.
/// </summary>
public interface IEval
{
    /// <summary>The eval's name, as the runner's verb names it ("cook").</summary>
    string Name { get; }

    /// <summary>Bumped by any change to a scenario, a mode or a grader.</summary>
    int Version { get; }

    /// <summary>One sentence, shown in the portal.</summary>
    string Proves { get; }

    /// <summary>
    /// Where the eval's manual is (F18 step 7.1): a markdown file beside the eval, as a path from the repository root with forward slashes ("evals/louis-agent.evals.puls/Manuals/cook.md"); the
    /// portal opens it from the eval's card. Null for an eval with none.
    /// </summary>
    string? Manual => null;

    /// <summary>The modes the task runs in (the agent alone, or with a helper). Some evals read them from the instance.</summary>
    ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken);

    IReadOnlyList<EvalScenario> Scenarios { get; }

    /// <summary>The dataset the scenarios came from (its name, version and hash), which a run's first line records; null for an eval with no dataset file, such as the stub (F18 step 5b).</summary>
    DatasetManifest? Dataset => null;

    IReadOnlyList<IGrader> Graders { get; }

    /// <summary>A person's labels of stored answers, which a model grader is calibrated against (F18 step 6.3); none for an eval with no <c>labels/</c> folder.</summary>
    IReadOnlyList<LouisAgent.Evals.Labels.LabeledAnswer> Labels => [];

    /// <summary>What the protocol records about one turn: who answered, its calls, its hosts and its error (F18 step 4.1).</summary>
    TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode);
}
