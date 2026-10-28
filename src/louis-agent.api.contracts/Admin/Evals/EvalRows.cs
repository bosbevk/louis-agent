using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Api.Contracts.Admin.Evals;

/// <summary>The admin's eval list: every eval the runner has, and whether the store could tell us their runs.</summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): a plain shape both sides read. The API builds it with <c>EvalListReply.Of</c>,
/// which lives beside the API's own types (<c>EvalReplies</c>), so this project needs none of them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record EvalListReply(IReadOnlyList<EvalRow> Evals, StoreRow Store);

/// <summary>
/// One eval: what it is (whose, its version, what it proves, its dataset, categories and graders), whether it has a
/// manual, the modes and models it was run on, and its runs, newest first. <see cref="NeverRun"/> is null when there
/// is no store to ask.
/// </summary>
public sealed record EvalRow(
    string Name,
    string? Identity,
    int? Version,
    string Proves,
    bool Manual,
    EvalDatasetRow? Dataset,
    IReadOnlyList<string> Categories,
    IReadOnlyList<EvalGraderRow> Graders,
    IReadOnlyList<string> Modes,
    IReadOnlyList<string> Models,
    bool? NeverRun,
    IReadOnlyList<EvalRunRow> Runs);

/// <summary>The dataset an eval's scenarios came from, by name and version.</summary>
public sealed record EvalDatasetRow(string Name, int Version);

/// <summary>One of an eval's graders: its name, its version and what it judges with.</summary>
public sealed record EvalGraderRow(string Name, int Version, string Kind);

/// <summary>One run of an eval: the stored run's own row (outcome and cost), and each mode with its rates.</summary>
public sealed record EvalRunRow(StoredRunRow Run, IReadOnlyList<ModeRow> Modes);
