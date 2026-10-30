using System.Text.Json;
using LouisAgent.Evals.Datasets.Scenarios;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// What the judge is told about the scenario: only the keys its eval file lists, one <c>key: value</c> line each,
/// so <c>serves: 4</c> reaches it and a budget it wasn't given can't sway it.
/// </summary>
/// <remarks>
/// The cook's judge taught this (F18 step 6a.4f): told the budget, a small judge judged the cost, which its prompt
/// forbids. A model can't misuse what it isn't told.
/// </remarks>
public sealed class JudgeAsk(string lines) : EvaluationContext("ask", lines)
{
    /// <summary>The lines, empty when the judge is told nothing from the scenario.</summary>
    public string Lines { get; } = lines;

    /// <summary>The listed keys' values in the scenario's <c>expected</c>, in the order listed.</summary>
    public static JudgeAsk Of(EvalScenario scenario, IReadOnlyList<string> keys) =>
        new(string.Join("\n", keys.Select(key => $"{key}: {Text(scenario.Expected.GetProperty(key))}")));

    /// <summary>A value as a person would write it: a string without its quotes, anything else as its JSON.</summary>
    private static string Text(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();
}
