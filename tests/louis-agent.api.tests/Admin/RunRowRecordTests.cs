using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Testing;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// The run rows became contract records: each writes the same bytes the anonymous row it replaces wrote, so the pages
/// read them as before. The old rows are written as Results.Json wrote them (the web defaults), the records with the
/// admin options.
/// </summary>
public class RunRowRecordTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A stored run with everything a run can carry, one from before step 2a, and one that cost nothing known.
    /// </summary>
    private static IEnumerable<BenchmarkDocuments.Run> StoredRuns()
    {
        yield return new BenchmarkDocuments.Run("run_1", SampleEval.Beta, 2, BenchmarkRunStatus.Done, At,
            At.AddMinutes(5), 0, Total: new BenchmarkDocuments.Total(1.25m, "USD", 1200, 4), StoreError: "late",
            Eval: SampleEval.Beta, EvalVersion: 3, Commit: "abc1234",
            Graders:
            [
                new("no-error", 1, BenchmarkGraderKind.FixedRule),
                new("meal.human", 1, BenchmarkGraderKind.Human, "Is this a meal?")
            ],
            Models: [new(SampleMode.Beta, BenchmarkModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")],
            RescoreOf: "run_0");
        yield return new BenchmarkDocuments.Run("run_2", SampleEval.Beta, 1, BenchmarkRunStatus.Running, At);
        yield return new BenchmarkDocuments.Run("run_3", SampleEval.Beta, 1, BenchmarkRunStatus.Failed, At,
            Total: new BenchmarkDocuments.Total(null, "USD", 0, 0));
    }

    /// <summary>A stored run's record writes what the anonymous row wrote.</summary>
    [TestCaseSource(nameof(StoredRuns))]
    public void AStoredRunsRecord_WritesWhatTheOldRowWrote(BenchmarkDocuments.Run run)
    {
        string old = JsonSerializer.Serialize(AdminRows.RunRow(run), Web);

        string now = JsonSerializer.Serialize(StoredRunRow.Of(run), AdminJson.SnakeCase);

        Assert.That(now, Is.EqualTo(old));
    }

    /// <summary>A live run, and a judge check, with and without what its recording kept.</summary>
    private static IEnumerable<TestCaseData> LiveRuns()
    {
        yield return new TestCaseData(new BenchmarkRun("live_1", SampleEval.Beta, 2), null);
        yield return new TestCaseData(new BenchmarkRun("live_2", SampleEval.Beta, 1),
            new BenchmarkRecorder.Result(false, "the store said no"));
        yield return new TestCaseData(
            new BenchmarkRun("live_3", SampleEval.Beta, 1, calibration: new BenchmarkCalibration("run_1", "meal")),
            new BenchmarkRecorder.Result(true, null));
    }

    /// <summary>A live run's record writes what the anonymous row wrote.</summary>
    [TestCaseSource(nameof(LiveRuns))]
    public void ALiveRunsRecord_WritesWhatTheOldRowWrote(BenchmarkRun run, BenchmarkRecorder.Result? stored)
    {
        string old = JsonSerializer.Serialize(AdminRows.Row(run, stored), Web);

        string now = JsonSerializer.Serialize(LiveRunRow.Of(run, stored), AdminJson.SnakeCase);

        Assert.That(now, Is.EqualTo(old));
    }
}
