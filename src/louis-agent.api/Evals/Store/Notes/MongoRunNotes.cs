using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Driver;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Notes;

/// <summary>
/// Keeps the notes in Mongo, one document per note, in a collection of their own.
/// </summary>
/// <remarks>
/// Extract Class (Fowler), for one concern. Notes can't live in the run's document: the recorder replaces that whole
/// document as the run grows and would wipe them. The collection is typed (ADR 0048): each note is a
/// <see cref="NoteDocument"/>, and the filter, sort and index name their fields through it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed class MongoRunNotes(
    IMongoCollection<NoteDocument> notes,
    Func<string, CancellationToken, Task<bool>> runExists)
{
    /// <summary>
    /// Saves a note if the text is fine and the run exists, otherwise says why not. Each note's id is later than the
    /// one before, which is how notes come back in the order written.
    /// </summary>
    /// <remarks>Guard clauses: each reason not to save returns first, so the insert is the last step.</remarks>
    public async Task<NoteResult> AddAsync(string run, string? text, DateTimeOffset at, ConversationKey? conversation,
        CancellationToken cancellationToken)
    {
        // Check the text first: no need to ask Mongo about a note we would turn down anyway.
        var made = RunNotes.Make(text, at, conversation);
        if (made is not NoteResult.Added added) return made;

        // A note needs a run to belong to.
        if (!await runExists(run, cancellationToken)) return new NoteResult.NoRun();

        // Its own document, apart from the run's.
        await notes.InsertOneAsync(NoteDocument.Of(run, added.Note), cancellationToken: cancellationToken);
        return made;
    }

    /// <summary>All of a run's notes, oldest first.</summary>
    public async Task<IReadOnlyList<RunNote>> OfRunAsync(string run, CancellationToken cancellationToken)
    {
        // Sorting on the id gives the order written, because each note's id is later than the one before.
        var found = await notes.Find(d => d.Run == run).SortBy(d => d.Id).ToListAsync(cancellationToken);
        return [.. found.Select(d => d.ToNote())];
    }

    /// <summary>Removes a run's notes when the run goes.</summary>
    public async Task<long> DeleteRunAsync(string run, CancellationToken cancellationToken) =>
        (await notes.DeleteManyAsync(d => d.Run == run, cancellationToken)).DeletedCount;

    /// <summary>Makes finding a run's notes fast.</summary>
    public Task CreateIndexAsync() =>
        notes.Indexes.CreateOneAsync(
            new CreateIndexModel<NoteDocument>(Builders<NoteDocument>.IndexKeys.Ascending(d => d.Run)));
}
