using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Testing;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// The live run's row became a contract record: it writes the same bytes the anonymous row it replaces wrote, so the
/// pages read it as before. The old row is written as Results.Json wrote it (the web defaults), the record with the
/// admin options. The stored run's row is pinned by the stored run's golden and <c>AdminRowsTests</c>.
/// </summary>
public class RunRowRecordTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

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
