using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Evals.Messages;
using LouisAgent.Api.Evals.Messages.Holes;
using LouisAgent.Api.Evals.Messages.Holes.Runs;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Runs.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using FlipAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Evals.Conversations;

/// <summary>
/// The routes of one conversation of a kept run: a person flips a criterion's mark at the mark's own address, with a
/// note if they want one, and puts it back with a DELETE there. A run that is going is refused, since its file is
/// still being written.
/// </summary>
/// <remarks>
/// Named handlers returning typed results, their checks first as guard clauses (ADR 0051). The store changes the run
/// under its lock (<see cref="RunStore.Change" />), so two flips at once both stay, and the run copies itself with
/// the flip (<c>EvalRun.WithFlip</c>), so no route reaches inside a mark. A flip lives on its mark (Louis's pick,
/// 2026-11-23, "On the mark itself"), with an optional one-line note (his pick, "An optional one-line note").
/// </remarks>
/// <param name="desk">The one run going, which can't be flipped.</param>
/// <param name="work">How a run is kept, or none on a host with no data folder.</param>
/// <param name="clock">The time a flip is made at.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public sealed class ConversationHandlers(RunDesk desk, RunWork? work, TimeProvider clock)
{
    /// <summary>The longest note a flip keeps, on its one line.</summary>
    public const int NoteLength = 200;

    /// <summary>
    /// <c>PUT …/runs/{id}/{conversation}/{repeat}/{turn}/criteria/{criterion}/flip</c>: flips the criterion's mark,
    /// with the body's note or none. The turn and the criterion count from 1, as the pages show them.
    /// </summary>
    public async Task<FlipAnswer> FlipAsync(string id, string conversation, int repeat, int turn, int criterion,
        HttpRequest body)
    {
        if (Refused(id) is { } refused) return refused;
        if (await RequestOf(body) is not { } request)
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.FlipNotRead));
        if (!IsOneLine(request.Note))
        {
            var most = new NoteNotOneLineHoles(NoteLength);
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.NoteNotOneLine, most));
        }

        // A note of nothing but spaces is no note.
        string? note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var flip = new Flip(clock.GetUtcNow(), note);
        return Change(new MarkAt(id, conversation, repeat, turn, criterion), flip);
    }

    /// <summary>
    /// <c>DELETE</c> at the same address: puts the judge's answer back. A mark that wasn't flipped stays as it was.
    /// </summary>
    public FlipAnswer UndoFlip(string id, string conversation, int repeat, int turn, int criterion)
    {
        if (Refused(id) is { } refused) return refused;

        return Change(new MarkAt(id, conversation, repeat, turn, criterion), null);
    }

    /// <summary>
    /// Why no mark of this run may be changed, or none: an id that names no run, a host with nowhere to keep one, or
    /// the run that is going, whose file is still being written.
    /// </summary>
    private JsonHttpResult<ApiError>? Refused(string id)
    {
        if (!RunId.TryParse(id, out _) || work is null) return EvalHandlers.NoSuchRun(id);
        if (desk.Current?.Id != id) return null;

        var going = new RunStillGoingHoles(id);
        return ApiErrors.Conflict(MessageText.Format(EvalsApiMessage.RunStillGoing, going));
    }

    /// <summary>The mark flipped, or put back with none, in the kept run; a run or a mark it lacks is 404.</summary>
    private FlipAnswer Change(MarkAt at, Flip? flip)
    {
        var store = work!.Store;
        if (store.Find(at.Id) is null) return EvalHandlers.NoSuchRun(at.Id);
        if (store.Change(at.Id, run => run.WithFlip(at.Conversation, at.Repeat, at.Turn, at.Criterion, flip)))
            return TypedResults.NoContent();

        var none = new NoSuchMarkHoles(at.Id, at.Conversation, at.Repeat, at.Turn, at.Criterion);
        return ApiErrors.NotFound(MessageText.Format(EvalsApiMessage.NoSuchMark, none));
    }

    /// <summary>
    /// The flip's body, read with the run's own options; an empty body is a flip with no note, and one that doesn't
    /// read is none.
    /// </summary>
    private static async Task<FlipRequest?> RequestOf(HttpRequest body)
    {
        if (body.ContentLength is 0 || (body.ContentLength is null && !body.HasJsonContentType()))
            return new FlipRequest(null);
        try
        {
            return await body.ReadFromJsonAsync<FlipRequest>(RunJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A note is one line, at most <see cref="NoteLength" /> characters, or none.</summary>
    private static bool IsOneLine(string? note) =>
        note is null || (note.Length <= NoteLength && note.IndexOfAny(['\n', '\r']) < 0);

    /// <summary>Where a criterion's mark is: the run, conversation and repeat, the turn and criterion from 1.</summary>
    private sealed record MarkAt(string Id, string Conversation, int Repeat, int Turn, int Criterion);
}
