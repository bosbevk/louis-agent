using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Calibrations;

/// <summary>The contract against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (as the review store's is): its own database, dropped when the fixture ends.</summary>
[Category("Integration")]
public class MongoCalibrationStoreTests : CalibrationStoreContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private const string Database = "puls_benchmarks_test_calibrations";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoCalibrationStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    protected override ICalibrationStore CreateStore() => new MongoCalibrationStore(Url!, Database);
}
