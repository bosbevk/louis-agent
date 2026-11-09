using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Tools.Results;

/// <summary>What the sorter said: which department the error belongs to, and why.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers</seealso>
public sealed record Sorting(
    [property: JsonPropertyName("department")]
    string Department,
    [property: JsonPropertyName("why")] string Why);
