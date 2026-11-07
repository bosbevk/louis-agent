namespace LouisAgent.Api.Contracts.Admin.Screens;

/// <summary>One row as a form: <c>GET /v1/admin/screens/{name}/rows/{id}</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ScreenRecordReply(string Title, IReadOnlyList<ScreenFieldReply> Fields);

/// <summary>One field: its name and label, the wire name of its kind, and what it holds.</summary>
public sealed record ScreenFieldReply(
    string Name,
    string Label,
    string Kind,
    string? Value,
    bool ReadOnly,
    bool Required,
    IReadOnlyList<string> RowColumns,
    IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>Saving a form: <c>PUT /v1/admin/screens/{name}/rows/{id}</c>, the plain fields and the fields of rows.</summary>
public sealed record ScreenSaveRequest(
    IReadOnlyDictionary<string, string?>? Values,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>>? Rows);
