using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The API's route table is pinned (CL20): every route, with who may call it, is compared with a committed golden
/// file, so a change to the API, or to who may call a route, shows as a diff of ApiRoutesSnapshot.json. To change it:
/// set LOUIS_UPDATE_GOLDEN=1, run the test, review the file's diff, commit it.
/// </summary>
/// <remarks>
/// The table is built through ApiTestHost, with the same AddApi and MapApi as ApiStartup; the second test keeps that
/// true, since neither ApiStartup nor a demo's program may map a route of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API's routes under /v1</seealso>
public partial class ApiRoutesSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(ApiTestHost.RepoRoot, "demos/puls/tests/louis-agent.api.tests", "Endpoints", "Routing",
            "ApiRoutesSnapshot.json");

    /// <summary>
    /// The framework's own mapping calls. The last two (F142) hand a host the <c>/v1</c> group for routes of its own,
    /// which that host's tests hold, as louis-agent.dev's hold its CV's.
    /// </summary>
    private static readonly string[] AllowedInProgram =
    [
        "MapStaticAssets", "MapLouisAgentSite", "MapApi", "MapLouisAgentApi", "MapAdmin", "MapLouisAgentAccounts",
        "MapLouisAgentEvals", "MapHostGroup", "MapLouisAgentHostGroup"
    ];

    /// <summary>Every route as "METHOD /pattern (Access)", sorted, as the golden file holds it.</summary>
    private static string Snapshot()
    {
        using var app = ApiTestHost.Build(evals: false);
        var routes = ApiTestHost.Routes(app)
            .Select(endpoint => $"{ApiTestHost.Line(endpoint)} ({ApiTestHost.Access(endpoint)})")
            .Order(StringComparer.Ordinal).ToList();
        return JsonSerializer.Serialize(routes, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>The routes are the ones the golden file says, or the diff shows what changed.</summary>
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
            "The API's routes, or who may call them, changed. "
            + "If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    /// <summary>A program maps only the framework's own calls, so the table above is the one that runs.</summary>
    [TestCase("src/hosts/louis-agent.api/ApiStartup.cs")]
    [TestCase("demos/puls/hosts/louis-agent.puls-api/Program.cs")]
    [TestCase("demos/louis-agent/hosts/louis-agent.coding-api/Program.cs")]
    [TestCase("demos/starter/hosts/louis-agent.starter-api/Program.cs")]
    public void TheStartUp_MapsNoRouteOfItsOwn(string file)
    {
        // The tests' table is the one that runs only while nothing adds a route beside ApiMap's and the admin portal's.
        string source = File.ReadAllText(Path.Combine(ApiTestHost.RepoRoot, file));
        var calls = MapCall().Matches(source).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal)
            .ToList();

        Assert.That(calls.Except(AllowedInProgram), Is.Empty,
            $"{file} maps a route of its own: put it in ApiMap, so the route tests see it (CL20, ADR 0039).");
    }

    /// <summary>
    /// A call whose name starts with Map: matches <c>app.MapGet(</c>, not <c>MapGet</c> without a dot.
    /// </summary>
    [GeneratedRegex(@"\.(Map\w*)\(")]
    private static partial Regex MapCall();
}
