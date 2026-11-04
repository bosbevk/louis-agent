namespace LouisAgent.Puls.Admin.Starting;

/// <summary>
/// The body of a post whose address says everything (a judge check, a re-score): it serializes to <c>{}</c>.
/// </summary>
/// <remarks>A named body instead of an anonymous <c>new { }</c>, as every serialized body is.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record NoBody
{
    /// <summary>The one empty body.</summary>
    public static NoBody Instance { get; } = new();
}
