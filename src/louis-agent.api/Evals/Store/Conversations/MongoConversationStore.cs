using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Conversations;

/// <summary>
/// Keeps the conversations in MongoDB's <c>conversations</c> collection, one document per conversation. Its index (by
/// run, in order) is made the first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository, with a Data Mapper (<see cref="ConversationDocuments"/>) written by hand, so what <c>mongosh</c> shows
/// is what the portal reads and the scores stay the JSON they came as. This class keeps only the collection and its
/// queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class MongoConversationStore : IConversationStore
{
    /// <summary>The conversations.</summary>
    private readonly IMongoCollection<BsonDocument> _conversations;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's conversations collection. Nothing is created until the first call.</summary>
    public MongoConversationStore(IMongoDatabase database)
    {
        _conversations = database.GetCollection<BsonDocument>("conversations");
        _index = new OnceItWorks(() => _conversations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("run").Ascending("order"))));
    }

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public async Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _conversations.ReplaceOneAsync(ById(ConversationDocuments.Id(run, conversation.Key)),
            ConversationDocuments.ToBson(run, conversation), Upsert, cancellationToken);
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public async Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _conversations.Find(OfRun(run)).Sort(Builders<BsonDocument>.Sort.Ascending("order"))
            .ToListAsync(cancellationToken);
        return found.Select(ConversationDocuments.ConversationOf).ToArray();
    }

    /// <summary>Deletes the run's conversations, and says whether there were any.</summary>
    public async Task<bool> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _conversations.DeleteManyAsync(OfRun(run), cancellationToken)).DeletedCount > 0;
    }
}
