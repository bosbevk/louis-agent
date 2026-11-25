using System.Text.Json.Serialization;

namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>
/// A file that goes with a message: its name and its text, or, for a photo or a PDF a tool reads (F171.1), its bytes
/// and its media type instead. The bytes travel in the message's JSON as base64, as the price import sends a file.
/// </summary>
/// <remarks>
/// The two file members come last and default to none, so every caller that sends text compiles as it did; an unset
/// one is left out of the JSON, as <see cref="SendMessageRequest" /> leaves out its page, so a text attachment writes
/// what it always wrote.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record AttachmentRequest(
    string Name,
    string? Content,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    byte[]? Bytes = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? MediaType = null);
