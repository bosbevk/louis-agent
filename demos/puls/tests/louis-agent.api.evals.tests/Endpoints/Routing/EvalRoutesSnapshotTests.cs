using System.Text.Encodings.Web;
using System.Text.Json;

namespace LouisAgent.Api.Evals.Tests.Endpoints.Routing;

/// <summary>
/// The studio's route table is pinned (CL57, as the API's is by ADR 0039): every route the API has with the studio
/// mapped, as "METHOD /pattern (Access)", sorted, against a committed golden file, so a change to the studio's routes,
/// or to who may call one, shows as a diff of EvalRoutesSnapshot.json. To change the golden: set
/// LOUIS_UPDATE_GOLDEN=1, run the test, review the file's diff, commit it.
/// </summary>
public class EvalRoutesSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(ApiTestHost.RepoRoot, "demos/puls/tests/louis-agent.api.evals.tests", "Endpoints", "Routing",
            "EvalRoutesSnapshot.json");

    private static string Snapshot()
    {
        using var app = ApiTestHost.Build();
        var routes = ApiTestHost.Routes(app)
            .Select(endpoint => $"{ApiTestHost.Line(endpoint)} ({ApiTestHost.Access(endpoint)})")
            .Order(StringComparer.Ordinal).ToList();
        return JsonSerializer.Serialize(routes, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }).ReplaceLineEndings("\n") + "\n";
    }

    [Test]
    public void TheRoutes_MatchTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")),
            "The studio's routes, or who may call them, changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }
}
