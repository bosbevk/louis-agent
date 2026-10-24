using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The checks the portal can run (F27, F18 step 4.3). The C# evals come from the runner's own listing (<see cref="EvalListing"/>), read on
/// first use; the Python checks stay until step 5 lists cook and routing, and step 7 deletes them. A listed eval hides the Python check with
/// its id. A listing that can't be read leaves the Python checks, and a warning says why.
/// </summary>
public sealed class BenchmarkCatalog
{
    /// <summary>The free stub check (F31): a canned run in the protocol, no session, no model, for the portal's hand checks.</summary>
    public static readonly BenchmarkCheck Stub = new("stub", "Stub check (free, no model)", "benchmarks/stub/run_check.py");

    /// <summary>The cook check (F25; F31; F34): Puls with the cook's tools against Puls with its cook, priced from the ledger. Spends money.</summary>
    public static readonly BenchmarkCheck Cook = new("cook", "Cook check: Puls with the cook's tools against Puls with the cook", "benchmarks/cook/run_check.py");

    /// <summary>The routing check (F5, F29; F31): one agent with every toolset against Puls as built, twelve questions. Spends money.</summary>
    public static readonly BenchmarkCheck Routing = new("routing", "Routing check: one agent against Puls as built", "benchmarks/routing/run_check.py");

    /// <summary>The Python checks the portal offers until step 5 and step 7.</summary>
    public static readonly IReadOnlyList<BenchmarkCheck> Default = [Stub, Cook, Routing];

    private readonly Lazy<IReadOnlyList<BenchmarkCheck>> _checks;

    /// <param name="checks">The checks; none by default (a test passes its own, with a stand-in script).</param>
    public BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
    {
        var list = checks ?? [];
        _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(() => list);
    }

    private BenchmarkCatalog(Func<IReadOnlyList<BenchmarkCheck>> checks) => _checks = new Lazy<IReadOnlyList<BenchmarkCheck>>(checks);

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
        evals.Select(eval => BenchmarkCheck.ForEval(eval.Name, eval.Proves))
            .Concat(python.Where(check => evals.All(eval => eval.Name != check.Id)))
            .ToList();
}
