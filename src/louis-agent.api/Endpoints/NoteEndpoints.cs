using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The route for adding a note to a run or to one of its conversations; the work is <c>NoteHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class NoteEndpoints
{
    /// <summary>Adds the route. The clock is passed in so a test can fix the time.</summary>
    public static void MapNotes(this RouteGroupBuilder admin, IBenchmarkStore? store, TimeProvider time)
    {
        var handlers = new NoteHandlers(store, time);

        admin.MapPost("/benchmarks/runs/{id}/notes", handlers.AddNote);
    }
}
