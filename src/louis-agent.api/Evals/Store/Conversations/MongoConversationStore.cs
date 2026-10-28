using LouisAgent.Api.Evals.Store.Mongo.Documents;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Store.Mongo.MongoEvalDatabase;

namespace LouisAgent.Api.Evals.Store.Conversations;

/// <summary>
/// Keeps the conversations in MongoDB's <c>conversations</c> collection, one document per conversation. Its index (by
/// run, in order) is made the first time anything needs it, so starting the API never waits on Mongo.
/// </summary>
/// <remarks>
/// Repository over a typed collection (ADR 0048): the driver maps each conversation's
/// <see cref="ConversationDocument"/>, and every filter, sort and index names its field through the record, so a
/// renamed field fails the build. This class keeps only the collection and its queries.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoConversationStore : IConversationStore
{
    /// <summary>The conversations.</summary>
    private readonly IMongoCollection<ConversationDocument> _conversations;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Uses the database's conversations collection. Nothing is created until the first call.</summary>
    public MongoConversationStore(IMongoDatabase database)
    {
        // The driver must know how enums and times are stored before it maps its first document.
        RegisterMappings();
        _conversations = database.GetCollection<ConversationDocument>("conversations");
        _index = new OnceItWorks(() => _conversations.Indexes.CreateOneAsync(
            new CreateIndexModel<ConversationDocument>(
                Builders<ConversationDocument>.IndexKeys.Ascending(d => d.Run).Ascending(d => d.Order))));
    }

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public async Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var document = ConversationDocument.Of(run, conversation);
        await _conversations.ReplaceOneAsync(Builders<ConversationDocument>.Filter.Eq(d => d.Id, document.Id),
            document, Upsert, cancellationToken);
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public async Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var found = await _conversations.Find(d => d.Run == run).SortBy(d => d.Order).ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToConversation())];
    }

    /// <summary>Deletes the run's conversations, and says whether there were any.</summary>
    public async Task<bool> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        return (await _conversations.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount > 0;
    }
}
