using System.Text.Json;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Testing;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// Pins the live run's row as the pages read it: its names in their order, and the values that don't depend on the
/// clock. The stored run's row is pinned by the stored run's golden and <c>AdminRowsTests</c>.
/// </summary>
public class RunRowRecordTests
{
    private const string Names =
        "id,check,repeat,status,exit_code,started_at,ended_at,lines,stored,store_error,calibration_of,grader,last_line";

    /// <summary>A run that hasn't finished: no exit code, no end, nothing stored yet, and no judge check.</summary>
    [Test]
    public void ALiveRun_KeepsItsNames_AndSaysWhatItIs()
    {
        var row = Json(LiveRunRow.Of(new BenchmarkRun("live_1", SampleEval.Beta, 2)));

        Assert.That(string.Join(",", row.EnumerateObject().Select(p => p.Name)), Is.EqualTo(Names));
        Assert.That(row.GetProperty("status").GetString(), Is.EqualTo("running"));
        Assert.That(row.GetProperty("repeat").GetInt32(), Is.EqualTo(2));
        Assert.That(row.GetProperty("stored").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("calibration_of").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    /// <summary>A judge check names the run and the judge it checks, and what its recording kept.</summary>
    [Test]
    public void AJudgeCheck_NamesItsRunAndJudge()
    {
        var run = new BenchmarkRun("live_3", SampleEval.Beta, 1,
            calibration: new BenchmarkCalibration("run_1", "meal"));

        var row = Json(LiveRunRow.Of(run, new BenchmarkRecorder.Result(false, "the store said no")));

        Assert.That(row.GetProperty("calibration_of").GetString(), Is.EqualTo("run_1"));
        Assert.That(row.GetProperty("grader").GetString(), Is.EqualTo("meal"));
        Assert.That(row.GetProperty("stored").GetBoolean(), Is.False);
        Assert.That(row.GetProperty("store_error").GetString(), Is.EqualTo("the store said no"));
    }

    /// <summary>A row as the endpoints write it.</summary>
    private static JsonElement Json(LiveRunRow row) => JsonSerializer.SerializeToElement(row, AdminJson.SnakeCase);
}
