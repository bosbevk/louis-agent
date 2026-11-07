namespace LouisAgent.Api.Contracts.Admin.Screens;

/// <summary>Every screen the packs offer, for the sidebar and the page: <c>GET /v1/admin/screens</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ScreensReply(IReadOnlyList<ScreenReply> Screens);

/// <summary>One screen's description: its name, heading and sentence, its columns, and whether it searches.</summary>
public sealed record ScreenReply(
    string Name,
    string Title,
    string Description,
    IReadOnlyList<ScreenColumnReply> Columns,
    bool Searchable)
{
    /// <summary>What can be done from the screen, so the page can draw its buttons.</summary>
    public IReadOnlyList<ScreenActionReply> Actions { get; init; } = [];
}

/// <summary>One column: its heading, and the wire name of the kind of value it holds.</summary>
public sealed record ScreenColumnReply(string Title, string Kind);

/// <summary>One page of a screen: <c>GET /v1/admin/screens/{name}/rows</c>.</summary>
public sealed record ScreenPageReply(IReadOnlyList<ScreenRowReply> Rows, long Total, int PageSize);

/// <summary>One row: what identifies it, and a value per column. Null is "unknown", never 0 or empty.</summary>
public sealed record ScreenRowReply(string Id, IReadOnlyList<string?> Cells);
