using System.Text.Json.Nodes;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// One line of the catalogue: a check type's name, how it decides, and the JSON schema of its settings, which a
/// form can be made from.
/// </summary>
/// <remarks>
/// The schema is a <see cref="JsonNode"/> because its shape depends on each entry's record, decided at run time
/// (ADR 0041's one case for a node over a record).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record ListedCheck(string Type, CheckKind Kind, JsonNode Parameters);
