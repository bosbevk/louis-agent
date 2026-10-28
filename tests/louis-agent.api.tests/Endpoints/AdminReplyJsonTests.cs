using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the exact JSON the admin routes in AdminEndpoints send, so the portal keeps reading them however the code
/// that builds them is shaped. The wire is snake_case as written, and a property's place counts: the replies are
/// compared as text.
/// </summary>
public class AdminReplyJsonTests
{
    private const string AdminKey = "admin-secret";
    private const string ManualPath = "evals/louis-agent.evals.runner/Manuals/stub.md";

    /// <summary>The page's first read lists each check with its title and whether it has a manual.</summary>
    [Test]
    public async Task TheListReply_NamesTheChecks_AndTheStore()
    {
        var catalog = new BenchmarkCatalog([new BenchmarkCheck("stub", "A stub", ManualPath)]);
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: new InMemoryBenchmarkStore(),
            catalog: catalog);
        await app.StartAsync();

        using var json = await GetAsync(app, "/v1/admin/benchmarks");

        Assert.That(Names(json.RootElement), Is.EqualTo("checks,store,runs"));
        Assert.That(json.RootElement.GetProperty("checks").GetRawText(),
            Is.EqualTo("""[{"id":"stub","title":"A stub","manual":true}]"""));
        Assert.That(json.RootElement.GetProperty("store").GetRawText(),
            Is.EqualTo("""{"configured":true,"error":null}"""));
        Assert.That(json.RootElement.GetProperty("runs").GetRawText(), Is.EqualTo("[]"));
    }

    /// <summary>
    /// Without a store the page's banner reads "not configured", and the reply still carries every part.
    /// </summary>
    [Test]
    public async Task TheListReply_WithoutAStore_SaysNotConfigured()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var json = await GetAsync(app, "/v1/admin/benchmarks");

        Assert.That(json.RootElement.GetProperty("store").GetRawText(),
            Is.EqualTo("""{"configured":false,"error":null}"""));
        Assert.That(json.RootElement.GetProperty("checks").GetRawText(), Is.EqualTo("[]"));
    }

    /// <summary>A check's manual comes with its id and title beside the markdown.</summary>
    [Test]
    public async Task TheManualReply_CarriesTheCheck_TheTitle_AndTheMarkdown()
    {
        var catalog = new BenchmarkCatalog([new BenchmarkCheck("stub", "A stub", ManualPath)]);
        await using var app = ApiTestHost.Build(adminKey: AdminKey, catalog: catalog);
        await app.StartAsync();

        using var json = await GetAsync(app, "/v1/admin/benchmarks/stub/manual");

        Assert.That(Names(json.RootElement), Is.EqualTo("check,title,markdown"));
        Assert.That(json.RootElement.GetProperty("check").GetString(), Is.EqualTo("stub"));
        Assert.That(json.RootElement.GetProperty("title").GetString(), Is.EqualTo("A stub"));
        Assert.That(json.RootElement.GetProperty("markdown").GetString(), Does.StartWith("# "));
    }

    /// <summary>
    /// The trend sends the check, a chart per category, and the overall lines with every point's numbers, its notes
    /// and the agents whose prompts changed.
    /// </summary>
    [Test]
    public async Task TheTrendReply_NamesEveryPointNumber()
    {
        var store = new InMemoryBenchmarkStore();
        var at = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
        var slice = new BenchmarkSlice(SampleMode.Beta, "overall", 8, 6, 2, 0, 0, ["a"], false, 0.75, 0.75, 0.75, [],
            [], []);
        await store.SaveRunAsync(new Run("r1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, at, Slices: [slice]));
        // One conversation, so the overall trend has a line with a point on it.
        var scores = JsonDocument.Parse("""{"routed_right":true}""").RootElement.Clone();
        await store.SaveConversationAsync("r1",
            new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, scores, 0, 1));
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();

        using var json = await GetAsync(app, $"/v1/admin/benchmarks/checks/{SampleEval.Beta}/trend");

        Assert.That(Names(json.RootElement), Is.EqualTo("check,categories,modes"));
        var mode = json.RootElement.GetProperty("modes")[0];
        Assert.That(Names(mode), Is.EqualTo("name,points"));
        Assert.That(Names(mode.GetProperty("points")[0]), Is.EqualTo(
            "run,at,cost,score,conversations,tokens,seconds_per_turn,cache_share,sourced,tools_failed,"
            + "cost_per_answer,sites,pass_at_k,pass_hat_k,k,notes,prompt_changed"));
    }

    /// <summary>Asks the admin route and parses what comes back; the call must have been a 200.</summary>
    private static async Task<JsonDocument> GetAsync(WebApplication app, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>The object's property names in the order they were written, joined by commas.</summary>
    private static string Names(JsonElement element) => string.Join(",", element.EnumerateObject().Select(p => p.Name));
}
