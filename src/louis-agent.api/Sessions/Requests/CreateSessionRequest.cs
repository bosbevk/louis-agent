using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// A new session's options: its toolsets (F5), its role (F21; <c>Personality</c> its alias until the TODO's line removes it), its LLM profile (F30), and optional tags for the usage ledger, kept short
/// and printable. Depth is 1 when another agent opens it as a sub-session.
/// </summary>
internal sealed record CreateSessionRequest(
    string? Task, string? Run, string? Service, List<string>? Toolsets = null, int Depth = 0, string? Parent = null,
    string? Personality = null, string? Location = null,
    [property: System.Text.Json.Serialization.JsonPropertyName("time_zone")] string? TimeZone = null, string? Llm = null,
    string? Tool = null, string? Role = null, string? Stream = null, ReplayRequest? Replay = null)
{
    internal const int MaxTagLength = 200;

    /// <summary>
    /// The role asked for: <c>role</c>, or <c>personality</c>, its name before CL14, from a client one deploy behind;
    /// both, differing, is a <see cref="RoleException"/> (the API's 400), since neither can be guessed to win.
    /// </summary>
    public string? RoleOrAlias()
    {
        string? role = string.IsNullOrWhiteSpace(Role) ? null : Role.Trim();
        string? alias = string.IsNullOrWhiteSpace(Personality) ? null : Personality.Trim();
        if (role is not null && alias is not null && role != alias)
            throw new RoleException(MessageText.Format(ApiUserText.RoleAndPersonalityDiffer, new { Role = role, Personality = alias }));
        return role ?? alias;
    }

    /// <summary>The format asked for with <c>stream</c>: Claude when none is sent; null when the name is unknown (the API's 400).</summary>
    public StreamFormat? StreamFormatOrNull() =>
        SettingChoice.TryParse(Stream, out StreamFormat? format) ? format ?? StreamFormat.Claude : null;

    /// <summary>Where the user is (F22): their location when they shared it, their time zone when sent; null with neither.</summary>
    public UserPlace? Place() => string.IsNullOrWhiteSpace(Location) && string.IsNullOrWhiteSpace(TimeZone)
        ? null
        : new UserPlace(string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(), string.IsNullOrWhiteSpace(TimeZone) ? null : TimeZone.Trim());

    /// <summary>The name of the first tag that is too long or holds control characters, or null when all are fine.</summary>
    public string? InvalidTag() =>
        new[] { ("task", Task), ("run", Run), ("service", Service), ("parent", Parent), ("location", Location), ("replay.of", Replay?.Of) }
            .FirstOrDefault(t => t.Item2 is { } value && (value.Length > MaxTagLength || value.Any(char.IsControl))).Item1;
}
