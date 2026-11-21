using LouisAgent.Api.Evals.Endpoints.Reviews.Queue;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Evals.Endpoints.Reviews;

/// <summary>
/// Hangs the routes for people's grades of a stored run, a <c>reviews</c> resource under the run (ADR 0039): add one,
/// list the run's, and export a judge's labels; and the queue of answers waiting across runs (F53.3). They sit in
/// the admin group, so the key covers them; the work is in <c>ReviewHandlers</c> and <c>ReviewQueueHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class ReviewEndpoints
{
    /// <summary>Adds the review routes to the group the eval routes hang on.</summary>
    public static void MapReviews(this RouteGroupBuilder evals, IEvalStore? store, IHumanReviewStore? reviews)
    {
        var handlers = new ReviewHandlers(store, reviews);
        var queue = new ReviewQueueHandlers(store, reviews);

        evals.MapPut("/runs/{id}/reviews/{mode}/{repeat:int}/{task}/{grader}", handlers.PutReview);
        evals.MapGet("/runs/{id}/reviews", handlers.GetReviews);
        evals.MapGet("/runs/{id}/labels", handlers.GetLabels);
        evals.MapGet("/reviews/queue", queue.GetQueue);
    }
}
