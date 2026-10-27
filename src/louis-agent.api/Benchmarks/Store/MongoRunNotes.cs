using MongoDB.Bson;
using MongoDB.Driver;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// Keeps the notes in Mongo, one document per note, in a collection of their own.
/// </summary>
/// <remarks>
/// Notes can't live in the run's document: the recorder replaces that whole document as the run grows and would wipe them. It is also a class of its own so `MongoBenchmarkStore`, already over the size limit, stays about the run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal sealed class MongoRunNotes(
    IMongoCollection<BsonDocument> notes,
    Func<string, CancellationToken, Task<bool>> runExists)
{
    /// <summary>Saves a note if the text is fine and the run exists, otherwise says why not. Mongo's ids rise with each insert, which is how notes come back in the order written.</summary>
    public async Task<NoteResult> AddAsync(string run, string? text, DateTimeOffset at, ConversationKey? conversation,
        CancellationToken cancellationToken)
    {
        // Check the text first: no need to ask Mongo about a note we would turn down anyway.
        var made = RunNotes.Make(text, at, conversation);
        if (made is not NoteResult.Added added) return made;

        // A note needs a run to belong to.
        if (!await runExists(run, cancellationToken)) return new NoteResult.NoRun();

        // Its own document, apart from the run's.
        await notes.InsertOneAsync(ToBson(run, added.Note), cancellationToken: cancellationToken);
        return made;
    }

    /// <summary>All of a run's notes, oldest first.</summary>
    public async Task<IReadOnlyList<RunNote>> OfRunAsync(string run, CancellationToken cancellationToken)
    {
        // Sorting on the id gives the order written, because Mongo's ids rise with each insert.
        var found = await notes.Find(Builders<BsonDocument>.Filter.Eq("run", run))
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id")).ToListAsync(cancellationToken);
        return found.Select(NoteOf).ToArray();
    }

    /// <summary>Removes a run's notes when the run goes.</summary>
    public async Task<long> DeleteRunAsync(string run, CancellationToken cancellationToken) =>
        (await notes.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("run", run), cancellationToken)).DeletedCount;

    /// <summary>Makes finding a run's notes fast.</summary>
    public Task CreateIndexAsync() =>
        notes.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("run")));

    /// <summary>A note as a Mongo document.</summary>
    private static BsonDocument ToBson(string run, RunNote note) => new()
    {
        ["run"] = run,
        ["text"] = note.Text,
        ["at"] = new BsonDateTime(note.At.UtcDateTime),
        ["mode"] = note.Conversation is { } c ? c.Mode : BsonNull.Value,
        ["repeat"] = note.Conversation is { } r ? r.Repeat : BsonNull.Value,
        ["task"] = note.Conversation is { } t ? t.Task : BsonNull.Value,
    };

    /// <summary>A document turned back into the note it was.</summary>
    private static RunNote NoteOf(BsonDocument d) => new(d["text"].AsString,
        new DateTimeOffset(d["at"].ToUniversalTime(), TimeSpan.Zero),
        d["mode"].IsBsonNull ? null : new ConversationKey(d["mode"].AsString, d["repeat"].AsInt32, d["task"].AsString));
}
