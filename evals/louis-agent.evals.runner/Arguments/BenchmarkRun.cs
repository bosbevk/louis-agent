namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>
/// The run's name, from BENCHMARK_RUN, as the Python checks read it (benchmarks/*/run_check.py): every session's tag is
/// <c>{run}:{mode}:{repeat}</c>, so the ledger's rows find their run. Unset is <see cref="Default"/>.
/// </summary>
public static class BenchmarkRun
{
    public const string Default = "local";

    public static string Of(string? value) => string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
}
