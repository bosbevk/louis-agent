using System.Text.Json.Serialization;

namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>
/// One ask in a trial's index (<c>trials/{safe name}.jsonl</c>, one snake_case JSON line each): the seam and key that were asked, when, and
/// either the hash of the answer file that answered or the error's text (never both). A missing hash is null, never an empty string (ADR 0007).
/// </summary>
public sealed record ReplayIndexLine(string Trial, ReplayKind Kind, string Key, DateTimeOffset At, string? Hash, string? Error)
{
    [JsonIgnore]
    public ReplayAsk Ask => new(Kind, Key);
}
