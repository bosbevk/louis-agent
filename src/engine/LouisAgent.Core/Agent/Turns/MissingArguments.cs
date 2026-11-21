using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Turns;

/// <summary>
/// Says what a tool call left out, in the tool's own words (FX108): every argument the tool needs that the call didn't
/// send, each with its description, so the model learns what to send and its choices. Without it the model heard only
/// "the arguments dictionary is missing a value for the required parameter", and could call a tool three times without
/// the one argument it needed.
/// </summary>
/// <remarks>
/// Read from the tool's own schema, which the model was given: its <c>required</c> names and each property's
/// <c>description</c>. So every pack's tools get it, with nothing written per tool, and the words the model reads are
/// the ones the tool's author wrote for it (ADR 0080: the tool says it better than a prompt rule would).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static class MissingArguments
{
    /// <summary>What the call left out, as the model reads it; null when it sent everything the tool needs.</summary>
    public static string? Message(AIFunction function, AIFunctionArguments arguments)
    {
        var schema = function.JsonSchema;
        if (!schema.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.Array)
            return null;

        // Each needed name the call didn't send, or sent empty, in the order the tool lists them.
        var missing = required.EnumerateArray().Select(name => name.GetString() ?? "")
            .Where(name => name.Length > 0 && !(arguments.TryGetValue(name, out var value) && value is not null))
            .ToList();
        if (missing.Count == 0) return null;

        var lines = missing.Select(name => MessageText.Format(AgentToolMessage.ArgumentMissing,
            new { Name = name, Meaning = MeaningOf(schema, name) }));
        return MessageText.Format(AgentToolMessage.ArgumentsMissing,
            new { Tool = function.Name, Missing = string.Join("\n", lines) });
    }

    /// <summary>A parameter's description from the schema, or its type when the tool gave none.</summary>
    /// <remarks>Guard clauses, one case each (ADR 0051).</remarks>
    private static string MeaningOf(JsonElement schema, string name)
    {
        if (!schema.TryGetProperty("properties", out var properties)) return "";
        if (!properties.TryGetProperty(name, out var property)) return "";
        if (property.TryGetProperty("description", out var description)) return description.GetString() ?? "";
        return property.TryGetProperty("type", out var type) ? type.ToString() : "";
    }
}
