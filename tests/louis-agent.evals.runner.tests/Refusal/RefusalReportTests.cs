using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Refusal;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Refusal;

// The runner's refusals (F18 step 4.4 review): a refusal of the harness while the catalogue is built reaches stderr with its reason and
// exit code 1, as the runner's own refusals do. Written first: RefusalReport threw NotImplementedException on the red run.
public class RefusalReportTests
{
    [Test]
    public void TheHarnessAndTheRunnersRefusalsAreRefusals()
    {
        Assert.That(RefusalReport.IsRefusal(new InvalidDataException("a dataset")), Is.True);
        Assert.That(RefusalReport.IsRefusal(new RunnerException("an eval")), Is.True);
    }

    [Test]
    public void AnyOtherErrorIsNotARefusal() =>
        Assert.That(RefusalReport.IsRefusal(new InvalidOperationException("a bug")), Is.False);

    [Test]
    public void ItWritesTheReasonAndExitsWithOne()
    {
        var stderr = new StringWriter();

        int code = RefusalReport.Report(new InvalidDataException("Dataset folder 'x' has no dataset.json."), stderr);

        Assert.That(code, Is.EqualTo(1));
        Assert.That(stderr.ToString().Trim(), Is.EqualTo("Dataset folder 'x' has no dataset.json."));
    }

    [Test]
    public void ADatasetThatWontLoadIsRefusedWithItsReason()
    {
        // The dataset is read when the eval is built, which is while the catalogue is built: the same path the runner's start takes.
        var error = Assert.Throws<InvalidDataException>(() => new EvalCatalog([new BrokenSet()], new StubEval()));
        var stderr = new StringWriter();

        Assert.That(RefusalReport.IsRefusal(error!), Is.True);
        RefusalReport.Report(error!, stderr);
        Assert.That(stderr.ToString(), Does.Contain("has no dataset.json"));
    }

    private sealed class BrokenSet : IEvalSet
    {
        public string Identity => "broken";
        public IReadOnlyList<IEval> Evals { get; } = [new BrokenEval()];
    }

    /// <summary>An eval whose dataset folder has no manifest: its scenarios can't load, so building it refuses.</summary>
    private sealed class BrokenEval : IEval
    {
        public string Name => "broken";
        public string? Identity => "broken";
        public int Version => 1;
        public string Proves => "A dataset that doesn't load.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
            DatasetLoader.Load(Path.Combine(Path.GetTempPath(), "louis-agent-no-such-dataset"), [new NoErrorGrader()]).Scenarios;

        public IReadOnlyList<IGrader> Graders { get; } = [new NoErrorGrader()];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotImplementedException();

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }
}
