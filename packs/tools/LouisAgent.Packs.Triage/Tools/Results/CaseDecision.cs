using System.Text.Json.Serialization;
using LouisAgent.Packs.Triage.Tools.Choices;

namespace LouisAgent.Packs.Triage.Tools.Results;

/// <summary>What the manager decided a case gets, and why.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers</seealso>
public sealed record CaseDecision(
    [property: JsonPropertyName("kind")] CaseDecisionKind Kind,
    [property: JsonPropertyName("reason")] string Reason);
