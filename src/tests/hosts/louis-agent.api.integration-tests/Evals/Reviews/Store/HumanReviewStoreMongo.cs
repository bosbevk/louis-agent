using LouisAgent.Api.Evals.Reviews.Store;

namespace LouisAgent.Api.IntegrationTests.Evals.Reviews.Store;

/// <summary>
/// Runs the review store's shared tests against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set. It gets a
/// database of its own and drops it at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and asks this class only for a store, so the Mongo store
/// passes exactly the tests the in-memory one does, and a new store is one small subclass.
/// </remarks>
[Category("Integration")]
public class MongoHumanReviewStoreTests : HumanReviewStoreContract
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_reviews";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoHumanReviewStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IHumanReviewStore CreateStore() => new MongoHumanReviewStore(Url!, Database);
}
