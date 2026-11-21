using System.Text.Json;

namespace LouisAgent.Evals.Datasets.Scenarios;

/// <summary>
/// One scenario of a task: its input (what the user sends) and its success criteria (<see cref="Expected"/>, the
/// values the checks read, each key named by a check). <see cref="Categories"/> slice the results (step 5c);
/// <see cref="Split"/> comes from the scenario's folder; <see cref="Target"/> is the sample's ideal answer, when its
/// file gives one (Inspect's <c>target</c>, step 7d).
/// </summary>
public sealed record EvalScenario(
    string Id,
    EvalInput Input,
    JsonElement Expected,
    IReadOnlyList<string> Categories,
    DatasetSplit Split,
    string? Target = null);
