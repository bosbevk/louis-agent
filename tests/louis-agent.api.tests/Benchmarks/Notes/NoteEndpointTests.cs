using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Notes;

/// <summary>Adding a note to a stored run over HTTP: it answers 201, the time is the server's, bad notes are a 400 with a reason, an unknown run is a 404, and the run's own read carries the notes.</summary>
public class NoteEndpointTests
{
    /// <summary>The admin key the test API is started with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>The id of the one stored run.</summary>
    private const string RunId = "8f7802b9a72e";

    /// <summary>When the stored run started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The time the fixed clock always says.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 14, 30, 0, TimeSpan.Zero);

    /// <summary>A clock that always says the same time, so a test can tell the server's time from one in the request.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>The time this clock was given.</summary>
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>The API under test, with its client and store.</summary>
    private sealed record Host(WebApplication App, HttpClient Client, InMemoryBenchmarkStore Store) : IAsyncDisposable
    {
        /// <summary>Stops the API.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>Starts the API with the admin key, a store holding one run, and the clock fixed.</summary>
    private static async Task<Host> StartAsync()
    {
        // A store that already holds one finished run.
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0));

        // The API with the admin key, that store, and a clock that never moves.
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, time: new FixedClock(Now));
        await app.StartAsync();

        return new Host(app, app.GetTestClient(), store);
    }

    /// <summary>The notes route of a run.</summary>
    private static string NotesPath(string id = RunId) => $"/v1/admin/benchmarks/runs/{id}/notes";

    /// <summary>Posts a body to a path, with the admin key unless told not to.</summary>
    private static Task<HttpResponseMessage> Post(HttpClient client, string url, object body, bool withKey = true)
    {
        // The admin key travels in the x-api-key header; leave it off to test the gate.
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (withKey) request.Headers.Add("x-api-key", AdminKey);

        return client.SendAsync(request);
    }

    /// <summary>The response body as JSON.</summary>
    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>The run's own read, with the admin key.</summary>
    private static async Task<JsonElement> ReadRun(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}");
        request.Headers.Add("x-api-key", AdminKey);
        return await JsonOf(await client.SendAsync(request));
    }

    /// <summary>A post keeps the note and answers 201 with it. The time is the server's, even if the request sends one.</summary>
    [Test]
    public async Task APost_KeepsTheNote_Answers201WithIt_AndTheTimeIsTheServers()
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath(),
            new { text = "  the judge changed  ", conversation = (object?)null, at = "2001-01-01T00:00:00Z" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.ToString(), Is.EqualTo(NotesPath()));
        var row = await JsonOf(response);
        Assert.That(row.GetProperty("text").GetString(), Is.EqualTo("the judge changed"));
        Assert.That(row.GetProperty("at").GetDateTimeOffset(), Is.EqualTo(Now));
        Assert.That(row.GetProperty("conversation").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(await host.Store.NotesAsync(RunId), Is.EqualTo(new[] { new RunNote("the judge changed", Now) }));
    }

    /// <summary>A note with a conversation key is kept on that conversation.</summary>
    [Test]
    public async Task APostWithAConversation_KeepsTheNoteOnThatConversation()
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath(),
            new
            {
                text = "hallucinated a price",
                conversation = new { mode = SampleMode.Beta, repeat = 2, task = "bobotie" }
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var kept = (await host.Store.NotesAsync(RunId)).Single();
        Assert.That(kept.Conversation,
            Is.EqualTo(new BenchmarkProtocol.ConversationKey(SampleMode.Beta, 2, "bobotie")));
        var echoed = (await JsonOf(response)).GetProperty("conversation");
        Assert.That(
            (echoed.GetProperty("mode").GetString(), echoed.GetProperty("repeat").GetInt32(),
                echoed.GetProperty("task").GetString()), Is.EqualTo((SampleMode.Beta, 2, "bobotie")));
    }

    /// <summary>The run's own read carries its notes, in the order written.</summary>
    [Test]
    public async Task TheRunsRead_CarriesItsNotes_InTheOrderWritten()
    {
        await using var host = await StartAsync();
        await host.Store.AddNoteAsync(RunId, "first", T0.AddMinutes(1));
        await host.Store.AddNoteAsync(RunId, "second", T0.AddMinutes(2),
            new BenchmarkProtocol.ConversationKey(SampleMode.Beta, 1, "bobotie"));

        var notes = (await ReadRun(host.Client)).GetProperty("notes").EnumerateArray().ToList();

        Assert.That(notes.Select(n => n.GetProperty("text").GetString()), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(notes[0].GetProperty("at").GetDateTimeOffset(), Is.EqualTo(T0.AddMinutes(1)));
        Assert.That(notes[0].GetProperty("conversation").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(notes[1].GetProperty("conversation").GetProperty("task").GetString(), Is.EqualTo("bobotie"));
    }

    /// <summary>A run with no notes reads with an empty list, not a missing one.</summary>
    [Test]
    public async Task ARunWithNoNotes_ReadsWithAnEmptyList()
    {
        await using var host = await StartAsync();

        Assert.That((await ReadRun(host.Client)).GetProperty("notes").GetArrayLength(), Is.EqualTo(0));
    }

    /// <summary>An empty note is a 400 that says why.</summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task ABlankNote_Is400_WithItsReason(string? text)
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath(), new { text });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(),
            Does.Contain(MessageText.Format(NoteUserText.NoteBlank)));
        Assert.That(await host.Store.NotesAsync(RunId), Is.Empty);
    }

    /// <summary>A note over the limit is a 400 that says the limit and the length.</summary>
    [Test]
    public async Task ATooLongNote_Is400_WithTheLimitAndTheLength()
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath(), new { text = new string('a', RunNotes.MaxLength + 1) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(),
            Does.Contain(MessageText.Format(NoteUserText.NoteTooLong,
                new { Limit = RunNotes.MaxLength, Length = RunNotes.MaxLength + 1 })));
    }

    /// <summary>A note on a run that isn't stored is a 404.</summary>
    [Test]
    public async Task ANoteOnAnUnknownRun_Is404()
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath("nope"), new { text = "hello" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Without the admin key you get a 401, and nothing is kept.</summary>
    [Test]
    public async Task WithoutTheAdminKey_Is401_AndNothingIsKept()
    {
        await using var host = await StartAsync();

        var response = await Post(host.Client, NotesPath(), new { text = "hello" }, withKey: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await host.Store.NotesAsync(RunId), Is.Empty);
    }
}
