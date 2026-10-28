using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// Turns an event into the line the runner prints, and a printed line back into its event. A line that isn't an event
/// is a person's line, and comes back as nothing.
/// </summary>
/// <remarks>
/// One options object per wire (ADR 0041): snake_case names, the enums by their wire names and never by number, and
/// built once and made read-only, so no caller can change it for everyone else. Reading is strict about our own events
/// (a missing field, or a null where the record has none, is not an event) and tolerant about everything else (an
/// unknown member is skipped, and the event's name may come anywhere in the object).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class EvalProtocolJson
{
    /// <summary>The protocol's one set of serializer options.</summary>
    public static JsonSerializerOptions Options { get; } = Build();

    /// <summary>The line the runner prints for an event: one JSON object, the event's name first.</summary>
    public static string Line(EvalEvent e) => JsonSerializer.Serialize(e, Options);

    /// <summary>
    /// The event a printed line holds, or nothing when the line is a person's or an event we don't know.
    /// </summary>
    public static EvalEvent? Read(string line)
    {
        // Only a line that opens an object can be an event; a person's line costs no parse.
        if (!line.TrimStart().StartsWith('{')) return null;

        // The serializer throws for broken JSON, a missing or unknown event name, and a missing field; each of those
        // is a line for people, not a failure of the run.
        try
        {
            return JsonSerializer.Deserialize<EvalEvent>(line, Options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Builds the options once and locks them.</summary>
    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            // UTF-8 as it is: an "ê" stays an "ê". Quotes, backslashes and control characters (a newline) are still
            // escaped, which is all one line on stdout needs; nothing here is ever put in a web page.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // Our writer puts "event" first, but a line written by hand or by another tool may not.
            AllowOutOfOrderMetadataProperties = true,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
