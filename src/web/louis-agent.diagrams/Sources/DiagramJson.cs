using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// How a diagram's file is written: names in camelCase, a kind, an icon or a link's way by its wire name, a field it
/// doesn't know refused rather than skipped, so a typo shows as an error, and a null where one isn't allowed refused.
/// A field left out is its default: a box's note and a card's code are optional, and the reader checks the title.
/// Written (CL60.2), a choice is its name in camel case, a field at its default is left out, and the file indented.
/// A sequence's step names its kind in its <c>step</c> field, read wherever a person put it among the fields (CL60.3).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records, one options object per wire</seealso>
internal static class DiagramJson
{
    /// <summary>The one options object for a diagram's file, read and written.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        AllowOutOfOrderMetadataProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        WriteIndented = true,
        IndentSize = 2
    };
}
