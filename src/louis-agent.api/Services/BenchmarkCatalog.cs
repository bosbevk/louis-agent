namespace LouisAgent.Api.Services;

/// <summary>
/// The checks the portal can run (F27). Empty since CL10: the old checks went with <c>samples/</c>, and the rewritten
/// ones (scripts in <c>benchmarks/</c>, results in SQL Server's <c>puls_benchmarks</c>) come with the Benchmarks feature.
/// The portal reads no run folders any more.
/// </summary>
/// <param name="checks">The checks; none by default (a test passes its own, with a stand-in script).</param>
public sealed class BenchmarkCatalog(IReadOnlyList<BenchmarkCheck>? checks = null)
{
    /// <summary>The free stub check (F31): a canned run in the protocol, no session, no model, for the portal's hand checks.</summary>
    public static readonly BenchmarkCheck Stub = new("stub", "Proefkontrole (gratis, geen model)", "benchmarks/stub/run_check.py");

    /// <summary>The cook check (F25; F31): Puls alone against Puls with its cook, priced from the ledger. Spends money.</summary>
    public static readonly BenchmarkCheck Cook = new("cook", "Kokkontrole: Puls alleen teen Puls met die kok", "benchmarks/cook/run_check.py");

    /// <summary>The checks the portal offers.</summary>
    public static readonly IReadOnlyList<BenchmarkCheck> Default = [Stub, Cook];

    public IReadOnlyList<BenchmarkCheck> Checks { get; } = checks ?? [];
}
