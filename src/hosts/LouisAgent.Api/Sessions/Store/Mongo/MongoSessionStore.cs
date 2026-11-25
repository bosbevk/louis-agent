using MongoDB.Driver;

namespace LouisAgent.Api.Sessions.Store.Mongo;

/// <summary>
/// Keeps chats in MongoDB, so a restart doesn't end them (F43.1): how each opened in <c>sessions</c>, each finished
/// turn in <c>turns</c>. Every turn moves its chat's last activity on, in both collections, and a TTL index on it in
/// each deletes a chat and its turns together once it has been idle for the retention.
/// </summary>
/// <remarks>
/// One of the strategies behind <see cref="ISessionStore" />, on typed collections (ADR 0048). The indexes are made
/// once, on the first call; a retention changed since the last run replaces the TTL index it had.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed class MongoSessionStore : ISessionStore
{
    /// <summary>The field both collections' TTL index reads.</summary>
    private const string LastActivity = "last_activity";

    /// <summary>The name of each collection's TTL index, so a changed retention finds it to replace.</summary>
    private const string Retention = "last_activity_ttl";

    /// <summary>The errors MongoDB answers when an index of that name or keys stands with other options.</summary>
    private static readonly int[] IndexConflicts = [85, 86];

    private readonly IMongoCollection<SessionDocument> _sessions;
    private readonly IMongoCollection<TurnDocument> _turns;
    private readonly TimeSpan _retention;
    private readonly Lazy<Task> _indexes;

    /// <summary>A store on this database, keeping an idle chat for <paramref name="retention" />.</summary>
    public MongoSessionStore(IMongoDatabase database, TimeSpan retention)
    {
        _sessions = database.GetCollection<SessionDocument>("sessions");
        _turns = database.GetCollection<TurnDocument>("turns");
        _retention = retention;
        _indexes = new Lazy<Task>(MakeIndexesAsync);
    }

    /// <inheritdoc/>
    public async Task SaveOpeningAsync(StoredSession session, CancellationToken cancellationToken = default)
    {
        await _indexes.Value;
        var document = SessionDocument.Of(session);
        await _sessions.ReplaceOneAsync(stored => stored.Id == document.Id, document,
            new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AppendTurnAsync(string sessionId, StoredTurn turn, CancellationToken cancellationToken = default)
    {
        await _indexes.Value;
        var at = turn.At.UtcDateTime;
        await _turns.InsertOneAsync(TurnDocument.Of(sessionId, turn), cancellationToken: cancellationToken);

        // The chat's last activity moves on in both collections, so the TTL index keeps its turns as long as the chat.
        await _sessions.UpdateOneAsync(session => session.Id == sessionId,
            Builders<SessionDocument>.Update.Set(session => session.LastActivity, at),
            cancellationToken: cancellationToken);
        await _turns.UpdateManyAsync(stored => stored.SessionId == sessionId,
            Builders<TurnDocument>.Update.Set(stored => stored.LastActivity, at), cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SavedSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _indexes.Value;
        var session = await _sessions.Find(stored => stored.Id == sessionId).FirstOrDefaultAsync(cancellationToken);
        if (session is null) return null;

        var turns = await _turns.Find(stored => stored.SessionId == sessionId).SortBy(stored => stored.Number)
            .ToListAsync(cancellationToken);
        return new SavedSession(session.ToStored(), [.. turns.Select(turn => turn.ToStored())]);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _indexes.Value;
        await _sessions.DeleteOneAsync(stored => stored.Id == sessionId, cancellationToken);
        await _turns.DeleteManyAsync(stored => stored.SessionId == sessionId, cancellationToken);
    }

    /// <summary>The TTL index on each collection, and the index a chat's turns are read in order by.</summary>
    private async Task MakeIndexesAsync()
    {
        await RetainAsync(_sessions);
        await RetainAsync(_turns);
        var byChat = Builders<TurnDocument>.IndexKeys.Ascending(turn => turn.SessionId).Ascending(turn => turn.Number);
        await _turns.Indexes.CreateOneAsync(new CreateIndexModel<TurnDocument>(byChat));
    }

    /// <summary>
    /// A collection's TTL index on its last activity, replaced when it stands with another retention.
    /// </summary>
    private async Task RetainAsync<T>(IMongoCollection<T> collection)
    {
        var ttl = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(LastActivity),
            new CreateIndexOptions { Name = Retention, ExpireAfter = _retention });
        try
        {
            await collection.Indexes.CreateOneAsync(ttl);
        }
        catch (MongoCommandException conflict) when (IndexConflicts.Contains(conflict.Code))
        {
            await collection.Indexes.DropOneAsync(Retention);
            await collection.Indexes.CreateOneAsync(ttl);
        }
    }
}
