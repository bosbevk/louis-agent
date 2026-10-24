using LouisAgent.Api.Sessions.Streaming.Claude;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>A session's format, made for each turn and for its view (F18 step 1f): the choice the session made, kept for its lifetime.</summary>
internal static class StreamFormats
{
    public static ITurnStreamFormat Create(StreamFormat format, string sessionId, IReadOnlyList<string>? toolsets, string? role) => format switch
    {
        StreamFormat.Claude => new ClaudeStreamFormat(sessionId, toolsets, role),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };
}
