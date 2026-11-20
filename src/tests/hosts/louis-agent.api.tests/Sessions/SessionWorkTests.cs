using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Replies.Work;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A session's work over the API (F127.4.2): <c>GET /v1/sessions/{id}/work</c> gives what the session was asked, what
/// it said, each tool it called with what it sent and what came back, without its thinking, so an agent that asked it
/// can read the turns someone else had with it too.
/// </summary>
public class SessionWorkTests
{
    /// <summary>A model that reads a file that isn't there, then says so.</summary>
    private static FakeChatClient ReadsAMissingFile() => new(
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextReasoningContent("Let me look."),
            new FunctionCallContent("call_1", "ReadWorkspaceFile",
                new Dictionary<string, object?> { ["relativePath"] = "missing.txt" })
        ])),
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "There is no such file.")));

    /// <summary>The steps of a turn, in order: asked, called, returned (failed), said; the thinking left out.</summary>
    [Test]
    public async Task ASessionsWork_IsItsStepsInOrder()
    {
        await using var app = ApiTestHost.Build(model: ReadsAMissingFile());
        await app.StartAsync();
        var client = app.GetTestClient();
        string id = await OpenSessionAsync(client);
        await SendAsync(client, id, "Read missing.txt please.");

        var work = await client.GetFromJsonAsync<SessionWorkReply>($"/v1/sessions/{id}/work", SessionJson.SnakeCase);

        Assert.Multiple(() =>
        {
            Assert.That(work!.SessionId, Is.EqualTo(id));
            Assert.That(work.Steps.Select(step => step.Kind),
                Is.EqualTo(new[]
                {
                    WorkStepKind.Asked, WorkStepKind.Called, WorkStepKind.Returned, WorkStepKind.Said
                }));
            Assert.That(work.Steps[0].Text, Does.Contain("Read missing.txt please."));
            Assert.That(work.Steps[1].Tool, Is.EqualTo("ReadWorkspaceFile"));
            Assert.That(work.Steps[1].Text, Does.Contain("missing.txt"));
            Assert.That(work.Steps[2].Failed, Is.True);
            Assert.That(work.Steps[3].Text, Is.EqualTo("There is no such file."));
            Assert.That(work.Steps.Select(step => step.Text), Has.None.Contains("Let me look."));
        });
    }

    /// <summary>A step's kind is written by its wire name, never a number.</summary>
    [Test]
    public void AStepsKind_IsWrittenByItsWireName()
    {
        string json = JsonSerializer.Serialize(new WorkStep(WorkStepKind.Returned, "ok", "GetToday", false),
            SessionJson.SnakeCase);

        Assert.That(json, Does.Contain($"\"kind\":\"{WorkStepKind.Returned.Wire()}\""));
    }

    /// <summary>An unknown session is a 404.</summary>
    [Test]
    public async Task AnUnknownSessionsWork_IsNotFound()
    {
        await using var app = ApiTestHost.Build(model: ReadsAMissingFile());
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync("/v1/sessions/sess_none/work");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Opens a session with the file tools and hands back its id.</summary>
    private static async Task<string> OpenSessionAsync(HttpClient client)
    {
        using var opened =
            await client.PostAsJsonAsync("/v1/sessions", new { toolsets = new[] { TestToolset.Files.Wire() } });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>Sends a message and reads its whole answer, so the turn is over.</summary>
    private static async Task SendAsync(HttpClient client, string id, string message)
    {
        using var response = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages", new { message });
        await response.Content.ReadAsStringAsync();
    }
}
