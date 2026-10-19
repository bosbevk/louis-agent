namespace LouisAgent.Api.Benchmarks.Runs;

public enum BenchmarkRunStatus
{
    Running,

    /// <summary>The script exited 0; its folder holds the results.</summary>
    Done,

    /// <summary>The script exited with an error, or couldn't start; its lines say why.</summary>
    Failed,
}
