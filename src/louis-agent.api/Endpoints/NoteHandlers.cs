using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using NoteAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.NoteRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// Adds a note to a run or to one of its conversations. The time is always the server's own, never what the request
/// says.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour: the route needs the store and the clock,
/// which aren't services, so they live here and the route is a named method a test can call with no HTTP. It answers
/// <c>CreatedJson</c>, a 201 written with the admin wire's options (the unattended run's finding 2).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class NoteHandlers(IBenchmarkStore? store, TimeProvider time)
{
    /// <summary>
    /// Keeps the note: 201 with it at the run's notes; 404 for a run the store doesn't hold; 400 for a blank or too
    /// long note, saying which; 503 without a store, or one that didn't answer.
    /// </summary>
    public async Task<NoteAnswer> AddNote(string id, NoteRequest? request, CancellationToken cancellationToken)
    {
        if (store is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            // The time comes from our own clock, never from the request.
            var result = await store.AddNoteAsync(id, request?.Text, time.GetUtcNow(),
                request?.Conversation?.ToKey(), cancellationToken);
            return Answer(id, result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>Each answer of the store becomes one status: 201, 404 or 400.</summary>
    /// <remarks>A switch expression on the store's answer, one arm per case (*No nested conditionals*).</remarks>
    private static NoteAnswer Answer(string id, NoteResult result) => result switch
    {
        NoteResult.Added added => new CreatedJson<NoteRow>(
            $"/v1/admin/benchmarks/runs/{Uri.EscapeDataString(id)}/notes", NoteRow.Of(added.Note), AdminJson.SnakeCase),
        NoteResult.NoRun => ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoStoredRun, new { Id = id })),
        NoteResult.Refused refused => ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error",
            Reason(refused)),
        var other => throw new InvalidOperationException(other.GetType().Name),
    };

    /// <summary>The words for a refusal: a blank note, or one past the limit with its length.</summary>
    private static string Reason(NoteResult.Refused refused) => refused.Reason switch
    {
        NoteRefusal.Blank => MessageText.Format(NoteUserText.NoteBlank),
        NoteRefusal.TooLong => MessageText.Format(NoteUserText.NoteTooLong,
            new
            {
                Limit = RunNotes.MaxLength,
                Length = refused.Length
            }),
        _ => throw new ArgumentOutOfRangeException(nameof(refused), refused.Reason, null),
    };
}
