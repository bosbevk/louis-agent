using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The checks the portal can run (F27, F18 step 4.3). The C# evals come from the runner's own listing (<see cref="EvalListing"/>), read on
/// first use. Every check is an eval the runner lists (F18 step 7.4: the Python checks, then their fallback, are gone): a listing that can't be read leaves no checks, and a warning says why.
/// </summary>
public sealed class BenchmarkCatalog
{
    private readonly Lazy<IReadOnlyList<BenchmarkCheck>> _checks;

    /// <param name="checks">The checks; none by default (a test passes its own, with a stand-in script).</param>
    public BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
    {
        var list = checks ?? [];
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(() => list);
    }

    private BenchmarkCatalog(Func<IReadOnlyList<BenchmarkCheck>> checks) =>
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(checks);

    /// <summary>Reads the listing now, so a warning lands in the start-up log rather than on the first request.</summary>
    public void Warm() => _ = _checks.Value;

    /// <summary>The checks the portal offers; the runner is listed the first time this is read.</summary>
    public IReadOnlyList<BenchmarkCheck> Checks => _checks.Value;

    /// <summary>The catalogue from the runner's listing; a listing that fails leaves it empty and warns.</summary>
    public static BenchmarkCatalog From(EvalListing listing, ILogger log) => new(() =>
    {
        var result = listing.Read();
        if (result.Failure is { } reason)
        {
            log.Log(LogLevel.Warning, ApiLogMessage.BenchmarkListingUnavailable, new { Reason = reason });
            return [];
        }

        return Merge(result.Evals);
    });

    /// <summary>The listed evals as checks: the name is the id, what it proves is the title, and its manual is the path it listed.</summary>
    public static IReadOnlyList<BenchmarkCheck> Merge(IReadOnlyList<EvalLine> evals) =>
        [.. evals.Select(eval => new BenchmarkCheck(eval.Name, eval.Proves, eval.Manual))];
}
