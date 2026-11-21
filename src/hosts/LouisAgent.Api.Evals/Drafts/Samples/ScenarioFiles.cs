using System.Text.Json;
using LouisAgent.Api.Evals.List;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Api.Evals.Drafts.Samples;

/// <summary>
/// Reads the scenario a conversation ran from its eval's folder in the workspace, so a sample made from the run keeps
/// its place, time zone and expected keys. Dev comes first, then held-out; what can't be read is not there.
/// </summary>
/// <remarks>
/// The folder comes from the runner's listing (Louis's pick), never from a caller, and is read behind
/// <see cref="EvalPaths"/>, the guard the manuals use; the scenario's name must be a plain file name, so it can't climb
/// out of the scenarios.
/// </remarks>
public static class ScenarioFiles
{
    /// <summary>The splits a scenario file can sit in, in the order they're looked in.</summary>
    private static readonly RunSplit[] Splits = [RunSplit.Dev, RunSplit.HeldOut];

    /// <summary>The scenario, from the eval's folder, if it has one by that name.</summary>
    /// <remarks>Guard clauses: no folder, no name, a name with a path, a folder outside, then no such file.</remarks>
    public static SampleBody? Read(string workspaceRoot, string? folder, string scenario)
    {
        if (folder is null) return null;
        if (scenario.Length == 0) return null;
        if (Path.GetFileName(scenario) != scenario) return null;
        if (EvalPaths.Inside(workspaceRoot, folder) is not { } eval) return null;

        // The first split that has the file; a scenario sits in one of them.
        var file = Splits.Select(split => Path.Combine(eval, "Dataset", "scenarios", split.Wire(), scenario + ".json"))
            .FirstOrDefault(File.Exists);
        return file is null
            ? null
            : JsonSerializer.Deserialize<SampleBody>(File.ReadAllText(file), SampleBody.FileJson);
    }
}
