using LouisAgent.Api.Evals.Reviews.Store;

namespace LouisAgent.Api.Tests.Evals.Reviews.Store;

/// <summary>Runs the review store's shared tests against the in-memory store, on every machine and on CI.</summary>
public class InMemoryHumanReviewStoreTests : HumanReviewStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IHumanReviewStore CreateStore() => new InMemoryHumanReviewStore();
}
