using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Reviews.Store;

/// <summary>Runs the review store's shared tests against the in-memory store, on every machine and on CI.</summary>
public class InMemoryHumanReviewStoreTests : HumanReviewStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IHumanReviewStore CreateStore() => new InMemoryHumanReviewStore();
}
