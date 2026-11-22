using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// The one options object for an eval file, read and written: names in camelCase, a field it doesn't know refused,
/// a field a record can't do without required, a null where none is allowed refused, and a fact and an agent read
/// straight into their types.
/// </summary>
/// <remarks>
/// One options object per wire (ADR 0041), strict as the diagrams' and the CV's readers are (ADR 0049: no lenient
/// reader), so a typo in a file is an error and never a field that quietly vanishes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
internal static class EvalJson
{
    /// <summary>The options an eval file is read and written with.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new FactConverter(), new AgentAddressConverter() },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        IndentSize = 2
    };
}
