using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;

namespace LouisAgent.Api.IntegrationTests.Evals.Store;

/// <summary>
/// Runs the run store's shared tests against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set: Louis's Puls
/// stack on his machine, the Mongo service on CI. Without it they skip. They get a database of their own, dropped at
/// the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and asks this class only for a store, so the Mongo store
/// passes exactly the tests the in-memory one does, and a new store is one small subclass.
/// </remarks>
[Category("Integration")]
public class MongoEvalStoreTests : EvalStoreContract
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>The database these tests use: the one the environment names, else their own default.</summary>
    private static string Database =>
        Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_DATABASE") is { Length: > 0 } name
            ? name
            : "puls_benchmarks_test";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to (CI fails instead).</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        var store = new MongoEvalStore(Url, Database);
        await store.Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IEvalStore CreateStore() => new MongoEvalStore(Url!, Database);
}

/// <summary>
/// Runs the shared tests for a run's own fields against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set, in a
/// database of their own that is dropped at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and this class only supplies the store.
/// </remarks>
[Category("Integration")]
public class MongoRunFieldsTests : EvalRunFieldsContract
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_fields";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to (CI fails instead).</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoEvalStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IEvalStore CreateStore() => new MongoEvalStore(Url!, Database);
}

/// <summary>
/// Runs the shared notes tests against a real MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set, in a database of
/// their own that is dropped at the end.
/// </summary>
/// <remarks>
/// Template Method: the contract base class holds the tests and this class only supplies the store.
/// </remarks>
[Category("Integration")]
public class MongoNotesTests : EvalNotesContract
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_benchmarks_test_notes";

    /// <summary>Skips the whole fixture when there's no Mongo to talk to (CI fails instead).</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoEvalStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>Hands the shared tests a Mongo store on the test database.</summary>
    protected override IEvalStore CreateStore() => new MongoEvalStore(Url!, Database);
}
