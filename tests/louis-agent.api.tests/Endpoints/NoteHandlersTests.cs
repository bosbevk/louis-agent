using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>The note handler declares what it answers (ADR 0045), and answers it with no HTTP in between.</summary>
public class NoteHandlersTests
{
    /// <summary>The time the run started and the clock always says.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Adding a note answers a 201 with the note, or an error.</summary>
    [Test]
    public void AddingANote_DeclaresItsAnswers()
    {
        // Found by name, so this test still compiles while the handler is being moved.
        var type = typeof(ApiErrors).Assembly.GetTypes().SingleOrDefault(t => t.Name == "NoteHandlers");

        Assert.That(type?.GetMethod("AddNote")?.ReturnType,
            Is.EqualTo(typeof(Task<Results<CreatedJson<NoteRow>, JsonHttpResult<ApiError>>>)));
    }

    /// <summary>A kept note comes back as a 201 at the run's notes, stamped with the server's time.</summary>
    [Test]
    public async Task AKeptNote_Is201_WithTheServersTime()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("run_1", SampleEval.Beta, 1, BenchmarkRunStatus.Done, At));
        var handlers = new NoteHandlers(store, new FixedClock(At));

        var answer = await handlers.AddNote("run_1", new NoteRequest("Too slow.", null), CancellationToken.None);

        var created = (CreatedJson<NoteRow>)answer.Result;
        Assert.That(created.Location, Is.EqualTo("/v1/admin/benchmarks/runs/run_1/notes"));
        Assert.That(created.Value, Is.EqualTo(new NoteRow("Too slow.", At, null)));
    }

    /// <summary>A note on a run the store doesn't hold is a 404, and without a store a 503.</summary>
    [Test]
    public async Task ANoteWithNowhereToGo_IsAnError()
    {
        var request = new NoteRequest("Too slow.", null);

        var noRun = await new NoteHandlers(new InMemoryBenchmarkStore(), new FixedClock(At))
            .AddNote("ghost", request, CancellationToken.None);
        var noStore =
            await new NoteHandlers(null, new FixedClock(At)).AddNote("run_1", request, CancellationToken.None);

        Assert.That(((JsonHttpResult<ApiError>)noRun.Result).StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(((JsonHttpResult<ApiError>)noStore.Result).StatusCode,
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    /// <summary>A clock that always says the same time.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>The one time it knows.</summary>
        public override DateTimeOffset GetUtcNow() => now;
    }
}
