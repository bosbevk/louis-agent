namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// One relative <c>url(…)</c> in a web app's stylesheet: the area file it is written in, the path as written, and the
/// file on disk the browser reaches from that file's folder (FX30).
/// </summary>
/// <remarks>Value Object (Fowler): a record, as ADR 0040 asks of a value, in place of a three-part tuple.</remarks>
/// <param name="File">The area file, relative to the css folder (<c>chat/finish.css</c>).</param>
/// <param name="Url">The path as written in the rule (<c>../../brand/puls-icon-tile.svg</c>).</param>
/// <param name="Target">The full path on disk it resolves to.</param>
public sealed record StylesheetUrl(string File, string Url, string Target);
