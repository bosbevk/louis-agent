using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// One model request from the ledger, the way the explorer draws it: when it ended, why, who asked, the model, its
/// tokens, what it cost and how long it took.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record RequestNode(
    DateTimeOffset At,
    string Purpose,
    string? Session,
    string? Parent,
    string? Model,
    long? Input,
    long? CacheWrite,
    long? CacheRead,
    long? Output,
    decimal? Cost,
    long DurationMs)
{
    /// <summary>A specialist made it: its session has a parent.</summary>
    public bool Specialist => Parent is not null;

    /// <summary>All its tokens added up; a kind the ledger didn't know counts as none.</summary>
    public long Tokens => (Input ?? 0) + (CacheWrite ?? 0) + (CacheRead ?? 0) + (Output ?? 0);

    /// <summary>When it started, worked back from when it ended.</summary>
    public DateTimeOffset Start => At - TimeSpan.FromMilliseconds(DurationMs);

    /// <summary>The request as the stored run sent it.</summary>
    /// <remarks>
    /// Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>: the explorer reads the
    /// contract's row once, here, and every chart works on this smaller shape.
    /// </remarks>
    public static RequestNode Of(RequestRow row) =>
        new(row.At, row.Purpose ?? "", row.Session, row.Parent, row.Model, row.InputTokens, row.CacheWriteTokens,
            row.CacheReadTokens, row.OutputTokens, row.Cost, row.DurationMs);
}
