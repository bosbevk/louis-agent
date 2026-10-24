using System.Text.Json;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// One scenario of a task: its input (what the user sends) and its success criteria (<see cref="Expected"/>, the values the
/// graders read, each key named by a grader). <see cref="Categories"/> slice the results (step 5c); <see cref="Split"/> comes
/// from the scenario's folder.
/// </summary>
public sealed record EvalScenario(string Id, EvalInput Input, JsonElement Expected, IReadOnlyList<string> Categories, DatasetSplit Split);
