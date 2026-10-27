
using System.Text;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Core.Tools.Agents.SubSessions;
/// <summary>A sub-session's SSE stream read to the end (S, CL15, from SpecialistTools), through the one session-stream
/// reader (F18 step 1c).</summary>
internal static class SubSessionStream
{
    /// <summary>
    /// The answer's text, the usage from message_delta (or the error event), and the error, from the SSE stream; every
    /// event also goes to <paramref name="onEvent"/> as it arrives (the live view of the child).
    /// </summary>
    public static async Task<(string Text, SubSessionUsage? Usage, string? Error)> ReadAsync(Stream stream,
        Action<string, string> onEvent,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        SubSessionUsage? usage = null;
        string? error = null;
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, cancellationToken))
        {
            onEvent(streamEvent.Type, streamEvent.Data.GetRawText());
            switch (streamEvent)
            {
                case ContentBlockDelta when streamEvent.Data.GetProperty("delta") is var delta
                                            && delta.GetProperty("type").GetString() == "text_delta":
                    text.Append(delta.GetProperty("text").GetString());
                    break;
                case MessageDelta when streamEvent.Data.TryGetProperty("usage", out var used) &&
                                       used.ValueKind == JsonValueKind.Object:
                    usage = SubSessionUsage.From(used);
                    break;
                case ErrorReported:
                    error = streamEvent.Data.GetProperty("error").GetProperty("message").GetString();
                    if (streamEvent.Data.TryGetProperty("usage", out var failed) &&
                        failed.ValueKind == JsonValueKind.Object)
                        usage = SubSessionUsage.From(failed);
                    break;
            }
        }

        return (text.ToString(), usage, error);
    }
}
