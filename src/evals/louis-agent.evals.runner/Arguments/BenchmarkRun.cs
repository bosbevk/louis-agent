using System.Globalization;

namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>
/// The run's name (F18 step 4.1 review, and 4.2 review): BENCHMARK_RUN when it is set, trimmed; otherwise <c>local-{yyyyMMdd-HHmmss}</c>
/// from the clock's local time, as the Python checks name a run (benchmarks/*/run_check.py). Every session's tag is
/// <c>{run}:{mode}:{repeat}</c>, so the ledger's rows find their run.
/// </summary>
public static class BenchmarkRun
{
    public static string Of(string? value, TimeProvider clock) =>
        string.IsNullOrWhiteSpace(value)
            ? clock.GetLocalNow().ToString("'local-'yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
            : value.Trim();
}
