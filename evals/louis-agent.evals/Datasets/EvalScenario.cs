using System.Text.Json;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// One scenario of a task: its input (what the user sends) and its success criteria (<see cref="Expected"/>, the values the
/// graders read, each key named by a grader).
/// </summary>
public sealed record EvalScenario(string Id, EvalInput Input, JsonElement Expected);
