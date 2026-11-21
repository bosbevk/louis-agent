using System.Text.Encodings.Web;
using System.Text.Json;

namespace LouisAgent.Web.Components.Site.Meta.Schema;

/// <summary>
/// Writes a schema.org record as the JSON-LD a page carries in its head (F123): schema.org's names in camel case, and
/// the angle brackets escaped, so words in a value can never close the script they sit in.
/// </summary>
/// <remarks>
/// One options object for this wire (ADR 0041): the records say what they are, and this says how they are written.
/// The default encoder already escapes &lt;, &gt; and &amp;, which is what keeps the JSON safe inside a script tag.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class SchemaJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default,
    };

    /// <summary>The record as JSON-LD.</summary>
    public static string Write(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);
}
