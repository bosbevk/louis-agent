using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the sessions API's JSON, which every client reads and which must not change: a session's names on open and
/// on read, and the exact answers to a delete and a cancel. A session's id and times change from run to run, so those
/// answers are compared with the id put in.
/// </summary>
public class SessionReplyJsonTests
{
    private const string CreatedNames = "session_id,created_at,toolsets,depth,role,llm,tool,task,run,service,parent";

    private const string SessionNames = "session_id,created_at,last_activity,busy,toolsets,depth,role,llm,tool,tools,"
                                        + "task,run,service,sub_sessions,usage";

    /// <summary>Opening a session answers 201 with what it opened with.</summary>
    [Test]
    public async Task OpeningASession_KeepsItsNames()
    {
        await using var app = await StartAsync();

        using var response = await OpenSessionAsync(app);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(Names(json.RootElement), Is.EqualTo(CreatedNames));
    }

    /// <summary>Reading a session shows it now, with its tools, its open sub-sessions and its usage.</summary>
    [Test]
    public async Task ReadingASession_KeepsItsNames()
    {
        await using var app = await StartAsync();
        string id = await OpenAsync(app);

        using var json = JsonDocument.Parse(await app.GetTestClient().GetStringAsync($"/v1/sessions/{id}"));

        Assert.That(Names(json.RootElement), Is.EqualTo(SessionNames));
    }

    /// <summary>A cancel says whether a turn was cancelled, and a delete that the session is gone.</summary>
    [Test]
    public async Task CancellingAndDeleting_AreWrittenAsBefore()
    {
        await using var app = await StartAsync();
        string id = await OpenAsync(app);

        using var cancel = await app.GetTestClient().PostAsync($"/v1/sessions/{id}/cancel", null);
        using var delete = await app.GetTestClient().DeleteAsync($"/v1/sessions/{id}");

        Assert.That(await cancel.Content.ReadAsStringAsync(),
            Is.EqualTo($$"""{"session_id":"{{id}}","cancelled":false}"""));
        Assert.That(await delete.Content.ReadAsStringAsync(),
            Is.EqualTo($$"""{"session_id":"{{id}}","deleted":true}"""));
    }

    /// <summary>Starts the API with no key.</summary>
    private static async Task<WebApplication> StartAsync()
    {
        var app = ApiTestHost.Build();
        await app.StartAsync();
        return app;
    }

    /// <summary>Opens a session and hands back its id.</summary>
    private static async Task<string> OpenAsync(WebApplication app)
    {
        using var response = await OpenSessionAsync(app);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>
    /// Asks the API to open a session with the skills toolset, sending the API's own request record (it stays in the
    /// API until a change moves it to the contract).
    /// </summary>
    private static Task<HttpResponseMessage> OpenSessionAsync(WebApplication app) =>
        app.GetTestClient().PostAsJsonAsync("/v1/sessions",
            new CreateSessionRequest(null, null, null, [CoreToolset.Skills.Wire()]), SessionJson.SnakeCase);

    /// <summary>The object's names in the order they were written.</summary>
    private static string Names(JsonElement element) =>
        string.Join(",", element.EnumerateObject().Select(p => p.Name));
}
