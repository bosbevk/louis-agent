using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The evals the portal can run, as the runner lists them, read the first time someone asks. If the listing can't be
/// read there are none, and a warning says why.
/// </summary>
/// <remarks>
/// Lazy Load (Fowler, PoEAA): listing the runner starts a process, so it waits until the checks are first wanted (or
/// <see cref="Warm"/> asks at start-up), and a test can hand in its own checks with no runner at all.
/// </remarks>
public sealed class BenchmarkCatalog
{
    private readonly Lazy<IReadOnlyList<BenchmarkCheck>> _checks;

    /// <param name="checks">The checks; none by default (a test passes its own).</param>
    public BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
    {
        var list = checks ?? [];
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(() => list);
    }

    /// <summary>A catalogue whose checks come from a read that waits until they're first wanted.</summary>
    private BenchmarkCatalog(Func<IReadOnlyList<BenchmarkCheck>> checks) =>
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(checks);

    /// <summary>Reads the listing now, so a warning lands in the start-up log, not on the first request.</summary>
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

    /// <summary>
    /// Each listed eval as a check: its name is the id, what it proves the title, and it keeps its manual's path and
    /// its whole line.
    /// </summary>
    public static IReadOnlyList<BenchmarkCheck> Merge(IReadOnlyList<EvalLine> evals) =>
        [.. evals.Select(eval => new BenchmarkCheck(eval.Name, eval.Proves, eval.Manual, eval))];
}
