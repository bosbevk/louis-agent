using LouisAgent.Core.Messages;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Studio;

/// <summary>
/// The eval a run runs: the catalogue's by its name, or the eval in the folder <c>--folder</c> names (a studio try,
/// F49.5), which must be the eval the run names; with <c>--samples</c>, only those of its scenarios.
/// </summary>
/// <remarks>
/// Guard clauses (ADR 0051): no folder is the catalogue's eval; a folder is sealed and loaded as <c>--check</c> loads
/// it, then its name is checked; samples narrow it through <see cref="SampleSubset"/>, and one the eval doesn't have is
/// refused, so a typo never runs an empty eval.
/// </remarks>
public static class RunTarget
{
    /// <summary>The eval the request runs.</summary>
    public static IEval Of(RunRequest run, EvalCatalog catalog)
    {
        var eval = run.Folder is null ? catalog.Get(run.Name) : FromFolder(run.Name, run.Folder);
        if (run.Samples is null) return eval;

        var samples = run.Samples.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        if (samples.FirstOrDefault(s => eval.Scenarios.All(scenario => scenario.Id != s)) is { } unknown)
            throw new RunnerException(MessageText.Format(RunnerMessage.UnknownSample,
                new
                {
                    Sample = unknown,
                    Eval = eval.Name,
                    Supported = string.Join(", ", eval.Scenarios.Select(s => s.Id))
                }));
        return new SampleSubset(eval, samples);
    }

    /// <summary>
    /// The eval in the folder, sealed and loaded as a check loads it; it must be the one the run names.
    /// </summary>
    private static IEval FromFolder(string name, string folder)
    {
        ManifestSeal.Seal(folder);
        var eval = FileEval.Load(folder, RunnerChecks.Catalog, RunnerReaders.Catalog, "studio");
        if (eval.Name != name)
            throw new RunnerException(MessageText.Format(RunnerMessage.FolderIsAnotherEval,
                new
                {
                    Folder = folder,
                    Found = eval.Name,
                    Named = name
                }));
        return eval;
    }
}
