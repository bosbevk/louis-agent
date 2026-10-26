namespace LouisAgent.Evals.Datasets;

/// <summary>A loaded dataset: its manifest, every scenario (dev first, then held-out) and a person's labels of stored answers (F18 step 6.3), none when it has no <c>labels/</c> folder.</summary>
public sealed record Dataset(DatasetManifest Manifest, IReadOnlyList<EvalScenario> Scenarios, IReadOnlyList<LouisAgent.Evals.Labels.LabeledAnswer>? Labels = null)
{
    public IReadOnlyList<LouisAgent.Evals.Labels.LabeledAnswer> Labels { get; init; } = Labels ?? [];
}
