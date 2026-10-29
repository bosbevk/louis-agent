using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Driver;

namespace LouisAgent.Api.IntegrationTests.Evals.Store;

/// <summary>
/// The MongoDB the store tests talk to, when <c>BENCHMARKS_MONGO_URL</c> is set: Louis's Puls stack on his machine,
/// the Mongo service on CI. Without it the tests skip. Each fixture names a database of its own and drops it at the
/// end, so a test never touches real runs.
/// </summary>
internal static class TestMongo
{
    /// <summary>Where Mongo runs, if this machine has one.</summary>
    public static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    /// <summary>Skips the whole fixture when there's no Mongo to talk to (CI fails instead).</summary>
    public static void Needed() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    /// <summary>The test database by this name, opened as the API opens its own.</summary>
    public static IMongoDatabase Open(string database) => MongoEvalDatabase.Open(Url!, database);

    /// <summary>Drops a test database once its fixture is done; nothing to do without Mongo.</summary>
    public static async Task DropAsync(string database)
    {
        if (string.IsNullOrEmpty(Url)) return;
        await Open(database).Client.DropDatabaseAsync(database);
    }
}
