using System.Text.Json;

namespace LouisAgent.Core.Tools.Agents.SubSessions;

/// <summary>
/// What one reply of a sub-session says it used: the four kinds of tokens, how many requests, and what it was charged
/// in the child's own currency (never the converted <c>display_cost</c>). It goes to the parent's turn and the ledger,
/// never into the text the model reads: on a replay it is zero, and the parent's next request must still match its
/// recording (FX27).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <param name="Requests">Null when the peer didn't report a count (then the roll-up counts as one request).</param>
internal sealed record SubSessionUsage(
    long Input,
    long CacheWrite,
    long CacheRead,
    long Output,
    int? Requests,
    decimal? Cost,
    string? Currency)
{
    /// <summary>
    /// Reads the API's usage object, from the end of a reply or from its error event (FX6). A field that's missing
    /// counts as nothing.
    /// </summary>
    public static SubSessionUsage From(JsonElement usage)
    {
        long Count(string name) => usage.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt64()
            : 0;

        long requests = Count("requests");
        return new(
            Count("input_tokens"), Count("cache_creation_input_tokens"), Count("cache_read_input_tokens"),
            Count("output_tokens"),
            requests > 0 ? (int)requests : null,
            usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : null,
            usage.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String
                ? cur.GetString()
                : null);
    }
}
