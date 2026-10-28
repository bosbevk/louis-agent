using MongoDB.Bson;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Opens the eval database with short timeouts: a server that doesn't answer fails a call within five seconds, so the
/// recorder gives up quickly and the run carries on. It also holds the few bits every record store asks Mongo the
/// same way: replace-or-insert, find by id, find a run's documents.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public static class MongoEvalDatabase
{
    /// <summary>How long a call waits for the server before giving up.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Replace the document if there is one, otherwise insert it.</summary>
    internal static readonly ReplaceOptions Upsert = new() { IsUpsert = true };

    /// <summary>The database on the server at the address. Nothing is contacted until the first call.</summary>
    public static IMongoDatabase Open(string url, string database)
    {
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        return new MongoClient(settings).GetDatabase(database);
    }

    /// <summary>Finds the document with this id.</summary>
    internal static FilterDefinition<BsonDocument> ById(BsonValue id) => Builders<BsonDocument>.Filter.Eq("_id", id);

    /// <summary>Finds everything that belongs to this run.</summary>
    internal static FilterDefinition<BsonDocument> OfRun(string run) => Builders<BsonDocument>.Filter.Eq("run", run);
}
