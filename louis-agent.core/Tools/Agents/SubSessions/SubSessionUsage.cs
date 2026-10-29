
using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Agents.Messages;

namespace LouisAgent.Core.Tools.Agents.SubSessions;
/// <summary>
/// What one reply of a sub-session reported it used: the API's usage object on <c>message_delta</c> (or on the
/// <c>error</c> event, FX6), with the four token kinds, how many requests they came from, and the charged cost in the
/// child's ledger currency (never <c>display_cost</c>, which is converted for showing).
/// </summary>
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

    /// <summary>The usage line in the tool result: <c>3 requests, 10,200 tokens, 0.0123 USD</c>.</summary>
    public string Line()
    {
        string cost = Cost is { } amount
            ? $"{amount.ToString("0.####", CultureInfo.InvariantCulture)} {Currency ?? ""}".Trim()
            : MessageText.Format(SubSessionToolMessage.SubSessionCostUnknown);
        return MessageText.Format(SubSessionToolMessage.SubSessionUsage, new
        {
            Requests = Requests ?? 0,
            Tokens = (Input + CacheWrite + CacheRead + Output).ToString("N0", CultureInfo.InvariantCulture),
            Cost = cost,
        });
    }
}
