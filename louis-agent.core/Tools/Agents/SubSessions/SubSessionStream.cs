namespace LouisAgent.Core.Tools.Agents.SubSessions;

using System.Text;
using System.Text.Json;

/// <summary>A sub-session's SSE stream read to the end (S, CL15, from SpecialistTools; Phase 3 step 1c replaces the loop with the one session-stream reader).</summary>
internal static class SubSessionStream
{
    /// <summary>
    /// The answer's text, the usage from message_delta (or the error event), and the error, from the SSE stream; every
    /// event also goes to <paramref name="onEvent"/> as it arrives (the live view of the child).
    /// </summary>
    public static async Task<(string Text, SubSessionUsage? Usage, string? Error)> ReadAsync(Stream stream, Action<string, string> onEvent,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        SubSessionUsage? usage = null;
        string? error = null, eventName = null;
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal)) { eventName = line[6..].Trim(); continue; }
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            string json = line[5..].Trim();
            if (eventName is not null) onEvent(eventName, json);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            switch (eventName)
            {
                case "content_block_delta" when root.GetProperty("delta") is var delta && delta.GetProperty("type").GetString() == "text_delta":
                    text.Append(delta.GetProperty("text").GetString());
                    break;
                case "message_delta" when root.TryGetProperty("usage", out var used) && used.ValueKind == JsonValueKind.Object:
                    usage = SubSessionUsage.From(used);
                    break;
                case "error":
                    error = root.GetProperty("error").GetProperty("message").GetString();
                    if (root.TryGetProperty("usage", out var failed) && failed.ValueKind == JsonValueKind.Object) usage = SubSessionUsage.From(failed);
                    break;
            }
        }
        return (text.ToString(), usage, error);
    }
}
