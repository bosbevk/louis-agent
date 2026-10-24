namespace LouisAgent.Evals.Datasets;

/// <summary>A loaded dataset: its manifest and every scenario, dev first, then held-out.</summary>
public sealed record Dataset(DatasetManifest Manifest, IReadOnlyList<EvalScenario> Scenarios);
