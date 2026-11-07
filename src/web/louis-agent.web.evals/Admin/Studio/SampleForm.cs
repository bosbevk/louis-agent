using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Evals.Admin.Reviews;

namespace LouisAgent.Web.Evals.Admin.Studio;

/// <summary>
/// What the person types in the sample dialog: the new sample's id, its expected values as JSON, its categories as a
/// comma list and an ideal answer. It starts from the scenario the conversation ran and becomes the request the API
/// takes, or says why it can't.
/// </summary>
/// <remarks>
/// A class, not a record: the dialog's inputs bind to it and change it as the person types (ADR 0040, a thing that
/// changes over time). The expected values are checked to be a JSON object here, so a typo never leaves the page;
/// the API checks the keys.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class SampleForm
{
    /// <summary>How the expected values are shown: indented, so each key sits on its own line.</summary>
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The new sample's id, in kebab-case.</summary>
    public string Id { get; set; } = "";

    /// <summary>The expected values, as a JSON object.</summary>
    public string Expected { get; set; } = "";

    /// <summary>The categories, comma between them.</summary>
    public string Categories { get; set; } = "";

    /// <summary>An ideal answer, or nothing.</summary>
    public string Target { get; set; } = "";

    /// <summary>
    /// The form as it starts: a new id beside the scenario's, its expected values and its categories.
    /// </summary>
    public static SampleForm From(ScenarioRow scenario) => new()
    {
        Id = $"{scenario.Id}-2",
        Expected = JsonSerializer.Serialize(scenario.Expected, Indented),
        Categories = string.Join(", ", scenario.Categories)
    };

    /// <summary>The request for what was typed, or why it can't be sent.</summary>
    /// <remarks>Guard clause: expected values that aren't a JSON object are refused before anything is sent.</remarks>
    public (SampleDraftRequest? Request, string? Error) Request(ConversationRef conversation, string author)
    {
        if (ExpectedObject() is not { } expected) return (null, StudioText.ExpectedNotAnObject);

        var categories = Categories.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var key = new ConversationKeyRow(conversation.Mode, conversation.Repeat, conversation.Task);
        return (new SampleDraftRequest(key, Id.Trim(), JsonSerializer.SerializeToElement(expected), categories,
            string.IsNullOrWhiteSpace(Target) ? null : Target.Trim(), author), null);
    }

    /// <summary>The expected values, if they are a JSON object.</summary>
    /// <remarks>
    /// A <see cref="JsonObject"/>: what the person typed is a shape decided at run time (ADR 0041), not a reply of the
    /// API's to read by name.
    /// </remarks>
    private JsonObject? ExpectedObject()
    {
        try
        {
            return JsonNode.Parse(Expected) as JsonObject;
        }
        catch (JsonException)
        {
            // Not JSON at all: the same refusal as JSON that isn't an object.
            return null;
        }
    }
}
