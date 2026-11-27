using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Tools;

/// <summary>
/// How a tool method is described to the model: its name, its <c>[Description]</c> and every parameter's, each with
/// LF line endings whatever the checkout, so the model is sent the same bytes on every machine.
/// </summary>
/// <remarks>
/// One factory that both the catalogue's tools and the engine's own go through, so neither can leave the parameters
/// out, as both did until FX254. The parameters are rewritten with Microsoft.Extensions.AI's own hook,
/// <see cref="AIJsonSchemaCreateOptions.TransformSchemaNode"/>, which runs on each schema as it is built, rather than
/// with a wrapper around the finished function.
/// </remarks>
internal static class ToolFunctionOptions
{
    /// <summary>The key a schema holds its description under.</summary>
    private const string DescriptionKey = "description";

    /// <summary>The options a tool method becomes a function with; its own name when none is given.</summary>
    public static AIFunctionFactoryOptions For(MethodInfo method, string? name = null) => new()
    {
        Name = name,
        Description = method.GetCustomAttribute<DescriptionAttribute>() is { } description
            ? AgentEngine.Normalise(description.Description)
            : null,
        JsonSchemaCreateOptions = new AIJsonSchemaCreateOptions
        {
            TransformSchemaNode = (_, node) => WithLfDescription(node)
        }
    };

    /// <summary>A schema with its description, if it has one, in LF line endings.</summary>
    private static JsonNode WithLfDescription(JsonNode node)
    {
        if (node is JsonObject schema && schema[DescriptionKey] is JsonValue value &&
            value.TryGetValue(out string? text))
            schema[DescriptionKey] = AgentEngine.Normalise(text);
        return node;
    }
}
