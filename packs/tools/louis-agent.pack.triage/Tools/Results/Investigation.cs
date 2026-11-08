using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Tools.Results;

/// <summary>What the investigator found: where the cause is, what it is, whose area, and how sure.</summary>
/// <remarks>The typed ending of the investigator's work (ADR 0041, 0045): a record the workflow reads, never prose.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers</seealso>
public sealed record Investigation(
    [property: JsonPropertyName("where")] string Where,
    [property: JsonPropertyName("cause")] string Cause,
    [property: JsonPropertyName("area")] DepartmentArea Area,
    [property: JsonPropertyName("confidence")]
    Confidence Confidence);
