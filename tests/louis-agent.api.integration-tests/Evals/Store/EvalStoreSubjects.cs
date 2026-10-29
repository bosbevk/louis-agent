using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;

namespace LouisAgent.Api.IntegrationTests.Evals.Store;

/// <summary>
/// Runs the whole store's shared tests against a real MongoDB (<see cref="TestMongo"/>), in a database of their own
/// that is dropped at the end: what reaches across the records, and the records not yet in a store of their own.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and asks this class only for a store, so the Mongo store
/// passes exactly the tests the in-memory one does, and a new store is one small subclass.
/// </remarks>
[Category("Integration")]
public class MongoEvalStoreTests : EvalStoreContract
{
    /// <summary>The database these tests use: the one the environment names, else their own default.</summary>
    private static string Database =>
        Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_DATABASE") is { Length: > 0 } name
            ? name
            : "puls_benchmarks_test";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() => TestMongo.Needed();

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public Task DropTheTestDatabase() => TestMongo.DropAsync(Database);

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IEvalStore CreateStore() => new MongoEvalStore(TestMongo.Url!, Database);
}

/// <summary>
/// Runs the shared notes tests against a real MongoDB (<see cref="TestMongo"/>), in a database of their own that is
/// dropped at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and this class only supplies the store.
/// </remarks>
[Category("Integration")]
public class MongoNotesTests : EvalNotesContract
{
    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_notes";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() => TestMongo.Needed();

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public Task DropTheTestDatabase() => TestMongo.DropAsync(Database);

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IEvalStore CreateStore() => new MongoEvalStore(TestMongo.Url!, Database);
}
