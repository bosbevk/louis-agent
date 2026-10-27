using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The checks the portal can run (F27, F18 step 4.3). The C# evals come from the runner's own listing (<see cref="EvalListing"/>), read on
/// first use. The Python checks are gone (F18 step 7.3: <see cref="Default"/> is empty); a check passed in as a script is the tests' stand-in until step 7.4 removes the script path.
/// A listed eval hides a script check with its id. A listing that can't be read leaves the script checks given, none by default, and a warning says why.
/// </summary>
public sealed class BenchmarkCatalog
{
    /// <summary>
    /// The script checks the portal offers: none (F18 step 7.3 deleted <c>benchmarks/</c>). Every check is an eval the runner lists: the stub (free, a canned run, no model) and Puls's
    /// <c>puls-cook</c> and <c>puls-routing</c>.
    /// </summary>
    public static readonly IReadOnlyList<BenchmarkCheck> Default = [];

    private readonly Lazy<IReadOnlyList<BenchmarkCheck>> _checks;

    /// <param name="checks">The checks; none by default (a test passes its own, with a stand-in script).</param>
    public BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
    {
        var list = checks ?? [];
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(() => list);
    }

    private BenchmarkCatalog(Func<IReadOnlyList<BenchmarkCheck>> checks) => _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(checks);

    /// <summary>Reads the listing now, so a warning lands in the start-up log rather than on the first request.</summary>
    public void Warm() => _ = _checks.Value;

    /// <summary>The checks the portal offers; the runner is listed the first time this is read.</summary>
    public IReadOnlyList<BenchmarkCheck> Checks => _checks.Value;

    /// <summary>The catalogue from the runner's listing and the script checks given (none by default); a listing that fails keeps those and warns.</summary>
    public static BenchmarkCatalog From(EvalListing listing, IReadOnlyList<BenchmarkCheck> python, ILogger log) => new(() =>
    {
        var result = listing.Read();
        if (result.Failure is { } reason)
        {
            log.Log(LogLevel.Warning, ApiLogMessage.BenchmarkListingUnavailable, new { Reason = reason });
            return python;
        }
        return Merge(result.Evals, python);
    });

    /// <summary>The listed evals, then the script checks whose id no listed eval has.</summary>
    public static IReadOnlyList<BenchmarkCheck> Merge(IReadOnlyList<EvalLine> evals, IReadOnlyList<BenchmarkCheck> python) =>
        evals.Select(eval => BenchmarkCheck.ForEval(eval.Name, eval.Proves, eval.Manual))
            .Concat(python.Where(check => evals.All(eval => eval.Name != check.Id)))
            .ToList();
}
