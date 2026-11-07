using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

// CL20 steps 1 and 2 (ADR 0039): the API's route table is pinned. Every route, as "METHOD /pattern (Access)", sorted, is compared
// with a committed golden file, so a change to the API, or to who may call a route, shows as a diff of ApiRoutesSnapshot.json. The
// table is built through ApiTestHost, which uses the same AddApi and MapApi as ApiStartup; the second test is what keeps that
// true: neither ApiStartup nor a demo's program may map a route of its own. To change the golden: set LOUIS_UPDATE_GOLDEN=1, run
// the test, review the file's diff, commit it.
public partial class ApiRoutesSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(ApiTestHost.RepoRoot, "demos/puls/tests/louis-agent.api.tests", "Endpoints", "Routing",
            "ApiRoutesSnapshot.json");

    private static readonly string[] AllowedInProgram =
        ["MapStaticAssets", "MapFallbackToFile", "MapApi", "MapLouisAgentApi", "MapAdmin", "MapLouisAgentAccounts"];

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
            "The API's routes, or who may call them, changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [TestCase("src/hosts/louis-agent.api/ApiStartup.cs")]
    [TestCase("demos/puls/hosts/louis-agent.puls-api/Program.cs")]
    [TestCase("demos/louis-agent/hosts/louis-agent.coding-api/Program.cs")]
    public void TheStartUp_MapsNoRouteOfItsOwn(string file)
    {
        // The tests' table is the one that runs only while nothing adds a route beside ApiMap's and the admin portal's.
        string source = File.ReadAllText(Path.Combine(ApiTestHost.RepoRoot, file));
        var calls = MapCall().Matches(source).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal)
            .ToList();

        Assert.That(calls.Except(AllowedInProgram), Is.Empty,
            $"{file} maps a route of its own: put it in ApiMap, so the route tests see it (CL20, ADR 0039).");
    }

    [GeneratedRegex(@"\.(Map\w*)\(")]
    private static partial Regex MapCall();
}
