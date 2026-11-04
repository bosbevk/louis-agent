using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using GradeAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.HumanGradeRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ReviewsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Reviews.ReviewsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using LabelsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Reviews;

/// <summary>
/// Keeps people's grades of a stored run: add one for a human review of the run (an earlier grade is never
/// overwritten), list the run's with the history marked, and export a judge's passes and fails as label files in a
/// zip. The portal has one shared key, so the reviewer is just a name the person types.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: the routes
/// share the store and the review store, which aren't services, so they live here and each route is a named method a
/// test can call with no HTTP. Each returns <c>TypedResults</c> with its answers declared. A new grade's checks are
/// guard clauses, one per refusal, in the order a person would fix them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class ReviewHandlers(IEvalStore? store, IHumanReviewStore? reviews)
{
    /// <summary>
    /// The longest note and reviewer name a grade may carry, so one request can't fill the store.
    /// </summary>
    internal const int MaxNoteLength = 2000, MaxReviewerLength = 100;

    /// <summary>
    /// Keeps a person's grade of one conversation, for one of the run's human reviews. It turns away an unknown
    /// verdict, a blank or too long reviewer or note, a grader the run has no human review for, and a run or
    /// conversation that isn't stored.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per refusal, in the order a person would fix them.</remarks>
    public async Task<GradeAnswer> PutReview(string id, string mode, int repeat, string task, string grader,
        ReviewRequest? request, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            if (Verdict(request?.Verdict) is not { } verdict) return UnknownVerdict(request?.Verdict ?? "");
            // A known verdict means the request was there, hence the !.
            if (Refusal(request!) is { } refusal) return Bad(refusal);
            var humans = ReviewQueries.HumanReviewsOf(run);
            if (!humans.Contains(grader)) return NotHuman(id, grader, humans);

            var key = new ConversationKey(mode, repeat, task);
            if ((await store.ConversationsAsync(id, cancellationToken)).All(c => c.Key != key))
                return NoConversation(id, key);

            // The grade is kept under the judge's own name (the grader without its human-review suffix), stamped now.
            var grade = new HumanGrade(id, key, grader[..^HumanReview.Suffix.Length], verdict, request!.Note ?? "",
                request.Reviewer!.Trim(), DateTimeOffset.UtcNow);
            await reviews.AddAsync(grade, cancellationToken);
            return TypedResults.Json(HumanGradeRow.Of(grade, latest: true), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>
    /// Lists every grade people gave the run, history included, marking each reviewer's latest.
    /// </summary>
    public async Task<ReviewsAnswer> GetReviews(string id, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is null) return NoRun(id);
            var all = await reviews.OfRunAsync(id, cancellationToken);
            var latest = HumanGrades.Latest(all).ToHashSet();
            var reply = new ReviewsReply([.. all.Select(g => HumanGradeRow.Of(g, latest.Contains(g)))]);
            return TypedResults.Json(reply, AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>
    /// Downloads a judge's labels as a zip of label files. The run needs a human review of that judge, and someone has
    /// to have decided a grade of it.
    /// </summary>
    public async Task<LabelsAnswer> GetLabels(string id, string? grader, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var humans = ReviewQueries.HumanReviewsOf(run);
            if (grader is null || !humans.Contains(grader + HumanReview.Suffix)) return NeedsAGrader(humans);

            var labels = await ReviewQueries.DecidedLabelsAsync(store, reviews, id, grader, cancellationToken);
            if (labels.Count == 0)
                return ApiErrors.NotFound(MessageText.Format(ReviewUserText.NoLabelsYet, new
                {
                    Grader = grader,
                    Id = id
                }));
            return TypedResults.File(ReviewLabels.Zip(labels), "application/zip", $"{grader}-labels-{id}.zip");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Reads the verdict from its word. A number never counts, even one an enum would take.</summary>
    private static HumanVerdict? Verdict(string? text)
    {
        if (string.IsNullOrEmpty(text) || int.TryParse(text, out _)) return null;
        return SettingChoice.TryParse(text, out HumanVerdict? verdict, null) ? verdict : null;
    }

    /// <summary>
    /// Says why the reviewer or the note can't be kept: a blank reviewer, or either one too long.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per reason, checked in the order the form shows them.</remarks>
    private static string? Refusal(ReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer)) return MessageText.Format(ReviewUserText.ReviewerBlank);

        int reviewer = request.Reviewer.Trim().Length;
        if (reviewer > MaxReviewerLength)
            return MessageText.Format(ReviewUserText.ReviewerTooLong, new
            {
                Limit = MaxReviewerLength,
                Length = reviewer
            });

        int note = (request.Note ?? "").Length;
        if (note > MaxNoteLength)
            return MessageText.Format(ReviewUserText.NoteTooLong, new
            {
                Limit = MaxNoteLength,
                Length = note
            });
        return null;
    }

    /// <summary>The 400 for a verdict we don't know, listing the ones we do.</summary>
    private static JsonHttpResult<ApiError> UnknownVerdict(string text) =>
        Bad(MessageText.Format(ReviewUserText.ReviewVerdictUnknown,
            new
            {
                Verdict = text,
                Supported = string.Join(", ", SettingChoice.Names<HumanVerdict>())
            }));

    /// <summary>The 400 for a grader the run has no human review for, listing the ones it has.</summary>
    private static JsonHttpResult<ApiError> NotHuman(string id, string grader, HashSet<string> humans) =>
        Bad(MessageText.Format(ReviewUserText.ReviewGraderNotHuman,
            new
            {
                Grader = grader,
                Id = id,
                Graders = string.Join(", ", humans.Order(StringComparer.Ordinal))
            }));

    /// <summary>The 400 for labels of a judge nobody reviewed on the run, listing the ones people did.</summary>
    private static JsonHttpResult<ApiError> NeedsAGrader(HashSet<string> humans) =>
        Bad(MessageText.Format(ReviewUserText.LabelsNeedAGrader,
            new
            {
                Graders = string.Join(", ",
                    humans.Select(h => h[..^HumanReview.Suffix.Length]).Order(StringComparer.Ordinal))
            }));

    /// <summary>The 404 for a conversation the run didn't store.</summary>
    private static JsonHttpResult<ApiError> NoConversation(string id, ConversationKey key) =>
        ApiErrors.NotFound(MessageText.Format(ReviewUserText.NoStoredConversation,
            new
            {
                Id = id,
                key.Mode,
                key.Repeat,
                key.Task
            }));

    /// <summary>A 400 that says what was wrong.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) => ApiErrors.BadRequest(message);

    /// <summary>The 503 when there's no store at all.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Unavailable(MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 503 when the store didn't answer, with its reason.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));

    /// <summary>The 404 for a run the store doesn't hold.</summary>
    private static JsonHttpResult<ApiError> NoRun(string id) =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
}
