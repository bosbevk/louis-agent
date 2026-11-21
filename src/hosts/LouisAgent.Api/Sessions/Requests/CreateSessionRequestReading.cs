using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Choices;
using LouisAgent.Core.Sessions;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// How the API reads a request to open a session: the role and identity trimmed, the stream format parsed, where the
/// user is, and the tags checked before anything is built.
/// </summary>
/// <remarks>
/// Extension methods over the shared <see cref="CreateSessionRequest"/> (F127.3.1): the record is the wire's and holds
/// only data, while these reads need the API's own types (the stream format, the user's place), so they stay here and
/// the callers read <c>request.RoleOrNull()</c> as before.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal static class CreateSessionRequestReading
{
    /// <summary>The longest a tag may be.</summary>
    internal const int MaxTagLength = 200;

    /// <summary>The role asked for, trimmed; none sent is the default voice (CL9).</summary>
    public static string? RoleOrNull(this CreateSessionRequest request) =>
        string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim();

    /// <summary>The identity asked for (F109.3), trimmed; none sent is the host's first.</summary>
    public static string? IdentityOrNull(this CreateSessionRequest request) =>
        string.IsNullOrWhiteSpace(request.Identity) ? null : request.Identity.Trim();

    /// <summary>
    /// The format asked for with <c>stream</c>: Claude when none is sent; null for a name we don't know (the 400).
    /// </summary>
    public static StreamFormat? StreamFormatOrNull(this CreateSessionRequest request) =>
        SettingChoice.TryParse(request.Stream, out StreamFormat? format) ? format ?? StreamFormat.Claude : null;

    /// <summary>Where the user is (F22): their location when they shared it, their time zone when sent.</summary>
    public static UserPlace? Place(this CreateSessionRequest request) =>
        string.IsNullOrWhiteSpace(request.Location) && string.IsNullOrWhiteSpace(request.TimeZone)
            ? null
            : new UserPlace(string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
                string.IsNullOrWhiteSpace(request.TimeZone) ? null : request.TimeZone.Trim());

    /// <summary>
    /// The first tag that is too long or holds control characters, by its name; none when all are fine.
    /// </summary>
    public static string? InvalidTag(this CreateSessionRequest request) =>
        new[]
            {
                ("task", request.Task), ("run", request.Run), ("service", request.Service),
                ("parent", request.Parent), ("location", request.Location), ("replay.of", request.Replay?.Of)
            }
            .FirstOrDefault(t => t.Item2 is { } value && (value.Length > MaxTagLength || value.Any(char.IsControl)))
            .Item1;
}
