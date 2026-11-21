namespace LouisAgent.Web.Components.Recorded;

/// <summary>
/// One turn of a recorded conversation (F128.2): what the visitor asked, and where the stream that answered it is.
/// </summary>
/// <param name="Question">The question, shown as the visitor's message.</param>
/// <param name="Source">Where the turn's recording is, from the site's own address: a <c>.sse</c> file.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecordedTurn(string Question, string Source);
