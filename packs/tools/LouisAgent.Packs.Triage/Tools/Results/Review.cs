using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Tools.Results;

/// <summary>What the reviewer said: happy or not, and why.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers</seealso>
public sealed record Review(
    [property: JsonPropertyName("happy")] bool Happy,
    [property: JsonPropertyName("reasons")]
    IReadOnlyList<string> Reasons);
