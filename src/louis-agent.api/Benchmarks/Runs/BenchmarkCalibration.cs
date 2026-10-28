namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// What a judge check looks at: the stored run whose people's grades are the right answers, and the judge (the model
/// grader) we hold up against them.
/// </summary>
public sealed record BenchmarkCalibration(string Of, string Grader);
