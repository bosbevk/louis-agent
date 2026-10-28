using System.Text.Json.Serialization;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// One line of <c>run --list</c>: an eval as the API reads it, with what the portal shows before the eval ever ran (its
/// dataset, its scenarios' categories and its graders).
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): the API can't reference the runner, so this record is the listing's whole
/// contract, written as snake_case JSON and read back by the API's own <c>EvalLine</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record ListedEval(
    string Name,
    string? Identity,
    int Version,
    string Proves,
    string? Manual,
    ListedDataset? Dataset,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ListedGrader> Graders);

/// <summary>The dataset an eval's scenarios came from, by name and version.</summary>
public sealed record ListedDataset(string Name, int Version);

/// <summary>
/// One of an eval's graders: its name, version and what it judges with, the kind written as the protocol writes it.
/// </summary>
public sealed record ListedGrader(
    string Name,
    int Version,
    [property: JsonConverter(typeof(JsonStringEnumConverter<GraderKind>))]
    GraderKind Kind);
