using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Reviews.Store;

/// <summary>The contract against the in-memory store: always, on every machine and on CI.</summary>
public class InMemoryHumanReviewStoreTests : HumanReviewStoreContract
{
    protected override IHumanReviewStore CreateStore() => new InMemoryHumanReviewStore();
}
