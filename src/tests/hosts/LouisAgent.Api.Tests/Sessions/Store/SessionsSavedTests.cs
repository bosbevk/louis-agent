using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Store;
using LouisAgent.Choices;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Sessions.Store;

/// <summary>
/// F43.1 step 3: the API writes its chats through the session store. Opening a chat saves how it opened; a finished
/// turn appends what it added to the history, thinking included, and its text attachments; deleting a chat forgets
/// it; an eval trial's chat is never saved; a failed turn appends only what it added.
/// </summary>
public class SessionsSavedTests
{
    /// <summary>A model that thinks, then answers in a few words.</summary>
    private static FakeChatClient Thinks() =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new TextReasoningContent("let me think") { ProtectedData = "c2lnbmF0dXJl" }, new TextContent("Done.")])));

    /// <summary>A model whose request fails.</summary>
    private static FakeChatClient Fails() => new(_ => throw new InvalidOperationException("the model is down"));

    /// <summary>An API on this model, started, with its client, its sessions and its store.</summary>
    private static async Task<(WebApplication App, HttpClient Client, AgentSessions Sessions, ISessionStore Store)>
        StartAsync(IChatClient model)
    {
        var app = ApiTestHost.Build(model: model);
        await app.StartAsync();
        return (app, app.GetTestClient(), app.Services.GetRequiredService<AgentSessions>(),
            app.Services.GetRequiredService<ISessionStore>());
    }

    /// <summary>Opens a chat with the files toolset over the API, and gives back its id.</summary>
    private static async Task<string> OpenAsync(HttpClient client)
    {
        var opening = new CreateSessionRequest(null, null, null, [TestToolset.Files.Wire()]);
        using var opened = await client.PostAsJsonAsync("/v1/sessions", opening);
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>Sends one message with a text attachment, and reads the answer to its end.</summary>
    private static async Task SendAsync(HttpClient client, string id)
    {
        var message = new SendMessageRequest("Plan three dinners", [new AttachmentRequest("list.txt", "eggs")]);
        using var answer = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages", message);
        await answer.Content.ReadAsStringAsync();
    }

    /// <summary>Opening a chat saves how it opened: its id and the toolsets it asked for.</summary>
    [Test]
    public async Task OpeningAChat_SavesHowItOpened()
    {
        var (app, client, _, store) = await StartAsync(Thinks());
        await using var _ = app;

        string id = await OpenAsync(client);
        var saved = await store.LoadAsync(id);

        Assert.That(saved?.Session.Opening.Toolsets, Is.EqualTo(new[] { TestToolset.Files.Wire() }));
    }

    /// <summary>
    /// A finished turn appends what it added to the history, the thinking with its signature among it, and the text
    /// attachment it was sent with, under its number.
    /// </summary>
    [Test]
    public async Task AFinishedTurn_AppendsWhatItAdded_ThinkingIncluded()
    {
        var (app, client, sessions, store) = await StartAsync(Thinks());
        await using var _ = app;

        string id = await OpenAsync(client);
        int before = sessions.Get(id)!.Conversation.Messages.Count;
        await SendAsync(client, id);
        var saved = await store.LoadAsync(id);

        var turn = saved?.Turns.Single();
        Assert.Multiple(() =>
        {
            Assert.That(turn?.Number, Is.EqualTo(1));
            Assert.That(turn?.Messages, Has.Count.EqualTo(sessions.Get(id)!.Conversation.Messages.Count - before));
            Assert.That(turn?.Messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>()
                .Single().ProtectedData, Is.EqualTo("c2lnbmF0dXJl"));
            Assert.That(turn?.Attachments["list.txt"], Is.EqualTo("eggs"));
        });
    }

    /// <summary>Deleting a chat forgets it in the store too.</summary>
    [Test]
    public async Task DeletingAChat_ForgetsItInTheStore()
    {
        var (app, client, _, store) = await StartAsync(Thinks());
        await using var _ = app;

        string id = await OpenAsync(client);
        using var deleted = await client.DeleteAsync($"/v1/sessions/{id}");

        Assert.That(await store.LoadAsync(id), Is.Null);
    }

    /// <summary>An eval trial's chat replays from its recordings and has no next day, so it is never saved.</summary>
    [Test]
    public async Task AnEvalTrialsChat_IsNeverSaved()
    {
        var (app, _, sessions, store) = await StartAsync(Thinks());
        await using var _ = app;

        var trial = new SessionReplay(WebReplayMode.Record, "r1:a:1/t1", false);
        var session = await sessions.CreateAsync(new SessionRequest([TestToolset.Files.Wire()], Replay: trial));

        Assert.That(await store.LoadAsync(session.Id), Is.Null);
    }

    /// <summary>A failed turn appends only what it added to the history, never more.</summary>
    [Test]
    public async Task AFailedTurn_AppendsOnlyWhatItAdded()
    {
        var (app, client, sessions, store) = await StartAsync(Fails());
        await using var _ = app;

        string id = await OpenAsync(client);
        int before = sessions.Get(id)!.Conversation.Messages.Count;
        await SendAsync(client, id);
        int added = sessions.Get(id)!.Conversation.Messages.Count - before;
        var saved = await store.LoadAsync(id);

        Assert.That(saved?.Turns.Sum(turn => turn.Messages.Count) ?? -1, Is.EqualTo(added));
    }
}
