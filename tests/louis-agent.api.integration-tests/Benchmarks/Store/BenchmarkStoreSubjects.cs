using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Store;

/// <summary>
/// The contract against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set in the test's environment: on Louis's machine
/// the Puls stack's <c>puls-mongo</c> at 127.0.0.1 (set in the shell, not read from <c>.env.secrets</c>), on CI its Mongo
/// service. Without it the fixture says it's inconclusive rather than passing. Its database is
/// <c>puls_benchmarks_test</c> (or <c>BENCHMARKS_MONGO_DATABASE</c>), dropped when the fixture ends.
/// </summary>
[Category("Integration")]
public class MongoBenchmarkStoreTests : BenchmarkStoreContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private static string Database =>
        Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_DATABASE") is { Length: > 0 } name ? name : "puls_benchmarks_test";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        var store = new MongoBenchmarkStore(Url, Database);
        await store.Database.Client.DropDatabaseAsync(Database);
    }

    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}

/// <summary>The same against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set; its own database, dropped when the fixture ends.</summary>
[Category("Integration")]
public class MongoRunFieldsTests : BenchmarkRunFieldsContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private const string Database = "puls_benchmarks_test_fields";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoBenchmarkStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}
