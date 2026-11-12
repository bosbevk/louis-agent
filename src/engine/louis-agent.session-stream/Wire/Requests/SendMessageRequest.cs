namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>
/// A message for a session to answer, with the files that go with it and, from the browser, the page the visitor is
/// reading (F115) and the pages of the agents' windows on their screen (FX134).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record SendMessageRequest(
    string? Message,
    List<AttachmentRequest>? Attachments,
    PageRequest? Page = null,
    List<WindowPageRequest>? Windows = null);
