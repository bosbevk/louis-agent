namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>What a check runs (F18 step 4.3): a Python script (the old checks, until step 7 deletes them), or a C# eval the runner lists.</summary>
public enum BenchmarkCheckKind
{
    Script,
    Eval,
}
