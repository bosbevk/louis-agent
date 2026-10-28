using LouisAgent.Testing;
using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// Beside the overall trend, the trend endpoint sends a chart per category, each shaped like the overall trend's
/// lines, so the portal's trend chart can draw it. A slice with too few trials is not a point. The trend's own
/// working-out is tested in CategoryTrendTests.
/// </summary>
public class CategoryTrendEndpointTests
{
    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>
    /// The trend's JSON has a chart for each category with enough trials, laid out like the overall trend, which is
    /// still there.
    /// </summary>
    [Test]
    public async Task TheTrendJson_CarriesAChartPerCategory_InTheShapeOfTheOverallTrend()
    {
        var store = new InMemoryBenchmarkStore();
        var at = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

        // A slice of six passes in eight, with the same number for each of its rates.
        BenchmarkSlice Slice(string name, double? rate, bool tooFew = false) =>
            new(SampleMode.Beta, name, 8, 6, 2, 0, 0, ["a"], tooFew, rate, rate, rate, [], [], []);

        // Two runs an hour apart; the budget category had too few trials in the first and isn't in the second.
        await store.SaveRunAsync(new Run("r1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, at,
            Slices: [Slice("overall", 0.75), Slice("budget", null, tooFew: true)]));
        await store.SaveRunAsync(new Run("r2", SampleEval.Beta, 1, BenchmarkRunStatus.Done, at.AddHours(1),
            Slices: [Slice("overall", 0.875)]));

        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        using var request =
            new HttpRequestMessage(HttpMethod.Get, $"/v1/admin/benchmarks/checks/{SampleEval.Beta}/trend");
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var categories = json.RootElement.GetProperty("categories").EnumerateArray().ToList();
        Assert.That(categories.Select(c => c.GetProperty("category").GetString()), Is.EqualTo(new[] { "overall" }),
            "budget had too few trials in its only run: no chart");
        var line = categories[0].GetProperty("modes")[0];
        Assert.That(line.GetProperty("name").GetString(), Is.EqualTo(SampleMode.Beta));
        var points = line.GetProperty("points").EnumerateArray().ToList();
        Assert.That(points.Select(p => p.GetProperty("run").GetString()), Is.EqualTo(new[] { "r1", "r2" }));
        Assert.That(points.Select(p => p.GetProperty("pass_at_k").GetDouble()), Is.EqualTo(new[] { 0.75, 0.875 }));
        Assert.That(json.RootElement.TryGetProperty("modes", out _), Is.True, "the overall trend is still there");
    }
}
