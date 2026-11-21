using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Evals.Messages;
using LouisAgent.Composition.Messages;
using LouisAgent.Api.Evals.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Evals.Endpoints.Runs;
using LouisAgent.Api.Evals.Reviews.Queue;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using QueueAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Reviews.ReviewQueueReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Evals.Endpoints.Reviews.Queue;

/// <summary>
/// Answers the review queue (F53.3): the answers waiting for a person across the stored runs, for one reviewer or
/// for nobody, the first so many as items and the total.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), as <c>ReviewHandlers</c>: the stores aren't services, so they live here and
/// the route is a named method a test can call with no HTTP, returning <c>TypedResults</c> with its answers declared.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class ReviewQueueHandlers(IEvalStore? store, IHumanReviewStore? reviews)
{
    /// <summary>How many items the queue hands back when the page doesn't say, and the most it ever will.</summary>
    internal const int DefaultLimit = 20, MaxLimit = 100;

    /// <summary>
    /// The answers waiting for <paramref name="reviewer"/> (nobody when blank), up to <paramref name="limit"/>
    /// items, with the total.
    /// </summary>
    public async Task<QueueAnswer> GetQueue(string? reviewer, int? limit, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null) return NoStore();
        try
        {
            string? name = string.IsNullOrWhiteSpace(reviewer) ? null : reviewer.Trim();
            int take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
            var (items, total) = await ReviewQueue.ListAsync(store, reviews, name, take, cancellationToken);
            var reply = new ReviewQueueReply([.. items.Select(ReviewQueueItem.Of)], total);
            return TypedResults.Json(reply, AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(EvalAdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The 503 when there's no store at all.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Unavailable(MessageText.Format(EvalApiUserText.NoBenchmarkStore));
}
