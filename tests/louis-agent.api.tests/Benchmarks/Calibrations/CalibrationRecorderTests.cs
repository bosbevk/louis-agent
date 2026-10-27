using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Calibrations;

// F18 step 6a.5b: a judge check runs as the eval runner's child process, like a re-score, and the recorder listens to its lines as the benchmark recorder does: when the run ends well it
// keeps the one `@{"event":"calibration",…}` line as a report in the calibration store, for the run whose labels were checked. A run that fails, or prints no such line, stores nothing and
// says why, so a half-run never looks like a verdict. Written first: none of it existed.
public class CalibrationRecorderTests
{
    private static readonly string Line = SampleEval.Fill(SampleMode.Fill(SampleScenario.Fill("""
        @{"event":"calibration","eval":"<EvalBeta>","grader":"meal-for-people","judge_model":"qwen3:4b","agreed":2,"total":3,"bar":90,"passes":false,"person_pass":{"judge_pass":1,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":1,"judge_fail":1,"judge_unknown":0},"disagreements":[{"scenario":"<ScenarioBeta>","mode":"<ModeBeta>","repeat":3,"run":"76667d39dcdb","label":"fail","judge":"pass","reason":"a meal"}]}
        """)));

    private static BenchmarkRun Calibration(string id = "cal123456789") =>
        new(id, SampleEval.Beta, 1, calibration: new BenchmarkCalibration("76667d39dcdb", "meal-for-people"),
            input: "{}\n");

    [Test]
    public async Task ARunThatEndsWell_StoresTheReportItsLineCarries()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = Calibration();
        recorder.Attach(run);

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
                { new CalibrationDisagreement(SampleScenario.Beta, SampleMode.Beta, 3, "fail", "pass", "a meal") }));
        Assert.That(report.At, Is.EqualTo(run.EndedAt));
    }

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

    [Test]
    public async Task ARunThatIsNotACalibration_IsNotRecorded()
    {
        var store = new InMemoryCalibrationStore();
        var recorder = new CalibrationRecorder(store);
        var run = new BenchmarkRun("run123456789", SampleEval.Beta, 1);
        recorder.Attach(run);

        run.Add(Line);
        run.Complete(0);
        var result = await recorder.RecordingOf(run.Id)!;

        Assert.That(result.Stored, Is.False);
        Assert.That(recorder.RecordingOf("unknown"), Is.Null);
    }

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
