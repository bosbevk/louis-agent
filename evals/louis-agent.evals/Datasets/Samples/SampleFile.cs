using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Datasets.Samples;

/// <summary>
/// One scenario file in Inspect's <c>Sample</c> shape (F18 step 7d, Louis's pick G): its <c>id</c>, its
/// <c>input</c> (the user's first question as a chat message), an optional <c>target</c> (the ideal answer), and ours
/// under <c>metadata</c>. The split stays the folder the file is in.
/// </summary>
/// <remarks>
/// A Data Transfer Object for the file (ADR 0041), read strictly by <see cref="DatasetLoader"/> and turned into an
/// <see cref="Scenarios.EvalScenario"/>, so the rest of the harness never sees the file's shape. Following Inspect's
/// field names means its tools, and anyone who knows them, can read our datasets ("our datasets should also comply").
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record SampleFile
{
    /// <summary>The scenario's id, unique in its dataset.</summary>
    public required string Id { get; init; }

    /// <summary>The user's first question, as one chat message.</summary>
    public required IReadOnlyList<SampleMessage> Input { get; init; }

    /// <summary>The ideal answer, when the dataset gives one; left out of the file otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Target { get; init; }

    /// <summary>What our harness needs beyond Inspect's fields.</summary>
    public required SampleMetadata Metadata { get; init; }
}

/// <summary>One chat message of a sample's input: who says it, and what.</summary>
public sealed record SampleMessage
{
    /// <summary>Who says it, by its wire name; the loader reads it as a <see cref="MessageRole"/>.</summary>
    public required string Role { get; init; }

    /// <summary>What is said.</summary>
    public required string Content { get; init; }
}

/// <summary>
/// The scenario's own fields: where the user is and their time zone, the follow-ups asked after the first answer, the
/// success criteria the graders read, and the categories that slice the results.
/// </summary>
public sealed record SampleMetadata
{
    /// <summary>Where the user says they are; null when the scenario gives no place.</summary>
    public required string? Place { get; init; }

    /// <summary>The user's time zone.</summary>
    public required string TimeZone { get; init; }

    /// <summary>The questions asked after the first, in order.</summary>
    public IReadOnlyList<string> FollowUps { get; init; } = [];

    /// <summary>The success criteria, each key read by a grader.</summary>
    public required JsonElement Expected { get; init; }

    /// <summary>The categories the results are sliced by.</summary>
    public IReadOnlyList<string> Categories { get; init; } = [];
}
