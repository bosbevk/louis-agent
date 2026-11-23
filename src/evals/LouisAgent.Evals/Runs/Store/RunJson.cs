using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Runs.Store;

/// <summary>
/// The one options object a run's file is written and read with: strict, as the eval file's are, with enums by
/// their wire names and the words of a transcript kept readable.
/// </summary>
/// <remarks>
/// A field it doesn't know, a missing one, or a null where none is allowed is refused, and nothing is lenient for an
/// older file (ADR 0049). Nulls are written, since a null cost means "nobody could price it", never "left out".
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public static class RunJson
{
    /// <summary>The run's file's options.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        IndentSize = 2
    };
}
