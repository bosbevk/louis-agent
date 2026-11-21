using System.Text.Json.Serialization;
using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Api;

/// <summary>
/// A message for the session to answer, with the files the user attached and, when it travels, the page they are
/// reading (F115); without one the wire is as it was, the page key left out.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): a named body in the browser app, written with the web's default options,
/// camelCase. The opening it goes with is the session wire's shared <c>CreateSessionRequest</c> since F127.3.1.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record MessageBody(
    string Message,
    IEnumerable<Attachment> Attachments,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Page? Page = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<WindowPage>? Windows = null);
