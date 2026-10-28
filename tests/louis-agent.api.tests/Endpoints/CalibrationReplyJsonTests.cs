using System.Net;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the exact JSON of a run's judge checks, so the portal's agreement card reads them however the endpoint that
/// sends them is shaped.
/// </summary>
public class CalibrationReplyJsonTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>When the run and its judge check happened.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A report comes back with its counts per class and each disagreement, under "calibrations".</summary>
    [Test]
    public async Task TheReportsReply_IsWrittenAsBefore()
    {
        // A finished run, and one judge check of it with one disagreement.
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("run_1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, At));
        var calibrations = new InMemoryCalibrationStore();
        await calibrations.AddAsync(new CalibrationRecord("run_1", "meal", "claude-haiku-5-5", At, 7, 8, 6, true,
            new CalibrationClassCounts(5, 1, 0), new CalibrationClassCounts(0, 2, 0),
            [new CalibrationDisagreement("bobotie", SampleMode.Beta, 1, "pass", "fail", "No ingredients.")]));

        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store,
            reviews: new InMemoryHumanReviewStore(), calibrations: calibrations);
        await app.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/admin/benchmarks/runs/run_1/calibrations");
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(OneLine(
            """
            {"calibrations":[{"run":"run_1","grader":"meal","judge_model":"claude-haiku-5-5",
            "at":"2026-10-25T09:00:00+00:00","agreed":7,"total":8,"bar":6,"passes":true,
            "person_pass":{"judge_pass":5,"judge_fail":1,"judge_unknown":0},
            "person_fail":{"judge_pass":0,"judge_fail":2,"judge_unknown":0},
            "disagreements":[{"scenario":"bobotie","mode":"mode-beta","repeat":1,"label":"pass",
            "judge":"fail","reason":"No ingredients."}]}]}
            """)));
    }

    /// <summary>The expected JSON, written over several lines to read easily, as the one line the API sends.</summary>
    private static string OneLine(string json) => json.ReplaceLineEndings("");
}
