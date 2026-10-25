using System.Text.Json.Serialization;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// One visit in a trial's index (<c>trials/{safe name}.jsonl</c>, one snake_case JSON line each): what was asked, when, and either the
/// hash of the page file that answered or the error's text (never both). A missing hash is null, never an empty string (ADR 0007).
/// </summary>
public sealed record ReplayIndexLine(string Trial, string Url, string? Script, string? ReadySelector, DateTimeOffset At, string? Hash, string? Error)
{
    [JsonIgnore]
    public ReplayAsk Ask => new(Url, Script, ReadySelector);
}
