using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the exact JSON a new note answers with, so the notes panel reads it however the route that sends it is shaped.
/// </summary>
public class NoteReplyJsonTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>The time the run started and the clock always says.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A note on a conversation comes back as its text, the server's time and its conversation.</summary>
    [Test]
    public async Task ANewNote_IsWrittenAsBefore()
    {
        // A finished run, and the API over it with a clock that never moves.
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("run_1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, At));
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store, time: new FixedClock(At));
        await app.StartAsync();

        // A note on one conversation of that run.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/admin/benchmarks/runs/run_1/notes")
        {
            Content = JsonContent.Create(new
            {
                text = "Too slow.",
                conversation = new
                {
                    mode = SampleMode.Beta,
                    repeat = 1,
                    task = "bobotie"
                }
            })
        };
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("/v1/admin/benchmarks/runs/run_1/notes"));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(
            """{"text":"Too slow.","at":"2026-10-25T09:00:00+00:00","conversation":{"mode":"mode-beta","repeat":1,"task":"bobotie"}}"""));
    }

    /// <summary>A clock that always says the same time, so the note's time is known.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>The one time it knows.</summary>
        public override DateTimeOffset GetUtcNow() => now;
    }
}
