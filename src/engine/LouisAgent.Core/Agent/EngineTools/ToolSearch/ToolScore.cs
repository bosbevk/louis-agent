using System.Text.Json;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.ToolSearch;

/// <summary>
/// How well one tool fits a search (F97.3): how many of the query's words it has anywhere, then how much they weigh,
/// a word in its name more than one in its description or its parameters, the fields Anthropic's own search reads.
/// </summary>
/// <param name="Name">The tool's name.</param>
/// <param name="Covered">How many of the query's words the tool has, the first thing a search sorts by.</param>
/// <param name="Weight">Where they were found: a word in the name counts more.</param>
internal sealed record ToolScore(string Name, int Covered, int Weight)
{
    /// <summary>What a query word in a tool's name weighs, against one in its description or parameters.</summary>
    private const int InName = 3, InText = 1;

    /// <summary>Whether the tool has any of the query's words at all.</summary>
    public bool Fits => Covered > 0;

    /// <summary>The tool scored against the query's words.</summary>
    public static ToolScore Of(AITool tool, IReadOnlyList<string> query)
    {
        var name = ToolWords.OfName(tool.Name);
        var text = ToolWords.Of(TextOf(tool));
        int covered = 0, weight = 0;
        foreach (string word in query.Distinct())
        {
            int found = WeightOf(word, name, text);
            if (found == 0) continue;
            covered++;
            weight += found;
        }

        return new ToolScore(tool.Name, covered, weight);
    }

    /// <summary>
    /// What one query word weighs for a tool: counted once, where it weighs most; nothing when it isn't there.
    /// </summary>
    private static int WeightOf(string word, IReadOnlyList<string> name, IReadOnlyList<string> text)
    {
        if (name.Any(w => ToolWords.Match(w, word))) return InName;
        if (text.Any(w => ToolWords.Match(w, word))) return InText;
        return 0;
    }

    /// <summary>A tool's description and its parameters' names and descriptions, as one text.</summary>
    private static string TextOf(AITool tool) =>
        tool is AIFunctionDeclaration function
            ? $"{tool.Description} {ParametersOf(function.JsonSchema)}"
            : tool.Description;

    /// <summary>A schema's parameters, each by its name and its description.</summary>
    private static string ParametersOf(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object ||
            !schema.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object) return "";

        var parts = new List<string>();
        foreach (var parameter in properties.EnumerateObject())
        {
            parts.Add(string.Join(' ', ToolWords.OfName(parameter.Name)));
            if (parameter.Value.TryGetProperty("description", out var description) &&
                description.ValueKind == JsonValueKind.String) parts.Add(description.GetString()!);
        }

        return string.Join(' ', parts);
    }
}
