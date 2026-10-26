using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// People's grades of a stored run (F18 step 6a.2, ADR 0039: a resource, <c>reviews</c>, under the run): <c>PUT …/reviews/{mode}/{repeat}/{task}/{grader}</c> adds one (the grader is the run's
/// human review, <c>meal-for-people.human</c>; the grade is kept for the model grader it answers, and an earlier grade is never overwritten), <c>GET …/reviews</c> lists the run's with
/// the history marked, and <c>GET …/labels?grader=</c> exports the passes and fails as label files in a zip. The portal's gate is one shared key, so the reviewer is a name the person gives.
/// They belong to the same <c>/admin</c> group as the rest, so the key gate covers them.
/// </summary>
internal static class ReviewEndpoints
{
    /// <summary>What the portal sends: the verdict as a word, a note, and the reviewer's name.</summary>
    internal sealed record ReviewRequest(string? Verdict, string? Note, string? Reviewer);

    /// <summary>The longest note and reviewer name a grade may carry (F18 step 6a.2b): a request cannot fill the store with one grade.</summary>
    internal const int MaxNoteLength = 2000, MaxReviewerLength = 100;

    private const string HumanSuffix = ".human";

    public static void MapReviews(this RouteGroupBuilder admin, IBenchmarkStore? store, IHumanReviewStore? reviews)
    {
        admin.MapPut("/benchmarks/runs/{id}/reviews/{mode}/{repeat:int}/{task}/{grader}",
            (string id, string mode, int repeat, string task, string grader, ReviewRequest? request, CancellationToken cancellationToken) => Guarded(store, reviews, async (s, r) =>
            {
                if (await s.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);

                string verdictText = request?.Verdict ?? "";
                if (!SettingChoice.TryParse(verdictText, out HumanVerdict? verdict, null) || verdict is null || int.TryParse(verdictText, out _))
                    return Bad(MessageText.Format(ReviewUserText.ReviewVerdictUnknown, new { Verdict = verdictText, Supported = string.Join(", ", SettingChoice.Names<HumanVerdict>()) }));
                if (string.IsNullOrWhiteSpace(request?.Reviewer)) return Bad(MessageText.Format(ReviewUserText.ReviewerBlank));
                if (request.Reviewer.Trim().Length > MaxReviewerLength)
                    return Bad(MessageText.Format(ReviewUserText.ReviewerTooLong, new { Limit = MaxReviewerLength, Length = request.Reviewer.Trim().Length }));
                if ((request.Note ?? "").Length > MaxNoteLength)
                    return Bad(MessageText.Format(ReviewUserText.NoteTooLong, new { Limit = MaxNoteLength, Length = request.Note!.Length }));
                var humans = HumanReviewsOf(run);
                if (!humans.Contains(grader))
                    return Bad(MessageText.Format(ReviewUserText.ReviewGraderNotHuman, new { Grader = grader, Id = id, Graders = string.Join(", ", humans.Order(StringComparer.Ordinal)) }));

                var key = new BenchmarkProtocol.ConversationKey(mode, repeat, task);
                if ((await s.ConversationsAsync(id, cancellationToken)).All(c => c.Key != key))
                    return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ReviewUserText.NoStoredConversation, new { Id = id, Mode = mode, Repeat = repeat, Task = task }));

                var grade = new HumanGrade(id, key, grader[..^HumanSuffix.Length], verdict.Value, request.Note ?? "", request.Reviewer.Trim(), DateTimeOffset.UtcNow);
                await r.AddAsync(grade, cancellationToken);
                return Results.Json(Row(grade, latest: true));
            }));

        admin.MapGet("/benchmarks/runs/{id}/reviews", (string id, CancellationToken cancellationToken) => Guarded(store, reviews, async (s, r) =>
        {
            if (await s.GetRunAsync(id, cancellationToken) is null) return NoRun(id);
            var all = await r.OfRunAsync(id, cancellationToken);
            var latest = HumanGrades.Latest(all).ToHashSet();
            return Results.Json(new { reviews = all.Select(g => Row(g, latest.Contains(g))) });
        }));

        admin.MapGet("/benchmarks/runs/{id}/labels", (string id, string? grader, CancellationToken cancellationToken) => Guarded(store, reviews, async (s, r) =>
        {
            if (await s.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var humans = HumanReviewsOf(run);
            if (grader is null || !humans.Contains(grader + HumanSuffix))
                return Bad(MessageText.Format(ReviewUserText.LabelsNeedAGrader, new { Graders = string.Join(", ", humans.Select(h => h[..^HumanSuffix.Length]).Order(StringComparer.Ordinal)) }));

            var decided = HumanGrades.Decided(await r.OfRunAsync(id, cancellationToken)).Where(g => g.Grader == grader).ToList();
            if (decided.Count == 0)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ReviewUserText.NoLabelsYet, new { Grader = grader, Id = id }));

            var labels = new List<(HumanGrade, Turn)>();
            foreach (var grade in decided)
                if ((await s.TurnsAsync(id, grade.Conversation, cancellationToken)).OrderBy(t => t.Key.Turn).LastOrDefault() is { } last) labels.Add((grade, last));
            return Results.File(ReviewLabels.Zip(labels), "application/zip", $"{grader}-labels-{id}.zip");
        }));
    }

    /// <summary>The human reviews a run has, by name: the run's Human graders.</summary>
    internal static HashSet<string> HumanReviewsOf(Run run) =>
        (run.Graders ?? []).Where(g => g.Kind == BenchmarkGraderKind.Human && g.Name.EndsWith(HumanSuffix, StringComparison.Ordinal)).Select(g => g.Name).ToHashSet();

    /// <summary>A grade as the portal reads it: the review's own name, the model grader it answers, and whether it is its reviewer's latest.</summary>
    internal static object Row(HumanGrade g, bool latest) => new
    {
        mode = g.Conversation.Mode, repeat = g.Conversation.Repeat, task = g.Conversation.Task, grader = g.Grader + HumanSuffix, model_grader = g.Grader,
        verdict = g.Verdict.ToString().ToLowerInvariant(), note = g.Note, reviewer = g.Reviewer, at = g.At, latest,
    };

    private static async Task<IResult> Guarded(IBenchmarkStore? store, IHumanReviewStore? reviews, Func<IBenchmarkStore, IHumanReviewStore, Task<IResult>> work)
    {
        if (store is null || reviews is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            return await work(store, reviews);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", AdminEndpoints.Unavailable(ex));
        }
    }

    private static IResult Bad(string message) => ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);

    private static IResult NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
}
