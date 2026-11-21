using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;

namespace LouisAgent.Packs.Triage.Tests.Runs;

/// <summary>
/// The file recorder (F106.3.1): a run is one JSON-lines file, a step a line as it lands; read back, the steps are
/// in order whatever order they were saved, the outcome is the finishing step's, and a run nobody saved is nothing.
/// </summary>
public class FileTriageRecorderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);

    private string _root = null!;
    private FileTriageRecorder _recorder = null!;

    [SetUp]
    public void MakeAFolder()
    {
        _root = Path.Combine(Path.GetTempPath(), $"triage-runs-{Guid.NewGuid():N}");
        _recorder = new FileTriageRecorder(_root);
    }

    [TearDown]
    public void RemoveTheFolder()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static TriageStep Step(int number, TriageStepKind kind, TriageOutcome? outcome = null) =>
        new("run-1", number, TriageAgent.Manager, kind, $"step {number}", "because", T0.AddMinutes(number), outcome);

    /// <summary>Steps saved out of order come back in order, and the outcome is the finishing step's.</summary>
    [Test]
    public async Task StepsSavedOutOfOrder_ComeBackInOrder_WithTheOutcome()
    {
        await _recorder.SaveAsync(Step(2, TriageStepKind.Decided));
        await _recorder.SaveAsync(Step(1, TriageStepKind.Collected));
        await _recorder.SaveAsync(Step(3, TriageStepKind.Finished, TriageOutcome.Ignored));

        var run = await _recorder.ReadAsync("run-1");

        Assert.That(run, Is.Not.Null);
        Assert.That(run!.Steps.Select(step => step.Number), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(run.Steps[0].Kind, Is.EqualTo(TriageStepKind.Collected));
        Assert.That(run.Outcome, Is.EqualTo(TriageOutcome.Ignored));
    }

    /// <summary>A run still going has no outcome; a run nobody saved is nothing.</summary>
    [Test]
    public async Task ARunStillGoing_HasNoOutcome_AndAnUnknownRun_IsNothing()
    {
        await _recorder.SaveAsync(Step(1, TriageStepKind.Collected));

        Assert.That((await _recorder.ReadAsync("run-1"))!.Outcome, Is.Null);
        Assert.That(await _recorder.ReadAsync("run-9"), Is.Null);
    }

    /// <summary>
    /// A step is written by its wire names, the way every record travels (ADR 0041, 0043), so another reader
    /// understands the file.
    /// </summary>
    [Test]
    public async Task AStep_IsWrittenByItsWireNames()
    {
        await _recorder.SaveAsync(Step(1, TriageStepKind.Finished, TriageOutcome.FixedOnBranch));

        string line = File.ReadAllText(Path.Combine(_root, "run-1.jsonl")).Trim();

        Assert.That(line, Does.Contain("\"agent\":\"manager\"").And.Contain("\"kind\":\"finished\"")
            .And.Contain("\"outcome\":\"fixed-on-branch\"").And.Contain("\"at\":\"2026-11-08T10:01:00+00:00\""));
    }

    /// <summary>A run id is a file name, so one that would leave the folder is refused.</summary>
    [Test]
    public void ARunId_ThatIsntAPlainToken_IsRefused()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _recorder.ReadAsync("../secrets"));
    }
}
