using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Site.Resumes;

/// <summary>
/// Reads Louis's CV from its JSON Resume file (F142), once, into the <see cref="Resume"/> the page draws and the guide
/// reads. A field the records don't know is refused, so a misspelt one fails the read instead of vanishing.
/// </summary>
/// <remarks>
/// One options object for this file and nothing else (ADR 0041): JSON Resume writes its names in camelCase
/// (<c>startDate</c>), and a strict reader keeps the file and the records in step (ADR 0049: no lenient reader).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public static class ResumeReader
{
    /// <summary>The CV file's names, camelCase as JSON Resume writes them; a field we don't know is an error.</summary>
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    /// <summary>The CV in this stream; a file that is only <c>null</c> is refused as well.</summary>
    public static Resume Read(Stream json) =>
        JsonSerializer.Deserialize<Resume>(json, Json) ?? throw new JsonException();
}
