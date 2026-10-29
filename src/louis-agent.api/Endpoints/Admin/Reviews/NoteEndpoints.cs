using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Endpoints.Admin.Reviews;

/// <summary>
/// Hangs the route that adds a note to a run or to one of its conversations; the work is in <c>NoteHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class NoteEndpoints
{
    /// <summary>Adds the notes route. The clock comes in from outside so a test can fix the time.</summary>
    public static void MapNotes(this RouteGroupBuilder evals, IEvalStore? store, TimeProvider time)
    {
        var handlers = new NoteHandlers(store, time);

        evals.MapPost("/runs/{id}/notes", handlers.AddNote);
    }
}
