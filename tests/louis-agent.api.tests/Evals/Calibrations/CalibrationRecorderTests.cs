using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Runs;

namespace LouisAgent.Api.Tests.Evals.Calibrations;

/// <summary>
/// Checking a judge against people's labels runs the eval runner as a child process, and the recorder listens to its
/// lines. When the check ends well, its one <c>@{"event":"calibration",…}</c> line is kept as a report for the run
/// whose labels were checked. A check that fails, or never prints that line, stores nothing and says why, so a
/// half-finished check never looks like a verdict.
/// </summary>
public class CalibrationRecorderTests
{
    /// <summary>The report a finished check prints: 2 of 3 agreed, under a bar of 90, with one disagreement.</summary>
    private static readonly string Line = SampleEval.Fill(SampleMode.Fill(SampleScenario.Fill("""
        @{"event":"calibration","eval":"<EvalBeta>","grader":"meal-for-people","judge_model":"qwen3:4b","agreed":2,"total":3,"bar":90,"passes":false,"person_pass":{"judge_pass":1,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":1,"judge_fail":1,"judge_unknown":0},"disagreements":[{"scenario":"<ScenarioBeta>","mode":"<ModeBeta>","repeat":3,"run":"76667d39dcdb","label":"fail","judge":"pass","reason":"a meal"}]}
        """)));

    /// <summary>A judge check of one stored run's labels for the meal-for-people grader.</summary>
    private static RunningEval Calibration(string id = "cal123456789") =>
        new(id, SampleEval.Beta, 1, calibration: new EvalCalibration("76667d39dcdb", "meal-for-people"),
            input: "{}\n");

    /// <summary>A check that ends well stores everything its report line says, dated when the check ended.</summary>
    [Test]
    public async Task ARunThatEndsWell_StoresTheReportItsLineCarries()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = Calibration();
        recorder.Attach(run);

        // The runner's readable lines first, then the report line, then a clean exit.
        run.Add("Agreement: 2 of 3");
        run.Add("Labelled pass: judge pass 1, fail 0, can't tell 0 (1 of 1, 100%), few labels");
        run.Add(Line);
        run.Complete(0);
        var result = await recorder.RecordingOf(run.Id)!;

        Assert.That(result.Stored, Is.True, result.Error);
        var report = (await store.OfRunAsync("76667d39dcdb")).Single();
        Assert.That(
            (report.Run, report.Grader, report.JudgeModel, report.Agreed, report.Total, report.Bar, report.Passes),
            Is.EqualTo(("76667d39dcdb", "meal-for-people", "qwen3:4b", 2, 3, 90, false)));
        Assert.That(report.PersonPass, Is.EqualTo(new CalibrationClassCounts(1, 0, 0)));
        Assert.That(report.PersonFail, Is.EqualTo(new CalibrationClassCounts(1, 1, 0)));
        Assert.That(report.Disagreements,
            Is.EqualTo(new[]
            {
                new CalibrationDisagreement(SampleScenario.Beta, SampleMode.Beta, 3, "fail", "pass", "a meal")
            }));
        Assert.That(report.At, Is.EqualTo(run.EndedAt));
    }

    /// <summary>A check that exits with an error stores nothing, and the result carries the runner's reason.</summary>
    [Test]
    public async Task ARunThatFails_StoresNothing_AndSaysWhy()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = Calibration();
        recorder.Attach(run);

        run.Add("Eval '" + SampleEval.Beta + "' has no grader 'meal-for-people'. Its graders are no-error.");
        run.Complete(1);
        var result = await recorder.RecordingOf(run.Id)!;

        Assert.That(result.Stored, Is.False);
        Assert.That(result.Error, Does.Contain("has no grader").And.Contain("1"));
        Assert.That(await store.OfRunAsync("76667d39dcdb"), Is.Empty);
    }

    /// <summary>A check that ends cleanly but never prints its report stores nothing, and says so.</summary>
    [Test]
    public async Task ARunWithNoReportLine_StoresNothing()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = Calibration();
        recorder.Attach(run);

        run.Add("Agreement: 2 of 3");
        run.Complete(0);
        var result = await recorder.RecordingOf(run.Id)!;

        Assert.That(result.Stored, Is.False);
        Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        Assert.That(await store.OfRunAsync("76667d39dcdb"), Is.Empty);
    }

    /// <summary>An ordinary eval run isn't a judge check, so even a report line in it isn't stored.</summary>
    [Test]
    public async Task ARunThatIsNotACalibration_IsNotRecorded()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = new RunningEval("run123456789", SampleEval.Beta, 1);
        recorder.Attach(run);

        run.Add(Line);
        run.Complete(0);
        var result = await recorder.RecordingOf(run.Id)!;

        Assert.That(result.Stored, Is.False);
        Assert.That(recorder.RecordingOf("unknown"), Is.Null);
    }

    /// <summary>The reader picks up a report line and ignores every other line, broken JSON included.</summary>
    [Test]
    public void TheReader_ReadsOnlyACalibrationLine()
    {
        var at = new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

        Assert.That(CalibrationLineReader.Read(Line, "76667d39dcdb", at)?.Agreed, Is.EqualTo(2));
        Assert.That(
            CalibrationLineReader.Read(SampleMode.Fill("""@{"event":"mode","mode":"<ModeBeta>","repeat":1}"""),
                "76667d39dcdb", at), Is.Null);
        Assert.That(CalibrationLineReader.Read("Agreement: 2 of 3", "76667d39dcdb", at), Is.Null);
        Assert.That(CalibrationLineReader.Read("@{not json", "76667d39dcdb", at), Is.Null);
    }
}
