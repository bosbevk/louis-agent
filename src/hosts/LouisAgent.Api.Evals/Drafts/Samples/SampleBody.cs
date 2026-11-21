using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Api.Evals.Drafts.Samples;

/// <summary>
/// A sample as an eval's scenario file holds it (Inspect's <c>Sample</c>): its id, the user's first question as a
/// chat message, the ideal answer when there is one, and ours under <c>metadata</c>.
/// </summary>
/// <remarks>
/// The API's own copy of the harness's <c>SampleFile</c>, since the API never carries the harness (it runs the runner
/// as a process); <c>SampleBodyShapeTests</c>, where both are referenced, holds the two to one shape. It is written
/// and read with <see cref="FileJson"/>, the scenario files' wire. The ideal answer is optional, as in the files, so
/// it comes last here, and the metadata is written after it, in Inspect's order.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record SampleBody(
    string Id,
    IReadOnlyList<SampleBodyMessage> Input,
    [property: JsonPropertyOrder(1)] SampleBodyMetadata Metadata,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Target = null)
{
    /// <summary>
    /// The scenario files' wire: snake_case, indented, Afrikaans written as it is, and as strict as the harness when
    /// reading, so a field the harness wouldn't take is refused here first.
    /// </summary>
    public static readonly JsonSerializerOptions FileJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };
}

/// <summary>One chat message of a sample's input: who says it (<c>user</c>), and what.</summary>
public sealed record SampleBodyMessage(string Role, string Content);

/// <summary>
/// The scenario's own fields: where the user is and their time zone, the follow-ups asked after the first answer, the
/// expected values the checks read, the categories that slice the results, and the role it opens in when that isn't
/// the mode's.
/// </summary>
/// <remarks>
/// The expected values stay a <see cref="JsonElement"/>: a key may hold text, a number, a list or null, and the API
/// never reads them, only keeps and writes them.
/// </remarks>
public sealed record SampleBodyMetadata(
    string? Place,
    string TimeZone,
    IReadOnlyList<string> FollowUps,
    JsonElement Expected,
    IReadOnlyList<string> Categories,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Role = null);
