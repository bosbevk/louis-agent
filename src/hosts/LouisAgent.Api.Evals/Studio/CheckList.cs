namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// The runner's catalogue of checks and readers, asked for the first time the studio wants it and kept once the
/// runner gave it. A failure is never kept, so a runner that wasn't ready is asked again next time.
/// </summary>
/// <remarks>
/// Lazy Load (Fowler, PoEAA), as <c>EvalList</c> is for the evals, except that only a success is cached: the catalogue
/// is the runner's build, which doesn't change while the API runs, but a runner still being copied in shouldn't leave
/// the studio empty until a restart. A test hands in its own read, with no runner at all.
/// </remarks>
public sealed class CheckList(Func<CheckListingResult> read)
{
    private readonly Lock _lock = new();
    private CheckListingResult? _listed;

    /// <summary>The catalogue the runner gave, asking it again only while it hasn't given one yet.</summary>
    public CheckListingResult Read()
    {
        lock (_lock)
        {
            if (_listed is { } listed) return listed;

            var result = read();
            if (result.Failure is null) _listed = result;
            return result;
        }
    }
}
