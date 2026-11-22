using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using LouisAgent.Choices;
using LouisAgent.Composition.Agents;

namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// The schema an editor checks an eval file against, made from the records the reader fills, with the reader's own
/// options, so the two can't say different things: a field's description is its hover text, a field the records don't
/// know is underlined, and a fact is a string that starts with one of the seven words.
/// </summary>
/// <remarks>
/// Generated from the records (Louis's pick, 2026-11-22), chosen over a schema written by hand and over none: .NET's
/// <see cref="JsonSchemaExporter"/> follows the reader's options, so a strict reader gives
/// <c>"additionalProperties": false</c>. The file in the repository is a golden file a test holds to this, so a change
/// to a record shows in review as a change to the schema. The fact's pattern is JSON Schema's own; no regular
/// expression runs in our code (ADR 0057).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class EvalSchema
{
    /// <summary>The JSON Schema draft the file follows.</summary>
    private const string Draft = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>The schema's title, which the editor shows.</summary>
    private const string Title = "An eval file: the agent it proves, and the conversations a person has with it";

    /// <summary>
    /// How the schema's text is written: indented by two, as the eval files are, and an apostrophe as itself.
    /// </summary>
    private static readonly JsonSerializerOptions Written = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>The schema's text, ending with a new line.</summary>
    public static string Write()
    {
        var reader = new JsonSerializerOptions(EvalJson.Options)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        var exporter = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = Transform
        };

        var schema = reader.GetJsonSchemaAsNode(typeof(EvalFile), exporter);
        return schema.ToJsonString(Written) + "\n";
    }

    /// <summary>
    /// One node of the schema, as the editor needs it: a fact, a list of facts and an agent as the strings the file
    /// writes, each field with its description, and the root naming its draft and its title.
    /// </summary>
    /// <remarks>
    /// A fact has a converter of its own, so the exporter knows nothing of its shape and leaves the list's items out;
    /// the list is given them here. A switch on the type, read like a table (ADR 0051).
    /// </remarks>
    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node)
    {
        var type = context.TypeInfo.Type;
        JsonNode shaped = type switch
        {
            _ when type == typeof(Fact) => FactNode(),
            _ when type == typeof(IReadOnlyList<Fact>) => FactsNode(),
            _ when type == typeof(AgentAddress) => AddressNode(),
            _ => node
        };

        string? description = DescriptionOf(context.PropertyInfo?.AttributeProvider);
        if (description is not null && shaped is JsonObject described) described["description"] = description;

        return context.Path.IsEmpty ? Root(shaped.AsObject()) : shaped;
    }

    /// <summary>A fact: a string that starts with one of the seven words, read from the enum.</summary>
    private static JsonObject FactNode()
    {
        string words = string.Join("|", SettingChoice.Names<FactKind>());
        return new JsonObject { ["type"] = "string", ["pattern"] = $"^({words})( .+)?$" };
    }

    /// <summary>A turn's facts: a list of them.</summary>
    private static JsonObject FactsNode() => new() { ["type"] = "array", ["items"] = FactNode() };

    /// <summary>An agent: its address as a string, an identity or one of its roles.</summary>
    private static JsonObject AddressNode() => new() { ["type"] = "string" };

    /// <summary>The root, with its draft and its title first.</summary>
    private static JsonObject Root(JsonObject root)
    {
        var named = new JsonObject { ["$schema"] = Draft, ["title"] = Title };
        foreach (var (name, value) in root.ToList())
        {
            root.Remove(name);
            named[name] = value;
        }

        return named;
    }

    /// <summary>A field's description, from its <see cref="DescriptionAttribute"/>, or none.</summary>
    private static string? DescriptionOf(ICustomAttributeProvider? field) =>
        field?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true).OfType<DescriptionAttribute>()
            .FirstOrDefault()?.Description;
}
