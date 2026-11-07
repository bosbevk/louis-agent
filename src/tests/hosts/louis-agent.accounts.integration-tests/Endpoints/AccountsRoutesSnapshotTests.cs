using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints;

/// <summary>
/// The account routes are pinned: every route as "METHOD /pattern (cookie|open)", sorted, against a committed golden
/// file, so a route that comes, goes or changes who may call it shows as a diff. Set <c>LOUIS_UPDATE_GOLDEN=1</c> to
/// rewrite it, review the diff, commit it. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set, since the host migrates.
/// </summary>
[Category("Integration")]
public class AccountsRoutesSnapshotTests
{
    private const string Database = "puls_accounts_test_snapshot";

    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot,
        "src/tests/hosts/louis-agent.accounts.integration-tests/Endpoints/AccountsRoutesSnapshot.json");

    [OneTimeSetUp]
    public void NeedsAServer() => Needed();

    [OneTimeTearDown]
    public Task Drop() => DropAsync(Database);

    [Test]
    public async Task TheRoutes_MatchTheGoldenFile()
    {
        await using var host = await AccountsTestHost.StartAsync(Database);
        var routes = host.App.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => $"{Methods(e)} {e.RoutePattern.RawText} ({Access(e)})")
            .Order(StringComparer.Ordinal).ToList();
        string actual = JsonSerializer.Serialize(routes, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }).ReplaceLineEndings("\n") + "\n";

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            await File.WriteAllTextAsync(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo((await File.ReadAllTextAsync(GoldenFile)).ReplaceLineEndings("\n")),
            "The account routes, or who may call them, changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    private static string Methods(RouteEndpoint endpoint) =>
        string.Join(",", endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"]);

    /// <summary>Who may call it: a route with an authorization mark needs the cookie, any other is open.</summary>
    private static string Access(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAuthorizeData>() is null ? "open" : "cookie";
}
