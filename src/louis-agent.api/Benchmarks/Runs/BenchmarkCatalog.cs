using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The checks the portal can run (F27, F18 step 4.3). The C# evals come from the runner's own listing (<see cref="EvalListing"/>), read on
/// first use; the Python checks stay until step 7 deletes them (only the stub is left, F18 step 5.3). A listed eval hides the Python check with
/// its id. A listing that can't be read leaves the Python checks, and a warning says why.
/// </summary>
public sealed class BenchmarkCatalog
{
    /// <summary>The free stub check (F31): a canned run in the protocol, no session, no model, for the portal's hand checks.</summary>
    public static readonly BenchmarkCheck Stub = new("stub", "Stub check (free, no model)", "benchmarks/stub/run_check.py");

    /// <summary>
    /// The Python checks the portal offers until step 7: the stub alone. The cook and the routing are the runner's evals now (F18 step 5.3:
    /// <c>puls-cook</c> and <c>puls-routing</c>); their scripts stay on disk until step 7 deletes them.
    /// </summary>
    public static readonly IReadOnlyList<BenchmarkCheck> Default = [Stub];

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

    /// <summary>The catalogue from the runner's listing and the Python checks; a listing that fails keeps the Python checks and warns.</summary>
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

    /// <summary>The listed evals, then the Python checks whose id no listed eval has.</summary>
    public static IReadOnlyList<BenchmarkCheck> Merge(IReadOnlyList<EvalLine> evals, IReadOnlyList<BenchmarkCheck> python) =>
        evals.Select(eval => BenchmarkCheck.ForEval(eval.Name, eval.Proves, eval.Manual))
            .Concat(python.Where(check => evals.All(eval => eval.Name != check.Id)))
            .ToList();
}
