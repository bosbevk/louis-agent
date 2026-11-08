using System.Text.Json;
using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;
using LouisAgent.Packs.Triage.Tools;
using LouisAgent.Packs.Triage.Tools.Choices;
using LouisAgent.Packs.Triage.Tools.Results;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Triage.Tests.Tools;

/// <summary>
/// The decision tools (F106.3.1): a good call is saved as the run's next step with its typed record, which reads
/// back as the record; a value that isn't a choice, an empty reason and an unknown run are refused with their reason.
/// </summary>
public class TriageToolsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private string _root = null!;
    private FileTriageRecorder _recorder = null!;
    private TriageTools _tools = null!;

    /// <summary>A recorder with one run already open, and the tools over it on a fixed clock.</summary>
    [SetUp]
    public async Task OpenARun()
    {
        _root = Path.Combine(Path.GetTempPath(), $"triage-tools-{Guid.NewGuid():N}");
        _recorder = new FileTriageRecorder(_root);
        await _recorder.SaveAsync(new TriageStep("run-1", 1, TriageAgent.Code, TriageStepKind.Collected,
            "Error abc123 read from the feed.", "", Now));
        _tools = new TriageTools(_recorder, ["code", "ops"], new FakeTimeProvider(Now.AddMinutes(1)));
    }

    /// <summary>The sorter's ending (F106.3.2): a department it was given is saved as Sorted; another is refused with the names.</summary>
    [Test]
    public async Task ASorting_KeepsTheDepartment_AndRefusesOneNotGiven()
    {
        string said = await _tools.SortCase("run-1", "ops", "The stack is in the hosting configuration.");
        string refused = await _tools.SortCase("run-1", "marketing", "no");

        var run = await _recorder.ReadAsync("run-1");
        var sorting = JsonSerializer.Deserialize<Sorting>(run!.Steps[1].Result!, FileTriageRecorder.Json)!;

        Assert.Multiple(() =>
        {
            Assert.That(said, Does.StartWith("Recorded step 2 of run run-1"));
            Assert.That(run.Steps[1].Agent, Is.EqualTo(TriageAgent.Sorter));
            Assert.That(run.Steps[1].Kind, Is.EqualTo(TriageStepKind.Sorted));
            Assert.That(sorting, Is.EqualTo(new Sorting("ops", "The stack is in the hosting configuration.")));
            Assert.That(refused, Does.Contain("is not a department").And.Contain("code, ops"));
            Assert.That(run.Steps, Has.Count.EqualTo(2));
        });
    }

    [TearDown]
    public void RemoveTheFolder() => Directory.Delete(_root, recursive: true);

    /// <summary>An investigation is saved as step 2 with its record, which reads back typed.</summary>
    [Test]
    public async Task AnInvestigation_IsSavedAsTheNextStep_WithItsRecord()
    {
        string said = await _tools.ReportInvestigation("run-1", "OrderApi.GetOrderTotal",
            "The discount code is not in the table.", "code", "high");

        var run = await _recorder.ReadAsync("run-1");
        var step = run!.Steps[1];
        var found = JsonSerializer.Deserialize<Investigation>(step.Result!, FileTriageRecorder.Json)!;

        Assert.Multiple(() =>
        {
            Assert.That(said, Does.StartWith("Recorded step 2 of run run-1"));
            Assert.That(step.Agent, Is.EqualTo(TriageAgent.Investigator));
            Assert.That(step.Kind, Is.EqualTo(TriageStepKind.Investigated));
            Assert.That(step.Summary, Does.Contain("OrderApi.GetOrderTotal").And.Contain("discount code"));
            Assert.That(step.At, Is.EqualTo(Now.AddMinutes(1)));
            Assert.That(found,
                Is.EqualTo(new Investigation("OrderApi.GetOrderTotal", "The discount code is not in the table.",
                    DepartmentArea.Code, Confidence.High)));
        });
    }

    /// <summary>A review keeps its reasons one per line; a decision keeps its kind and reason.</summary>
    [Test]
    public async Task AReview_AndADecision_KeepTheirRecords()
    {
        await _tools.ReportReview("run-1", false, "The test for the fix is missing.\nThe branch name is wrong.");
        await _tools.DecideCase("run-1", "auto-fix", "A one-line fix the investigator is sure of.");

        var run = await _recorder.ReadAsync("run-1");
        var review = JsonSerializer.Deserialize<Review>(run!.Steps[1].Result!, FileTriageRecorder.Json)!;
        var decision = JsonSerializer.Deserialize<CaseDecision>(run.Steps[2].Result!, FileTriageRecorder.Json)!;

        Assert.Multiple(() =>
        {
            Assert.That(review.Happy, Is.False);
            Assert.That(review.Reasons,
                Is.EqualTo(new[] { "The test for the fix is missing.", "The branch name is wrong." }));
            Assert.That(run.Steps[1].Summary, Does.Contain("not happy"));
            Assert.That(decision,
                Is.EqualTo(new CaseDecision(CaseDecisionKind.AutoFix, "A one-line fix the investigator is sure of.")));
            Assert.That(run.Steps[2].Agent, Is.EqualTo(TriageAgent.Manager));
        });
    }

    /// <summary>A value that isn't a choice is refused and the choices named; nothing is saved.</summary>
    [TestCase("code", "very", "confidence", "low, medium, high")]
    [TestCase("marketing", "high", "department area", "code, data, hosting, third-party, unknown")]
    public async Task AValueThatIsntAChoice_IsRefused_WithTheChoices(string area, string confidence, string what,
        string options)
    {
        string said = await _tools.ReportInvestigation("run-1", "somewhere", "a cause", area, confidence);

        Assert.That(said, Does.Contain($"is not a {what}").And.Contain(options));
        Assert.That((await _recorder.ReadAsync("run-1"))!.Steps, Has.Count.EqualTo(1));
    }

    /// <summary>An empty reason, an unknown run and a bad run id are each refused with their reason.</summary>
    [Test]
    public async Task AnEmptyReason_AnUnknownRun_AndABadId_AreRefused()
    {
        Assert.Multiple(async () =>
        {
            Assert.That(await _tools.DecideCase("run-1", "ignore", "  "), Does.Contain("needs a reason"));
            Assert.That(await _tools.ReportReview("run-1", true, "\n"), Does.Contain("needs a reason"));
            Assert.That(await _tools.DecideCase("run-9", "ignore", "fine"), Does.Contain("no run 'run-9'"));
            Assert.That(await _tools.DecideCase("../x", "ignore", "fine"), Does.Contain("not a run id"));
        });
    }
}
