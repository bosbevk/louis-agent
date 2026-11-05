using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Studio;

/// <summary>
/// An eval run on a few of its scenarios only, the ones a studio try names (F49.5): everything else is the eval's own.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four) over <see cref="IEval"/>: the runner, the graders and the protocol see an eval like any
/// other, so a try is graded and recorded the way a run is, with no flag through the harness. Every member is passed
/// on, the interface's defaults too, so the eval behind it decides them.
/// </remarks>
public sealed class SampleSubset(IEval eval, IReadOnlySet<string> samples) : IEval
{
    /// <inheritdoc />
    public string Name => eval.Name;

    /// <inheritdoc />
    public string? Identity => eval.Identity;

    /// <inheritdoc />
    public int Version => eval.Version;

    /// <inheritdoc />
    public string Proves => eval.Proves;

    /// <inheritdoc />
    public string? Manual => eval.Manual;

    /// <inheritdoc />
    public string? Folder => eval.Folder;

    /// <summary>Only the scenarios the try named, in the eval's order.</summary>
    public IReadOnlyList<EvalScenario> Scenarios { get; } = [.. eval.Scenarios.Where(s => samples.Contains(s.Id))];

    /// <inheritdoc />
    public DatasetManifest? Dataset => eval.Dataset;

    /// <inheritdoc />
    public IReadOnlyList<ICheck> Checks => eval.Checks;

    /// <inheritdoc />
    public IReadOnlyList<LabeledAnswer> Labels => eval.Labels;

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
        CancellationToken cancellationToken) => eval.ModesAsync(sessions, cancellationToken);

    /// <inheritdoc />
    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => eval.TurnFactsOf(turn, mode);
}
