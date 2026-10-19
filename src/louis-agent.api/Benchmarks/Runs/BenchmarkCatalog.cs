namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The checks the portal can run (F27). Empty since CL10: the old checks went with <c>samples/</c>, and the rewritten
/// ones (scripts in <c>benchmarks/</c>, results in SQL Server's <c>puls_benchmarks</c>) come with the Benchmarks feature.
/// The portal reads no run folders any more.
/// </summary>
/// <param name="checks">The checks; none by default (a test passes its own, with a stand-in script).</param>
public sealed class BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
{
    /// <summary>The free stub check (F31): a canned run in the protocol, no session, no model, for the portal's hand checks.</summary>
    public static readonly BenchmarkCheck Stub = new("stub", "Stub check (free, no model)", "benchmarks/stub/run_check.py");

    /// <summary>The cook check (F25; F31; F34): Puls with the cook's tools against Puls with its cook, priced from the ledger. Spends money.</summary>
    public static readonly BenchmarkCheck Cook = new("cook", "Cook check: Puls with the cook's tools against Puls with the cook", "benchmarks/cook/run_check.py");

    /// <summary>The routing check (F5, F29; F31): one agent with every toolset against Puls as built, twelve questions. Spends money.</summary>
    public static readonly BenchmarkCheck Routing = new("routing", "Routing check: one agent against Puls as built", "benchmarks/routing/run_check.py");

    /// <summary>The checks the portal offers.</summary>
    public static readonly IReadOnlyList<BenchmarkCheck> Default = [Stub, Cook, Routing];

    public IReadOnlyList<BenchmarkCheck> Checks { get; } = checks ?? [];
}
