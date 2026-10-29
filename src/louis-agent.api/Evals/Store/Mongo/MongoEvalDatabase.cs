using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Evals.Protocol.Kinds;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Opens the eval database with short timeouts: a server that doesn't answer fails a call within five seconds, so the
/// recorder gives up quickly and the run carries on. It also registers how enums and times are stored, and holds the
/// replace-or-insert option every record store saves with.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public static class MongoEvalDatabase
{
    /// <summary>How long a call waits for the server before giving up.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Replace the document if there is one, otherwise insert it.</summary>
    internal static readonly ReplaceOptions Upsert = new() { IsUpsert = true };

    /// <summary>The mappings, registered the first time a database is opened, and only then.</summary>
    private static readonly Lazy<bool> Mapped = new(Map, isThreadSafe: true);

    /// <summary>
    /// Registers, once, how the documents' enums and times are stored: each enum by its wire name, each time as a
    /// BSON date in UTC, read back at offset zero.
    /// </summary>
    internal static void RegisterMappings() => _ = Mapped.Value;

    /// <summary>Hands the driver a serializer for every enum a document holds, and the one for times.</summary>
    /// <remarks>
    /// Registered before the first collection, as the driver asks (ADR 0048); a type it already has a serializer for
    /// keeps that one, so a second call can't throw.
    /// </remarks>
    private static bool Map()
    {
        BsonSerializer.TryRegisterSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));
        WireName<EvalRunStatus>();
        WireName<RunSplit>();
        WireName<GraderKind>();
        WireName<ModelLevel>();
        WireName<GradeOutcome>();
        WireName<TrialOutcome>();
        WireName<TurnFailure>();
        WireName<HumanVerdict>();
        return true;
    }

    /// <summary>Stores this enum by its wire name.</summary>
    private static void WireName<TEnum>() where TEnum : struct, Enum =>
        BsonSerializer.TryRegisterSerializer(new WireNameSerializer<TEnum>());

    /// <summary>The database on the server at the address. Nothing is contacted until the first call.</summary>
    public static IMongoDatabase Open(string url, string database)
    {
        RegisterMappings();
        var settings = MongoClientSettings.FromConnectionString(url);
        settings.ServerSelectionTimeout = Timeout;
        settings.ConnectTimeout = Timeout;
        return new MongoClient(settings).GetDatabase(database);
    }
}
