using System.Text.Json.Serialization;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Api;

/// <summary>
/// What the web apps send to open a session: the toolsets it keeps (F5), its voice (F21), and where the user is, only
/// when they shared it (F22).
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): a named body in the browser app, as the portal's <c>StartRunBody</c>
/// (finding 9), since the API's own request records stay in the API for now (finding 3). Written with the web's
/// default options, camelCase, as the anonymous object it replaces was.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record CreateSessionBody(
    IEnumerable<string> Toolsets,
    string? Role,
    string? Location,
    [property: JsonPropertyName("time_zone")]
    string? TimeZone);

/// <summary>A message for the session to answer, with the files the user attached.</summary>
public sealed record MessageBody(string Message, IEnumerable<Attachment> Attachments);
