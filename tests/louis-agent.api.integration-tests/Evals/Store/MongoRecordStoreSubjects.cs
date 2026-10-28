using LouisAgent.Api.Evals.Store.Runs;

namespace LouisAgent.Api.IntegrationTests.Evals.Store;

/// <summary>
/// Runs the run store's shared tests against a real MongoDB (<see cref="TestMongo"/>), in a database of their own that
/// is dropped at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and this class only supplies the store, so the MongoDB
/// runs pass exactly the tests the in-memory ones do.
/// </remarks>
[Category("Integration")]
public class MongoRunStoreTests : RunStoreContract
{
    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_runs";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() => TestMongo.Needed();

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public Task DropTheTestDatabase() => TestMongo.DropAsync(Database);

    /// <summary>Hands the shared tests the runs on the test database.</summary>
    protected override IRunStore CreateStore() => new MongoRunStore(TestMongo.Open(Database));
}

/// <summary>
/// Runs the shared tests for a run's own fields against a real MongoDB (<see cref="TestMongo"/>), in a database of
/// their own that is dropped at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and this class only supplies the store.
/// </remarks>
[Category("Integration")]
public class MongoRunFieldsTests : RunFieldsContract
{
    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_fields";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() => TestMongo.Needed();

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public Task DropTheTestDatabase() => TestMongo.DropAsync(Database);

    /// <summary>Hands the shared tests the runs on the test database.</summary>
    protected override IRunStore CreateStore() => new MongoRunStore(TestMongo.Open(Database));
}
