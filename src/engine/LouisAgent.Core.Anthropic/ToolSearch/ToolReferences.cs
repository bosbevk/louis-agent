using System.Text.Json;
using Anthropic.Models.Messages;
using LouisAgent.Core.Agent.EngineTools.ToolSearch;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.ToolSearch;

/// <summary>
/// FindTool's answer as Anthropic's custom tool search wants it (F97.3): a tool reference block per tool found, carried
/// as the raw form the SDK's adapter passes into a tool result, so the API loads each tool's definition.
/// </summary>
internal static class ToolReferences
{
    /// <summary>
    /// The references for the names FindTool found, or none when its answer names no tool; the answer is the record on
    /// a live run and its JSON once kept in the history or played back.
    /// </summary>
    public static IReadOnlyList<AIContent> Of(object? answer) =>
        [.. NamesOf(answer).Select(Reference)];

    /// <summary>One tool reference block, in the form the adapter sends as it is.</summary>
    private static AIContent Reference(string tool) =>
        new() { RawRepresentation = new Block(new ToolReferenceBlockParam { ToolName = tool }) };

    /// <summary>The names in FindTool's answer, as the record or as its JSON.</summary>
    private static IEnumerable<string> NamesOf(object? answer) => answer switch
    {
        FoundTools found => found.Names,
        JsonElement { ValueKind: JsonValueKind.Object } json => NamesOf(json),
        _ => []
    };

    /// <summary>The names in the record's JSON, by its one property, whatever the case it was written in.</summary>
    private static IEnumerable<string> NamesOf(JsonElement json)
    {
        foreach (var property in json.EnumerateObject())
        {
            if (!property.NameEquals(nameof(FoundTools.Names)) &&
                !string.Equals(property.Name, nameof(FoundTools.Names), StringComparison.OrdinalIgnoreCase)) continue;
            if (property.Value.ValueKind != JsonValueKind.Array) return [];
            return property.Value.EnumerateArray()
                .Where(name => name.ValueKind == JsonValueKind.String)
                .Select(name => name.GetString()!)
                .ToList();
        }

        return [];
    }
}
