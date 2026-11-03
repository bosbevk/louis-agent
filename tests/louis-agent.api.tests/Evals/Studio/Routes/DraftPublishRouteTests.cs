using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Studio;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Evals.Studio.Routes;

/// <summary>
/// A publish the runner's check refuses says so in one clean sentence (FX32): the runner's problem ends with its own
/// full stop, and the refusal around it adds none.
/// </summary>
public class DraftPublishRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>The refusal quotes the runner's first problem once, with no doubled full stop.</summary>
    [Test]
    public async Task ARefusedPublish_QuotesTheProblem_WithOneFullStop()
    {
        var checker = new DraftChecker(_ =>
            new RunnerOutput("{\"problem\":\"scenario 'weather-paarl' has no reference transcript.\"}\n{\"ok\":false}\n",
                null));
        await using var app = ApiTestHost.Build(adminKey: AdminKey, drafts: new InMemoryEvalDraftStore(),
            checker: checker);
        await app.StartAsync();
        var client = app.GetTestClient();
        string id = await MakeADraft(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/admin/evals/drafts/{id}/publish");
        request.Headers.Add("x-api-key", AdminKey);
        using var response = await client.SendAsync(request);

        string message = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement
            .GetProperty("error").GetProperty("message").GetString()!;
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(message, Does.Contain("has no reference transcript").And.Not.Contain(".."));
    }

    /// <summary>Saves an eval draft and gives its id.</summary>
    private static async Task<string> MakeADraft(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/admin/evals/drafts")
        {
            Content = JsonContent.Create(new EvalDraftRequest(
                """{"name":"weather-eval","identity":"weather-agent"}""", null, "Louis", null))
        };
        request.Headers.Add("x-api-key", AdminKey);
        using var made = await client.SendAsync(request);
        return JsonDocument.Parse(await made.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
    }
}
