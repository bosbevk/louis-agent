namespace LouisAgent.Api.Evals.Drafts.Store;

/// <summary>
/// Where the drafts wait until F49.5 publishes them: in memory for the tests, in MongoDB for Louis's runs.
/// </summary>
/// <remarks>
/// Repository (Fowler, PoEAA), the shape of the review store, beside it. A draft is found and deleted by its id alone;
/// a get names its kind, since whoever reads one knows which it wants (Louis's pick). <c>EvalDraftStoreContract</c>
/// holds every store to the same behaviour.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public interface IEvalDraftStore
{
    /// <summary>Adds a sample draft.</summary>
    Task AddAsync(SampleDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Adds a transcript draft.</summary>
    Task AddAsync(TranscriptDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Every draft of the eval, oldest first, samples and transcripts each in their list.</summary>
    Task<EvalDrafts> OfEvalAsync(string eval, CancellationToken cancellationToken = default);

    /// <summary>The sample draft with this id, if there is one.</summary>
    Task<SampleDraft?> GetSampleAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>The transcript draft with this id, if there is one.</summary>
    Task<TranscriptDraft?> GetTranscriptAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the draft with this id, whichever kind it is, and says whether there was one.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
