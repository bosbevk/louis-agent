namespace LouisAgent.Api.Contracts.Admin.Imports;

/// <summary>Every upload the packs take (F55): <c>GET /v1/admin/imports</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ImportsReply(IReadOnlyList<ImportRow> Imports);

/// <summary>One upload: its name (the last part of its URL), what it is, and the files it takes.</summary>
public sealed record ImportRow(string Name, string Description, IReadOnlyList<ImportSlotRow> Files);

/// <summary>One file an upload takes: its slot's name and what to pick.</summary>
public sealed record ImportSlotRow(string Name, string Description);

/// <summary>
/// What the page sends to preview or save an upload: each file with its slot, its name on the person's computer and
/// its bytes (base64 in the JSON).
/// </summary>
public sealed record ImportRequest(IReadOnlyList<ImportFileBody>? Files);

/// <summary>One uploaded file.</summary>
public sealed record ImportFileBody(string Slot, string FileName, byte[] Content);

/// <summary>
/// What the pack found and did: the period the files cover, how many things Save writes, whether this call saved
/// them, the facts to check and the problems that stop a save.
/// </summary>
public sealed record ImportReportReply(
    string? Period,
    int Changes,
    bool Saved,
    IReadOnlyList<ImportFactRow> Facts,
    IReadOnlyList<string> Problems);

/// <summary>One line of the report: a label and its value.</summary>
public sealed record ImportFactRow(string Label, string Value);
