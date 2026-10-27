using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The route for adding a note to a run or to one of its conversations. The time is always the server's own, never what the request says.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class NoteEndpoints
{
    /// <summary>What the portal sends: the text, and a conversation's key if the note is on one.</summary>
    internal sealed record NoteRequest(string? Text, BenchmarkProtocol.ConversationKey? Conversation);

    /// <summary>Adds the route. The clock is passed in so a test can fix the time.</summary>
    public static void MapNotes(this RouteGroupBuilder admin, IBenchmarkStore? store, TimeProvider time)
    {
        // The time comes from our own clock, never from the request. Each answer of the store becomes one status: 201, 404 or 400.
        admin.MapPost("/benchmarks/runs/{id}/notes",
            (string id, NoteRequest? request, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store,
                async s =>
                    await s.AddNoteAsync(id, request?.Text, time.GetUtcNow(), request?.Conversation,
                            cancellationToken) switch
                        {
                            NoteResult.Added added => Results.Created(
                                $"/v1/admin/benchmarks/runs/{Uri.EscapeDataString(id)}/notes", Row(added.Note)),
                            NoteResult.NoRun => ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                                MessageText.Format(ApiUserText.NoStoredRun, new { Id = id })),
                            NoteResult.Refused refused => ApiErrors.Result(StatusCodes.Status400BadRequest,
                                "invalid_request_error", Reason(refused)),
                            var other => throw new InvalidOperationException(other.GetType().Name),
                        }));
    }

    /// <summary>A note the way the portal reads it.</summary>
    internal static object Row(RunNote note) => new
    {
        text = note.Text,
        at = note.At,
        conversation = note.Conversation is { } c ? new { mode = c.Mode, repeat = c.Repeat, task = c.Task } : null,
    };

    /// <summary>The words for a refusal.</summary>
    private static string Reason(NoteResult.Refused refused) => refused.Reason switch
    {
        NoteRefusal.Blank => MessageText.Format(NoteUserText.NoteBlank),
        NoteRefusal.TooLong => MessageText.Format(NoteUserText.NoteTooLong,
            new { Limit = RunNotes.MaxLength, Length = refused.Length }),
        _ => throw new ArgumentOutOfRangeException(nameof(refused), refused.Reason, null),
    };
}
