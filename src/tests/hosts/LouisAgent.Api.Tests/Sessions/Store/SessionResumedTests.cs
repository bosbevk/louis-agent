using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Store;
using LouisAgent.Core.Agent;
using LouisAgent.Choices;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Sessions.Store;

/// <summary>
/// F43.1 step 4: a chat survives a restart. A second API over the same store, sent a chat's id it doesn't hold, reads
/// the chat back, opens it again the same way, and its next turn carries the earlier ones. A visitor's chat, opened
/// through the open door, resumes without the key, as it was used; a chat opened with the key still needs it (Louis's
/// pick, "Visitors' own, keyed need the key"); an id no one holds is still a 404.
/// </summary>
public class SessionResumedTests
{
    private const string Key = "test-key";

    /// <summary>An open identity, as the site's guide is.</summary>
    private static readonly FakeIdentity Guide = new("guide", voice: TestAgent.OtherVoice);

    /// <summary>A model that answers each request with these words, keeping what it was asked.</summary>
    private static FakeChatClient Answers(string words) =>
        new(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, words)), 4).ToArray());

    /// <summary>
    /// One run of the API, behind the key, with the guide's door open, keeping its chats in the store given: two runs
    /// over one store are the same API before and after a restart.
    /// </summary>
    private static async Task<(WebApplication App, HttpClient Client)> RunAsync(ISessionStore store,
        IChatClient model)
    {
        var app = ApiTestHost.Build(Key, model: model, moreIdentities: [Guide],
            openDoor: new OpenDoorOptions(["guide"], 5),
            services: (builder, _) => builder.Services.AddSingleton(store));
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    /// <summary>Opens a chat, with the key or through the door as a visitor, and gives back its id.</summary>
    private static async Task<string> OpenAsync(HttpClient client, bool keyed)
    {
        var opening = keyed
            ? new CreateSessionRequest(null, null, null, [TestToolset.Files.Wire()])
            : new CreateSessionRequest(null, null, null, Identity: "guide");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/sessions");
        request.Content = JsonContent.Create(opening, options: SessionJson.SnakeCase);
        if (keyed) request.Headers.Add("x-api-key", Key);
        using var opened = await client.SendAsync(request);
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>Sends a message to the chat, with the key or without, and reads the answer to its end.</summary>
    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string id, string words, bool keyed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/sessions/{id}/messages");
        request.Content = JsonContent.Create(new SendMessageRequest(words, null));
        if (keyed) request.Headers.Add("x-api-key", Key);
        using var answer = await client.SendAsync(request);
        await answer.Content.ReadAsStringAsync();
        return answer.StatusCode;
    }

    /// <summary>The words of every message the model was sent in its first request.</summary>
    private static string[] FirstRequest(FakeChatClient model) =>
        model.Requests[0].Messages.Select(message => message.Text).ToArray();

    /// <summary>
    /// A chat opened with the key answers its next message after a restart, with the key, and the model is sent the
    /// earlier turn: the question and its answer, before the new question.
    /// </summary>
    [Test]
    public async Task AKeyedChat_ResumesAfterARestart_WithItsEarlierTurn()
    {
        var store = new InMemorySessionStore();
        var (before, beforeClient) = await RunAsync(store, Answers("Monday, Tuesday and Wednesday."));
        string id = await OpenAsync(beforeClient, keyed: true);
        await SendAsync(beforeClient, id, "Plan three dinners", keyed: true);
        await before.DisposeAsync();

        var after = Answers("Fish on Monday.");
        var (restarted, client) = await RunAsync(store, after);
        await using var _ = restarted;
        var status = await SendAsync(client, id, "What is on Monday?", keyed: true);

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(FirstRequest(after),
                Is.SupersetOf(new[] { "Plan three dinners", "Monday, Tuesday and Wednesday.", "What is on Monday?" }));
        });
    }

    /// <summary>
    /// A command such as /tools is a turn that keeps nothing, so a chat that had one and lived through a restart counts
    /// on from its last kept turn, never under a number it already used: every message after the restart is answered
    /// and kept (FX256: the restored chat counted its kept turns, wrote its next under a used number, and stayed busy).
    /// </summary>
    [Test]
    public async Task AChatWithACommand_CountsOnAfterARestart()
    {
        var store = new InMemorySessionStore();
        var (before, beforeClient) = await RunAsync(store, Answers("Monday, Tuesday and Wednesday."));
        string id = await OpenAsync(beforeClient, keyed: true);
        await SendAsync(beforeClient, id, "/tools", keyed: true);
        await SendAsync(beforeClient, id, "Plan three dinners", keyed: true);
        await before.DisposeAsync();

        var (restarted, client) = await RunAsync(store, Answers("Fish on Monday."));
        await using var _ = restarted;
        var first = await SendAsync(client, id, "What is on Monday?", keyed: true);
        var second = await SendAsync(client, id, "And on Tuesday?", keyed: true);

        var kept = (await store.LoadAsync(id))!.Turns.Select(turn => turn.Number);
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(kept, Is.EqualTo(new[] { 2, 3, 4 }));
        });
    }

    /// <summary>
    /// A store that refuses to keep a turn never leaves the chat busy: the next message is answered, not refused as
    /// still answering (FX256: the refusal was thrown where the turn lets the chat go, so the chat stayed busy).
    /// </summary>
    [Test]
    public async Task AStoreThatRefusesATurn_NeverLeavesTheChatBusy()
    {
        var (app, client) = await RunAsync(new RefusingStore(), Answers("Monday, Tuesday and Wednesday."));
        await using var _ = app;
        string id = await OpenAsync(client, keyed: true);

        var first = await SendAsync(client, id, "Plan three dinners", keyed: true);
        var second = await SendAsync(client, id, "What is on Monday?", keyed: true);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>A store that keeps a chat's opening but refuses every turn, as a database that is down would.</summary>
    private sealed class RefusingStore : ISessionStore
    {
        private readonly InMemorySessionStore _kept = new();

        /// <summary>Keeps the opening.</summary>
        public Task SaveOpeningAsync(StoredSession session, CancellationToken cancellationToken = default) =>
            _kept.SaveOpeningAsync(session, cancellationToken);

        /// <summary>Refuses the turn.</summary>
        public Task AppendTurnAsync(string sessionId, StoredTurn turn, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The store is down.");

        /// <summary>Gives back what it kept.</summary>
        public Task<SavedSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default) =>
            _kept.LoadAsync(sessionId, cancellationToken);

        /// <summary>Forgets the chat.</summary>
        public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default) =>
            _kept.DeleteAsync(sessionId, cancellationToken);
    }

    /// <summary>After a restart a chat opened with the key still needs the key: without it, it is refused.</summary>
    [Test]
    public async Task AKeyedChat_StillNeedsTheKey_AfterARestart()
    {
        var store = new InMemorySessionStore();
        var (before, beforeClient) = await RunAsync(store, Answers("Done."));
        string id = await OpenAsync(beforeClient, keyed: true);
        await SendAsync(beforeClient, id, "Plan three dinners", keyed: true);
        await before.DisposeAsync();

        var (restarted, client) = await RunAsync(store, Answers("Done."));
        await using var _ = restarted;

        Assert.That(await SendAsync(client, id, "And now?", keyed: false), Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// A visitor's chat, opened through the open door with no key, resumes after a restart with no key, carrying its
    /// earlier turn.
    /// </summary>
    [Test]
    public async Task AVisitorsChat_ResumesWithoutTheKey()
    {
        var store = new InMemorySessionStore();
        var (before, beforeClient) = await RunAsync(store, Answers("Welcome."));
        string id = await OpenAsync(beforeClient, keyed: false);
        await SendAsync(beforeClient, id, "Hello", keyed: false);
        await before.DisposeAsync();

        var after = Answers("Still here.");
        var (restarted, client) = await RunAsync(store, after);
        await using var _ = restarted;
        var status = await SendAsync(client, id, "Are you there?", keyed: false);

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(FirstRequest(after), Is.SupersetOf(new[] { "Hello", "Welcome.", "Are you there?" }));
        });
    }

    /// <summary>
    /// A chat saved with one earlier turn, its prompt and the mark of the tools that turn was sent with, as a store
    /// written by an earlier release holds it.
    /// </summary>
    private static async Task<InMemorySessionStore> SavedChatAsync(string id, string prompt, ToolsFingerprint? tools)
    {
        var store = new InMemorySessionStore();
        var opening = new SessionRequest([TestToolset.Files.Wire()]);
        var at = new DateTimeOffset(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
        await store.SaveOpeningAsync(new StoredSession(id, opening, at, prompt));
        ChatMessage[] said = [new(ChatRole.User, "Plan a dinner"), new(ChatRole.Assistant, "Fish.")];
        await store.AppendTurnAsync(id, new StoredTurn(1, at.AddMinutes(1), said, new Dictionary<string, string>(),
            tools));
        return store;
    }

    /// <summary>
    /// A chat resumed after a release changed the system prompt sends the prompt it was saved with, not today's, so
    /// the thinking in its earlier turns still matches.
    /// </summary>
    [Test]
    public async Task AResumedChat_SendsTheSystemPromptItWasSavedWith()
    {
        const string savedPrompt = "The prompt this chat was saved with.";
        var store = await SavedChatAsync("sess_saved", savedPrompt, tools: null);
        var model = Answers("Fish again.");
        var (app, client) = await RunAsync(store, model);
        await using var _ = app;

        await SendAsync(client, "sess_saved", "And on Tuesday?", keyed: true);

        Assert.That(model.Requests[0].Messages[0].Text, Is.EqualTo(savedPrompt));
    }

    /// <summary>A chat saved and resumed on the same tools knows they didn't change.</summary>
    [Test]
    public async Task AResumedChat_OnTheSameTools_KnowsTheyDidntChange()
    {
        var store = new InMemorySessionStore();
        var (before, beforeClient) = await RunAsync(store, Answers("Monday, Tuesday and Wednesday."));
        string id = await OpenAsync(beforeClient, keyed: true);
        await SendAsync(beforeClient, id, "Plan three dinners", keyed: true);
        await before.DisposeAsync();

        var (after, _) = await RunAsync(store, Answers("Fish."));
        await using var __ = after;
        var resumed = await after.Services.GetRequiredService<AgentSessions>().GetAsync(id);

        Assert.That(resumed!.Conversation.ToolsChangedSinceSaved, Is.False);
    }

    /// <summary>A chat whose saved turns were sent other tools than today's knows its tools changed since.</summary>
    [Test]
    public async Task AResumedChat_OnOtherTools_KnowsTheyChanged()
    {
        var store = await SavedChatAsync("sess_moved", "You plan dinners.", new ToolsFingerprint("an older tool list"));
        var (app, _) = await RunAsync(store, Answers("Fish."));
        await using var __ = app;

        var resumed = await app.Services.GetRequiredService<AgentSessions>().GetAsync("sess_moved");

        Assert.That(resumed!.Conversation.ToolsChangedSinceSaved, Is.True);
    }

    /// <summary>An id that neither memory nor the store holds is still a 404.</summary>
    [Test]
    public async Task AnIdNoOneHolds_IsStillNotFound()
    {
        var (app, client) = await RunAsync(new InMemorySessionStore(), Answers("Done."));
        await using var _ = app;

        Assert.That(await SendAsync(client, "sess_nobody", "Hi", keyed: true), Is.EqualTo(HttpStatusCode.NotFound));
    }
}
