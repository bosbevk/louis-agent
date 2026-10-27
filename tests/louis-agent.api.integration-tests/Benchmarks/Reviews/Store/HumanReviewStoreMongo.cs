using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Reviews.Store;

/// <summary>The contract against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (as the benchmark store's own contract is): its own database, dropped when the fixture ends.</summary>
[Category("Integration")]
public class MongoHumanReviewStoreTests : HumanReviewStoreContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private const string Database = "puls_benchmarks_test_reviews";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoHumanReviewStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    protected override IHumanReviewStore CreateStore() => new MongoHumanReviewStore(Url!, Database);
}
