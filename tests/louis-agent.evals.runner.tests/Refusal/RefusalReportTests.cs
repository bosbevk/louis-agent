using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Refusal;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Refusal;

/// <summary>
/// When the harness refuses something while the catalogue is built, you see why on stderr and the runner exits with
/// 1, just as it does for its own refusals.
/// </summary>
public class RefusalReportTests
{
    /// <summary>A bad dataset and the runner's own complaints both count as refusals.</summary>
    [Test]
    public void TheHarnessAndTheRunnersRefusalsAreRefusals()
    {
        Assert.That(RefusalReport.IsRefusal(new InvalidDataException("a dataset")), Is.True);
        Assert.That(RefusalReport.IsRefusal(new RunnerException("an eval")), Is.True);
    }

    /// <summary>A bug is not a refusal: it still crashes loudly.</summary>
    [Test]
    public void AnyOtherErrorIsNotARefusal() =>
        Assert.That(RefusalReport.IsRefusal(new InvalidOperationException("a bug")), Is.False);

    /// <summary>A refusal prints only its reason, and the exit code is 1.</summary>
    [Test]
    public void ItWritesTheReasonAndExitsWithOne()
    {
        var stderr = new StringWriter();

        int code = RefusalReport.Report(new InvalidDataException("Dataset folder 'x' has no dataset.json."), stderr);

        Assert.That(code, Is.EqualTo(1));
        Assert.That(stderr.ToString().Trim(), Is.EqualTo("Dataset folder 'x' has no dataset.json."));
    }

    /// <summary>An eval whose dataset won't load stops the start, and says what's missing.</summary>
    [Test]
    public void ADatasetThatWontLoadIsRefusedWithItsReason()
    {
        // The dataset is read when the eval is built, which is while the catalogue is built: the same path the
        // runner's start takes.
        var error = Assert.Throws<InvalidDataException>(() => _ = new EvalCatalog([new BrokenEval()], new StubEval()));
        var stderr = new StringWriter();

        Assert.That(RefusalReport.IsRefusal(error!), Is.True);
        RefusalReport.Report(error!, stderr);
        Assert.That(stderr.ToString(), Does.Contain("has no dataset.json"));
    }

    /// <summary>
    /// An eval whose dataset folder has no manifest: its scenarios can't load, so building it refuses.
    /// </summary>
    private sealed class BrokenEval : IEval
    {
        public string Name => "broken";
        public int Version => 1;
        public string Proves => "A dataset that doesn't load.";

        /// <summary>No modes: it never gets as far as running.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
            DatasetLoader.Load(Path.Combine(Path.GetTempPath(), "louis-agent-no-such-dataset"), [new NoErrorCheck()])
                .Scenarios;

        public IReadOnlyList<ICheck> Checks { get; } = [new NoErrorCheck()];

        /// <summary>Never asked: it never runs a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }
}
