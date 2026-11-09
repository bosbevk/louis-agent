using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Runs.Steps;
using LouisAgent.Packs.Triage.Web.Runs;

namespace LouisAgent.Packs.Triage.Web.Tests.Runs;

/// <summary>
/// The demo reads a run the way the pack wrote it (F106.3.4): one step per line, in the pack's own JSON. The web
/// project keeps its own copy of the step, since it may not reference the pack, so these tests hold the two together.
/// </summary>
public class TriageRunFileTests
{
    private static string Recording => Path.Combine(TestPaths.RepoRoot,
        "demos/louis-agent/web/louis-agent.code-review/wwwroot/recordings/triage-run.jsonl");

    /// <summary>The recorded run reads as its steps, in order, from the error collected to the case closed.</summary>
    [Test]
    public void TheRecordedRun_ReadsAsItsSteps()
    {
        var steps = TriageRunFile.Read(File.ReadAllText(Recording));

        Assert.Multiple(() =>
        {
            Assert.That(steps.Select(step => step.Number), Is.Ordered.And.Unique);
            Assert.That(steps[0].Kind, Is.EqualTo(TriageStepKind.Collected.Wire()));
            Assert.That(steps[^1].Kind, Is.EqualTo(TriageStepKind.Finished.Wire()));
            Assert.That(steps[^1].Outcome, Is.EqualTo(TriageOutcome.FixedOnBranch.Wire()));
            Assert.That(steps.Select(step => step.Agent), Does.Contain(TriageAgent.Reviewer.Wire()));
        });
    }

    /// <summary>
    /// A step the pack writes reads back field for field, so the web's copy can't drift from the pack's.
    /// </summary>
    [Test]
    public void APacksStep_ReadsBackFieldForField()
    {
        var at = new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);
        var step = new TriageStep("run-1", 3, TriageAgent.Investigator, TriageStepKind.Investigated, "Found it.",
            "Line 18.", at, TriageOutcome.FixedOnBranch, "{\"area\":\"code\"}");

        var read = TriageRunFile.Read(JsonSerializer.Serialize(step, FileTriageRecorder.Json) + "\n\n").Single();

        Assert.That(read, Is.EqualTo(new TriageStepView("run-1", 3, TriageAgent.Investigator.Wire(),
            TriageStepKind.Investigated.Wire(), "Found it.", "Line 18.", at, TriageOutcome.FixedOnBranch.Wire())));
    }
}
