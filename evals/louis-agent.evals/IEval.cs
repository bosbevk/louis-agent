using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;

namespace LouisAgent.Evals;

/// <summary>
/// An eval: a real task, its scenarios and its checks. The harness runs it; the eval says what its task is, which
/// modes it runs in, and how its checks judge a trial.
/// </summary>
/// <remarks>
/// An eval is a folder of files (<c>Files/FileEval</c>, F49.2); the stub and the template's example are the C# ones
/// left. The runner runs both through this one contract.
/// </remarks>
public interface IEval
{
    /// <summary>The eval's name, as the runner's verb names it ("cook").</summary>
    string Name { get; }

    /// <summary>
    /// The identity the eval proves, as its file names it (<c>puls</c>); null for one that proves none, such as the
    /// stub. <c>EVALS=</c> chooses evals by it.
    /// </summary>
    string? Identity => null;

    /// <summary>Bumped by any change to a scenario, a mode or a check.</summary>
    int Version { get; }

    /// <summary>One sentence, shown in the portal.</summary>
    string Proves { get; }

    /// <summary>
    /// Where the eval's manual is: a markdown file beside the eval, as a path from the repository root with forward
    /// slashes (<c>evals/louis-agent.evals.puls/Manuals/cook.md</c>); the portal opens it from the eval's card. Null
    /// for an eval with none.
    /// </summary>
    string? Manual => null;

    /// <summary>
    /// The modes the task runs in (the agent alone, or with a helper). Some evals read them from the instance.
    /// </summary>
    ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken);

    /// <summary>The scenarios a run asks, from the eval's dataset.</summary>
    IReadOnlyList<EvalScenario> Scenarios { get; }

    /// <summary>
    /// The dataset the scenarios came from (its name, version and hash), which a run's first line records; null for an
    /// eval with no dataset file, such as the stub.
    /// </summary>
    DatasetManifest? Dataset => null;

    /// <summary>The checks, in the order the portal shows them; at least one is a required fixed rule.</summary>
    IReadOnlyList<ICheck> Checks { get; }

    /// <summary>
    /// A person's labels of stored answers, which a model check is calibrated against; none for an eval with no
    /// <c>labels/</c> folder.
    /// </summary>
    IReadOnlyList<LabeledAnswer> Labels => [];

    /// <summary>
    /// What the protocol records about one turn: who answered, its calls, its hosts and its error; read from the
    /// engine's events unless the eval says its own.
    /// </summary>
    TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => TurnFacts.Of(turn);
}
