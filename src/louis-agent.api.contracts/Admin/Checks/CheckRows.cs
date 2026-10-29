using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Api.Contracts.Admin.Checks;

/// <summary>One check in the page's list: what it is called and whether it has a manual to open.</summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): a plain shape both sides read. The API builds it with <c>CheckRow.Of</c>,
/// which lives beside the API's own types (<c>AdminReplies</c>), so this project needs none of them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record CheckRow(string Id, string Title, bool Manual);

/// <summary>
/// Whether the store is there, and why not when it was asked and didn't answer: what the page's banner shows.
/// </summary>
public sealed record StoreRow(bool Configured, string? Error);

/// <summary>What the admin page reads first: the checks, the store's state and the stored runs.</summary>
public sealed record BenchmarkListReply(
    IReadOnlyList<CheckRow> Checks,
    StoreRow Store,
    IReadOnlyList<StoredRunRow> Runs);

/// <summary>A check's manual: which check, its title and the markdown beside its script.</summary>
public sealed record ManualReply(string Check, string Title, string Markdown);
