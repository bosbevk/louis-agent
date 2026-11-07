using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Contracts.Admin.Studio;

namespace LouisAgent.Web.Evals.Admin.Studio.Schema;

/// <summary>
/// Draws a check's form fields from the JSON schema of its settings, as the runner's catalogue gives it (F49.5): each
/// property is a field of the kind its type says, required where the schema's <c>required</c> lists it; a property
/// named <c>reader</c> is a pick from the catalogue's readers.
/// </summary>
/// <remarks>
/// The schema is read as a <see cref="JsonNode"/>: its shape is each check's settings record, decided in the runner
/// (ADR 0041's one case for a node). A switch on the schema's type, one arm per kind (ADR 0051); a type given as
/// <c>["number","null"]</c> is its first non-null type.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class SchemaFields
{
    /// <summary>The fields of one check type, in its schema's order.</summary>
    public static IReadOnlyList<SchemaField> Of(CheckTypeRow check)
    {
        var schema = JsonSerializer.SerializeToNode(check.Parameters) as JsonObject;
        if (schema?["properties"] is not JsonObject properties) return [];

        var required = (schema["required"] as JsonArray ?? []).Select(n => n?.GetValue<string>()).ToHashSet();
        return
        [
            .. properties.Select(p => new SchemaField(p.Key, KindOf(p.Key, p.Value), required.Contains(p.Key),
                p.Value?["default"]?.ToJsonString().Trim('"')))
        ];
    }

    /// <summary>Every check type's fields, by its type, for writing a file from the form.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<SchemaField>> ByType(IEnumerable<CheckTypeRow> checks) =>
        checks.ToDictionary(c => c.Type, Of, StringComparer.Ordinal);

    /// <summary>What a property holds, from its name and its schema's type.</summary>
    private static FieldKind KindOf(string name, JsonNode? property)
    {
        if (name == "reader") return FieldKind.Reader;
        return TypeOf(property) switch
        {
            "integer" => FieldKind.Whole,
            "number" => FieldKind.Number,
            "boolean" => FieldKind.YesNo,
            "array" => FieldKind.List,
            _ => FieldKind.Text,
        };
    }

    /// <summary>The property's type: the text, or the first of a list that isn't <c>null</c>.</summary>
    private static string? TypeOf(JsonNode? property) => property?["type"] switch
    {
        JsonValue value => value.GetValue<string>(),
        JsonArray types => types.Select(t => t?.GetValue<string>()).FirstOrDefault(t => t != "null"),
        _ => null,
    };
}
