namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>What a judge check is of (F18 step 6a.5b): the stored run whose people's grades are the labels, and the judge (the model grader) checked against them.</summary>
public sealed record BenchmarkCalibration(string Of, string Grader);
