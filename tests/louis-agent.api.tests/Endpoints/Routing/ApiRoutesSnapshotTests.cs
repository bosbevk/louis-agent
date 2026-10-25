using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

// CL20 step 1 (ADR 0039): the API's route table is pinned. Every route, as "METHOD /pattern", sorted, is compared with a committed
// golden file, so a change to the API shows as a diff of ApiRoutesSnapshot.json. The table is built through ApiTestHost, which uses
// the same AddApi and MapApi as Program.cs; the second test is what keeps that true: Program.cs may map no route of its own. To
// change the golden: set LOUIS_UPDATE_GOLDEN=1, run the test, review the file's diff, commit it.
public partial class ApiRoutesSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Endpoints", "Routing", "ApiRoutesSnapshot.json");

    private static readonly string[] AllowedInProgram = ["MapStaticAssets", "MapFallbackToFile", "MapApi", "MapAdmin"];

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        string method = methods is { Count: > 0 } ? string.Join(",", methods) : "ANY";
        return $"{method} {Pattern(endpoint.RoutePattern)}";
    }

    /// <summary>The pattern's text with its group prefix: <c>/admin/benchmarks</c>, built from the segments when the raw text lacks it.</summary>
    private static string Pattern(RoutePattern pattern)
    {
        string text = pattern.RawText ?? "";
        if (text.StartsWith('/')) return text;
        return "/" + string.Join("/", pattern.PathSegments.Select(s => string.Concat(s.Parts.Select(p => p switch
        {
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternParameterPart parameter => $"{{{(parameter.IsCatchAll ? "**" : "")}{parameter.Name}}}",
            _ => "",
        }))));
    }

    private static string Snapshot()
    {
        using var app = ApiTestHost.Build();
        var routes = ApiTestHost.Routes(app).Select(Describe).Order(StringComparer.Ordinal).ToList();
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
            "The API's routes changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [Test]
    public void Program_MapsNoRouteOfItsOwn()
    {
        // The tests' table is the one that runs only while Program.cs adds no route beside ApiMap's and the admin portal's.
        string program = File.ReadAllText(Path.Combine(ApiTestHost.RepoRoot, "src", "louis-agent.api", "Program.cs"));
        var calls = MapCall().Matches(program).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal).ToList();

        Assert.That(calls.Except(AllowedInProgram), Is.Empty,
            "Program.cs maps a route of its own: put it in ApiMap, so the route tests see it (CL20, ADR 0039).");
    }

    [GeneratedRegex(@"\.(Map\w*)\(")]
    private static partial Regex MapCall();
}
