using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Sessions.Store;
using LouisAgent.Api.Sessions.Store.Mongo;
using LouisAgent.Choices;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace LouisAgent.Api.IntegrationTests.Sessions.Store;

/// <summary>
/// F43.1's done-when, through the API on a real MongoDB: a chat answered by one run of the API answers its next
/// message in a second run over the same database, as after a restart, and the model is sent the earlier turn. Runs
/// where <c>SESSIONS_MONGO_TEST_URL</c> names a MongoDB (CI's throwaway one), on a database of its own.
/// </summary>
[Category("Integration")]
public class MongoSessionResumedTests
{
    private const string Key = "test-key";

    /// <summary>The setting that names the MongoDB the test runs against.</summary>
    private const string Setting = "SESSIONS_MONGO_TEST_URL";

    private MongoClient _client = null!;
    private string _database = null!;

    [SetUp]
    public void SetUp()
    {
        string? url = Environment.GetEnvironmentVariable(Setting);
        Outside.Tool(!string.IsNullOrEmpty(url), $"{Setting} isn't set: a chat's resume on MongoDB isn't tested here");
        _client = new MongoClient(url);
        _database = $"sessions_resume_{Guid.NewGuid():N}";
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_client is null) return;
        await _client.DropDatabaseAsync(_database);
        _client.Dispose();
    }

    /// <summary>A model that answers each request with these words, keeping what it was asked.</summary>
    private static FakeChatClient Answers(string words) =>
        new(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, words)), 4).ToArray());

    /// <summary>
    /// One run of the API, keeping its chats in this test's database: a new store, as a restarted host has.
    /// </summary>
    private async Task<(WebApplication App, HttpClient Client)> RunAsync(IChatClient model)
    {
        ISessionStore store = new MongoSessionStore(_client.GetDatabase(_database), TimeSpan.FromDays(30));
        var app = ApiTestHost.Build(Key, model: model,
            services: (builder, _) => builder.Services.AddSingleton(store));
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", Key);
        return (app, client);
    }

    /// <summary>Sends a message and reads the answer to its end.</summary>
    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string id, string words)
    {
        using var answer = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages",
            new SendMessageRequest(words, null));
        await answer.Content.ReadAsStringAsync();
        return answer.StatusCode;
    }

    /// <summary>The chat answers after the restart, and the model is sent the first question and its answer.</summary>
    [Test]
    public async Task AChat_AnswersAfterARestart_KnowingItsEarlierTurn()
    {
        var (before, beforeClient) = await RunAsync(Answers("Monday, Tuesday and Wednesday."));
        var opening = new CreateSessionRequest(null, null, null, [TestToolset.Files.Wire()]);
        using var opened = await beforeClient.PostAsJsonAsync("/v1/sessions", opening, SessionJson.SnakeCase);
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("session_id").GetString()!;
        await SendAsync(beforeClient, id, "Plan three dinners");
        await before.DisposeAsync();

        var after = Answers("Fish on Monday.");
        var (restarted, client) = await RunAsync(after);
        await using var _ = restarted;
        var status = await SendAsync(client, id, "What is on Monday?");

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(after.Requests[0].Messages.Select(message => message.Text), Is.SupersetOf(new[]
            {
                "Plan three dinners", "Monday, Tuesday and Wednesday.", "What is on Monday?"
            }));
        });
    }
}
