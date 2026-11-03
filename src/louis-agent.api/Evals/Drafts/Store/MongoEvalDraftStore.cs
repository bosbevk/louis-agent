using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Driver;

namespace LouisAgent.Api.Evals.Drafts.Store;

/// <summary>
/// Drafts kept in MongoDB: one collection, <c>eval_drafts</c>, in the eval store's database, one document per draft,
/// its id the document's own and a <c>kind</c> field saying which kind it is.
/// </summary>
/// <remarks>
/// Repository over typed collections (ADR 0048): the store opens two typed views of the one collection, a sample's
/// <see cref="SampleDraftDocument"/> and a transcript's <see cref="TranscriptDraftDocument"/>, and each reads only the
/// documents of its kind, so the driver needs no polymorphism (Louis's pick). A delete goes by the id alone, whichever
/// kind it is. It opens the database as the eval store does (<see cref="MongoEvalDatabase.Open"/>), with the same
/// short timeouts and mappings; the index on eval then time serves both views and is made on first use.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public sealed class MongoEvalDraftStore : IEvalDraftStore
{
    /// <summary>The collection both kinds share.</summary>
    private const string Collection = "eval_drafts";

    /// <summary>The collection, read as sample drafts.</summary>
    private readonly IMongoCollection<SampleDraftDocument> _samples;

    /// <summary>The same collection, read as transcript drafts.</summary>
    private readonly IMongoCollection<TranscriptDraftDocument> _transcripts;

    /// <summary>Makes the index the first time anything needs it, and tries again if that failed.</summary>
    private readonly OnceItWorks _index;

    /// <summary>Connects to the database, with short timeouts so a missing server fails fast.</summary>
    public MongoEvalDraftStore(string url, string database)
    {
        var db = MongoEvalDatabase.Open(url, database);
        _samples = db.GetCollection<SampleDraftDocument>(Collection);
        _transcripts = db.GetCollection<TranscriptDraftDocument>(Collection);

        // An eval's drafts, oldest first, is the one query; both kinds name the two fields the same way.
        _index = new OnceItWorks(() => _samples.Indexes.CreateOneAsync(new CreateIndexModel<SampleDraftDocument>(
            Builders<SampleDraftDocument>.IndexKeys.Ascending(d => d.Eval).Ascending(d => d.At))));
    }

    /// <summary>The database itself, for the tests to drop theirs.</summary>
    internal IMongoDatabase Database => _samples.Database;

    /// <summary>Writes the sample draft as a new document.</summary>
    public async Task AddAsync(SampleDraft draft, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _samples.InsertOneAsync(SampleDraftDocument.Of(draft), cancellationToken: cancellationToken);
    }

    /// <summary>Writes the transcript draft as a new document.</summary>
    public async Task AddAsync(TranscriptDraft draft, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        await _transcripts.InsertOneAsync(TranscriptDraftDocument.Of(draft), cancellationToken: cancellationToken);
    }

    /// <summary>Reads the eval's drafts back, each kind oldest first.</summary>
    public async Task<EvalDrafts> OfEvalAsync(string eval, CancellationToken cancellationToken = default)
    {
        await _index.RunAsync();
        var samples = await _samples.Find(d => d.Eval == eval && d.Kind == DraftKind.Sample).SortBy(d => d.At)
            .ToListAsync(cancellationToken);
        var transcripts = await _transcripts.Find(d => d.Eval == eval && d.Kind == DraftKind.Transcript)
            .SortBy(d => d.At).ToListAsync(cancellationToken);
        return new EvalDrafts([.. samples.Select(d => d.ToDraft())], [.. transcripts.Select(d => d.ToDraft())]);
    }

    /// <summary>Reads the sample draft with this id; a transcript's id finds none.</summary>
    public async Task<SampleDraft?> GetSampleAsync(string id, CancellationToken cancellationToken = default) =>
        (await _samples.Find(d => d.Id == id && d.Kind == DraftKind.Sample).FirstOrDefaultAsync(cancellationToken))
        ?.ToDraft();

    /// <summary>Reads the transcript draft with this id; a sample's id finds none.</summary>
    public async Task<TranscriptDraft?> GetTranscriptAsync(string id, CancellationToken cancellationToken = default) =>
        (await _transcripts.Find(d => d.Id == id && d.Kind == DraftKind.Transcript)
            .FirstOrDefaultAsync(cancellationToken))?.ToDraft();

    /// <summary>Deletes the draft with this id, whichever kind it is.</summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        (await _samples.DeleteOneAsync(d => d.Id == id, cancellationToken)).DeletedCount > 0;
}
