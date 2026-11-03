using System.Text.Json;

namespace LouisAgent.Api.Contracts.Admin.Studio;

/// <summary>
/// The catalogue the studio composes an eval from: every check type the runner's evals can name, and every reader a
/// check can read a value with.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA), built by the API's <c>CheckListReply.Of</c> beside its own records
/// (<c>StudioReplies</c>), so this project needs none of them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record CheckListReply(IReadOnlyList<CheckTypeRow> Checks, IReadOnlyList<ReaderRow> Readers);

/// <summary>
/// One check type: its name in an eval file, its kind (<c>fixed-rule</c>, <c>model</c>, <c>human</c>) and the JSON
/// schema of its settings, which the compose form draws its fields from.
/// </summary>
/// <remarks>The schema stays JSON: its shape is each check's settings record, decided in the runner.</remarks>
public sealed record CheckTypeRow(string Type, string Kind, JsonElement Parameters);

/// <summary>
/// One reader: its name in an eval file and the kind of value it reads (<c>money</c>, <c>number</c>, <c>image</c>,
/// <c>price</c>).
/// </summary>
public sealed record ReaderRow(string Name, string Kind);
