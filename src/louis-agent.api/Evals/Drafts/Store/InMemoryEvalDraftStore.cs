using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Transcripts;

namespace LouisAgent.Api.Evals.Drafts.Store;

/// <summary>
/// Drafts kept in memory: what the tests run against, and the reference the store contract is checked on. One lock
/// guards the three lists.
/// </summary>
public sealed class InMemoryEvalDraftStore : IEvalDraftStore
{
    private readonly Lock _lock = new();
    private readonly List<SampleDraft> _samples = [];
    private readonly List<TranscriptDraft> _transcripts = [];
    private readonly List<EvalDraft> _evals = [];

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

    /// <summary>Adds the eval draft to its list.</summary>
    public Task AddAsync(EvalDraft draft, CancellationToken cancellationToken = default)
    {
        lock (_lock) _evals.Add(draft);
        return Task.CompletedTask;
    }

    /// <summary>Puts this draft in the place of the one with its id.</summary>
    public Task<bool> ReplaceAsync(EvalDraft draft, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            int at = _evals.FindIndex(d => d.Id == draft.Id);
            if (at < 0) return Task.FromResult(false);
            _evals[at] = draft;
            return Task.FromResult(true);
        }
    }

    /// <summary>The eval draft with this id.</summary>
    public Task<EvalDraft?> GetEvalAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_evals.FirstOrDefault(d => d.Id == id));
    }

    /// <summary>Every eval draft, oldest first, copied out so nobody holds the lock.</summary>
    public Task<IReadOnlyList<EvalDraft>> EvalsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<EvalDraft>>([.. _evals.OrderBy(d => d.At)]);
    }

    /// <summary>Drops the draft with this id from whichever list has it.</summary>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult(_samples.RemoveAll(d => d.Id == id) + _transcripts.RemoveAll(d => d.Id == id) +
                _evals.RemoveAll(d => d.Id == id) > 0);
    }
}
