using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;
using LouisAgent.Packs.Triage.Tools;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Triage.Tests.Tools;

/// <summary>
/// The run read back and a case raised (F106.3.1): a run reads as one line per step with how it ended; raising
/// writes the case where people read and records the step; the refusals say why.
/// </summary>
public class RunToolsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private string _root = null!;
    private FileTriageRecorder _recorder = null!;
    private RunTools _tools = null!;

    /// <summary>A run with two steps, and the tools over it with a people folder of their own.</summary>
    [SetUp]
    public async Task OpenARun()
    {
        _root = Path.Combine(Path.GetTempPath(), $"triage-run-tools-{Guid.NewGuid():N}");
        _recorder = new FileTriageRecorder(Path.Combine(_root, "runs"));
        await _recorder.SaveAsync(new TriageStep("run-1", 1, TriageAgent.Code, TriageStepKind.Collected,
            "Error abc123 read from the feed.", "", Now));
        await _recorder.SaveAsync(new TriageStep("run-1", 2, TriageAgent.Sorter, TriageStepKind.Sorted,
            "The sorter sent it to the code department.", "The stack is in OrderApi.", Now.AddSeconds(30)));
        _tools = new RunTools(_recorder, Path.Combine(_root, "people"), new FakeTimeProvider(Now.AddMinutes(1)));
    }

    [TearDown]
    public void RemoveTheFolder() => Directory.Delete(_root, recursive: true);

    /// <summary>A run reads as one line per step, and says how it ended once it has.</summary>
    [Test]
    public async Task ARun_ReadsAsOneLinePerStep_AndSaysHowItEnded()
    {
        string going = await _tools.ReadTriageRun("run-1");
        await _recorder.SaveAsync(new TriageStep("run-1", 3, TriageAgent.Code, TriageStepKind.Finished,
            "Nothing to do.", "", Now.AddMinutes(2), TriageOutcome.Ignored));
        string ended = await _tools.ReadTriageRun("run-1");

        Assert.Multiple(() =>
        {
            Assert.That(going, Does.StartWith("Run run-1: 2 steps so far.")
                .And.Contain("1. code (collected, 12:00:00): Error abc123 read from the feed.")
                .And.Contain("2. sorter (sorted, 12:00:30): The sorter sent it to the code department. The stack is in OrderApi."));
            Assert.That(ended, Does.StartWith("Run run-1: 3 steps so far, ended ignored."));
        });
    }

    /// <summary>Raising writes the case with the run so far where people read, and records the step.</summary>
    [Test]
    public async Task RaisingACase_WritesItForPeople_AndRecordsTheStep()
    {
        string said = await _tools.RaiseToPerson("run-1", "The refund path double-charges.", "It touches money.");

        string path = Path.Combine(_root, "people", "run-1.md");
        var run = await _recorder.ReadAsync("run-1");

        Assert.Multiple(() =>
        {
            Assert.That(said, Does.StartWith("Raised run run-1 to a person").And.Contain("step 3"));
            Assert.That(File.ReadAllText(path), Does.StartWith("# Case run-1").And.Contain("Raised 2026-11-08T12:01:00Z.")
                .And.Contain("The refund path double-charges.").And.Contain("**Why a person:** It touches money.")
                .And.Contain("- 2. sorter"));
            Assert.That(run!.Steps[2].Kind, Is.EqualTo(TriageStepKind.Raised));
            Assert.That(run.Steps[2].Agent, Is.EqualTo(TriageAgent.Manager));
            Assert.That(run.Steps[2].Why, Is.EqualTo("It touches money."));
        });
    }

    /// <summary>An unknown run, a bad id and no reason are each refused, and nothing is written.</summary>
    [Test]
    public async Task TheRefusals_SayWhy_AndWriteNothing()
    {
        Assert.Multiple(async () =>
        {
            Assert.That(await _tools.ReadTriageRun("run-9"), Does.Contain("no run 'run-9'"));
            Assert.That(await _tools.ReadTriageRun("../x"), Does.Contain("not a run id"));
            Assert.That(await _tools.RaiseToPerson("run-1", "a case", " "), Does.Contain("needs a reason"));
            Assert.That(await _tools.RaiseToPerson("run-9", "a case", "why"), Does.Contain("no run 'run-9'"));
        });
        Assert.That(Directory.Exists(Path.Combine(_root, "people")), Is.False);
    }
}
