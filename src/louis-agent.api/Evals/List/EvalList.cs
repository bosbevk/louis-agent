using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;

namespace LouisAgent.Api.Evals.List;

/// <summary>
/// The evals the portal can run, as the runner lists them, read the first time someone asks. If the listing can't be
/// read there are none, and a warning says why.
/// </summary>
/// <remarks>
/// Lazy Load (Fowler, PoEAA): listing the runner starts a process, so it waits until the checks are first wanted (or
/// <see cref="Warm"/> asks at start-up), and a test can hand in its own checks with no runner at all.
/// </remarks>
public sealed class EvalList
{
    private readonly Lazy<IReadOnlyList<EvalEntry>> _checks;

    /// <summary>A catalogue that holds the checks you hand it, or none. Tests use this one.</summary>
    /// <param name="checks">The checks; none by default (a test passes its own).</param>
    public EvalList(IReadOnlyList<EvalEntry>? checks = null)
    {
        var list = checks ?? [];
        _checks = new Lazy<IReadOnlyList<EvalEntry>>(() => list);
    }

    /// <summary>A catalogue whose checks come from a read that waits until they're first wanted.</summary>
    private EvalList(Func<IReadOnlyList<EvalEntry>> checks) =>
        _checks = new Lazy<IReadOnlyList<EvalEntry>>(checks);

    /// <summary>Reads the listing now, so a warning lands in the start-up log, not on the first request.</summary>
    public void Warm() => _ = _checks.Value;

    /// <summary>The evals the portal offers. The first time you ask, it asks the runner for its list.</summary>
    public IReadOnlyList<EvalEntry> Checks => _checks.Value;

    /// <summary>
    /// Builds the catalogue from the runner's list. If the runner can't list them, there are none and the log says
    /// why.
    /// </summary>
    public static EvalList From(EvalListing listing, ILogger log) => new(() =>
    {
        var result = listing.Read();
        // No list: warn once and offer nothing, rather than failing every page that asks.
        if (result.Failure is { } reason)
        {
            log.Log(LogLevel.Warning, ApiLogMessage.BenchmarkListingUnavailable, new { Reason = reason });
            return [];
        }

        return Merge(result.Evals);
    });

    /// <summary>
    /// Turns each listed eval into something the portal can run: its name goes in the URL, what it proves is its
    /// title, and it keeps its manual's path and the runner's whole line.
    /// </summary>
    public static IReadOnlyList<EvalEntry> Merge(IReadOnlyList<EvalLine> evals) =>
        [.. evals.Select(eval => new EvalEntry(eval.Name, eval.Proves, eval.Manual, eval))];
}
