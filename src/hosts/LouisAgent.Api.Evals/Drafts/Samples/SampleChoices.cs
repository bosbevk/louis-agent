using System.Text.Json;

namespace LouisAgent.Api.Evals.Drafts.Samples;

/// <summary>
/// What the person decides when they make a sample from a conversation: its id, the expected values that change, the
/// categories when they pick their own, an ideal answer if they typed one, and their name.
/// </summary>
/// <remarks>
/// The expected values are only the keys that change, as a JSON object; the scenario's other values stay. Null for
/// the categories keeps the scenario's.
/// </remarks>
public sealed record SampleChoices(
    string SampleId,
    JsonElement? Expected,
    IReadOnlyList<string>? Categories,
    string? Target,
    string Author);
