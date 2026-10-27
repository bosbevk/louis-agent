using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Store;

/// <summary>
/// The store rules, run against MongoDB when `BENCHMARKS_MONGO_URL` is set: Louis's Puls stack at 127.0.0.1 on his machine, the Mongo service on CI. Without it the tests skip. They use a test database of their own, dropped when they end.
/// </summary>
[Category("Integration")]
public class MongoBenchmarkStoreTests : BenchmarkStoreContract
{
    /// <summary>The Mongo address from the environment, if there is one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>The test database's name, from the environment or a default.</summary>
    private static string Database =>
        Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_DATABASE") is { Length: > 0 } name ? name : "puls_benchmarks_test";

    /// <summary>Skips when there's no Mongo to test against, and fails on CI.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Drops the test database when the tests end.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        var store = new MongoBenchmarkStore(Url, Database);
        await store.Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>A store on the test database.</summary>
    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}

/// <summary>
/// The run-fields rules, run against MongoDB when `BENCHMARKS_MONGO_URL` is set, in a test database dropped afterwards.
/// </summary>
[Category("Integration")]
public class MongoRunFieldsTests : BenchmarkRunFieldsContract
{
    /// <summary>The Mongo address from the environment, if there is one.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>The test database these tests use.</summary>
    private const string Database = "puls_benchmarks_test_fields";

    /// <summary>Skips when there's no Mongo to test against, and fails on CI.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Drops the test database when the tests end.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoBenchmarkStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>A store on the test database.</summary>
    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}

/// <summary>The notes rules, run against MongoDB when `BENCHMARKS_MONGO_URL` is set, in a test database that is dropped afterwards.</summary>
[Category("Integration")]
public class MongoNotesTests : BenchmarkNotesContract
{
    /// <summary>The Mongo address from the environment, or null when there is none.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>The test database these tests use.</summary>
    private const string Database = "puls_benchmarks_test_notes";

    /// <summary>Skips locally (fails on CI) when there is no Mongo to test against.</summary>
    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>Drops the fixture's database when it ends.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoBenchmarkStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    /// <summary>A store on the fixture's database.</summary>
    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}
