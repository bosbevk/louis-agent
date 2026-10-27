using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// People's grades of a stored run (F18 step 6a.2, ADR 0039: a resource, <c>reviews</c>, under the run): add one, list
/// the run's, and export a judge's labels. They belong to the same <c>/admin</c> group as the rest, so the key gate
/// covers them; the work is <c>ReviewHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class ReviewEndpoints
{
    /// <summary>Adds the review routes to the admin group.</summary>
    public static void MapReviews(this RouteGroupBuilder admin, IBenchmarkStore? store, IHumanReviewStore? reviews)
    {
        var handlers = new ReviewHandlers(store, reviews);

        admin.MapPut("/benchmarks/runs/{id}/reviews/{mode}/{repeat:int}/{task}/{grader}", handlers.PutReview);
        admin.MapGet("/benchmarks/runs/{id}/reviews", handlers.GetReviews);
        admin.MapGet("/benchmarks/runs/{id}/labels", handlers.GetLabels);
    }
}
