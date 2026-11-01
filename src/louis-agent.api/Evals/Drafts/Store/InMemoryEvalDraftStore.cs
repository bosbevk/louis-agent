namespace LouisAgent.Api.Evals.Drafts.Store;

/// <summary>
/// Drafts kept in memory: what the tests run against, and the reference the store contract is checked on. One lock
/// guards both lists.
/// </summary>
public sealed class InMemoryEvalDraftStore : IEvalDraftStore
{
    private readonly Lock _lock = new();
    private readonly List<SampleDraft> _samples = [];
    private readonly List<TranscriptDraft> _transcripts = [];

    /// <summary>Adds the sample draft to its list.</summary>
    public Task AddAsync(SampleDraft draft, CancellationToken cancellationToken = default)
    {
        lock (_lock) _samples.Add(draft);
        return Task.CompletedTask;
    }

    /// <summary>Adds the transcript draft to its list.</summary>
    public Task AddAsync(TranscriptDraft draft, CancellationToken cancellationToken = default)
    {
        lock (_lock) _transcripts.Add(draft);
        return Task.CompletedTask;
    }

    /// <summary>The eval's drafts, oldest first, copied out so nobody holds the lock.</summary>
    public Task<EvalDrafts> OfEvalAsync(string eval, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult(new EvalDrafts(
                [.. _samples.Where(d => d.Eval == eval).OrderBy(d => d.At)],
                [.. _transcripts.Where(d => d.Eval == eval).OrderBy(d => d.At)]));
    }

    /// <summary>The sample draft with this id.</summary>
    public Task<SampleDraft?> GetSampleAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_samples.FirstOrDefault(d => d.Id == id));
    }

    /// <summary>The transcript draft with this id.</summary>
    public Task<TranscriptDraft?> GetTranscriptAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_transcripts.FirstOrDefault(d => d.Id == id));
    }

    /// <summary>Drops the draft with this id from whichever list has it.</summary>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult(_samples.RemoveAll(d => d.Id == id) + _transcripts.RemoveAll(d => d.Id == id) > 0);
    }
}
