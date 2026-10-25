using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Template.Evals.Example.Grading;
using LouisAgent.Evals.Template.Evals.Example.Scoring;
using LouisAgent.Evals.Template.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Template.Evals.Example;

/// <summary>
/// One eval of this set (docs/guides/add-an-eval-set.md, section 4). Copy the project, rename <c>Example</c> and <c>Template</c>, and write what throws
/// <see cref="NotImplementedException"/>. Its references and known-bad transcripts go in <c>Transcripts/</c> beside this file (section 7); the folder is
/// not here because git keeps no empty folder and the conformance check refuses a stray file.
/// </summary>
public sealed class ExampleEval : IEval
{
    // Static, so the scenarios can load with them: the loader checks every expected key against these graders (section 3).
    private static readonly IReadOnlyList<IGrader> GraderList = [new ExampleGrader()];

    // The eval's name: MCP-safe (letters, digits, '-' and '_'), and the portal's check id (section 4).
    public string Name => "template-example";

    // The identity the eval proves; null for an eval that proves none (section 4).
    public string? Identity => "template";

    // Bumped by any change to a scenario, a mode or a grader (section 4).
    public int Version => 1;

    // One sentence, the portal's title: a message enum member, not inline text (ADR 0009; section 4).
    public string Proves => MessageText.Format(TemplateMessage.ExampleProves);

    // The modes the task runs in, each a role and the toolsets a session opens with; some evals read them from the instance (section 4). Write it.
    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) => throw new NotImplementedException();

    // The scenarios, from the dataset beside this eval: EvalFiles finds it, so no eval builds a path (sections 2 and 4).
    private static readonly Dataset Loaded = DatasetLoader.Load(EvalFiles.DatasetOf(typeof(ExampleEval)), GraderList);

    public IReadOnlyList<EvalScenario> Scenarios { get; } = Loaded.Scenarios;

    // The dataset's name, version and hash: a run's first line records them, so a later run (or a re-score) can say which scenarios it graded.
    public DatasetManifest? Dataset => Loaded.Manifest;

    // The graders, in the order the portal shows them; at least one required fixed rule (section 3).
    public IReadOnlyList<IGrader> Graders => GraderList;

    // The eval's own scores for one trial: keys that stay put once a baseline exists (section 4). Write ExampleScorer.
    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => ExampleScorer.Score(trial);

    // What the protocol records about one turn: who answered, its calls, the hosts it read and its error (section 4). Write it.
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
}
