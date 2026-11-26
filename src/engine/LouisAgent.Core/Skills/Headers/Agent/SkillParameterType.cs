using LouisAgent.Choices;

namespace LouisAgent.Core.Skills.Headers.Agent;

/// <summary>
/// The kind of value a skill's parameter takes: JSON Schema's own type names, the six a call's arguments can hold.
/// </summary>
/// <seealso href="https://json-schema.org/understanding-json-schema/reference/type">JSON Schema: type-specific keywords</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum SkillParameterType
{
    [WireName("string")] String,
    [WireName("number")] Number,
    [WireName("integer")] Integer,
    [WireName("boolean")] Boolean,
    [WireName("array")] Array,
    [WireName("object")] Object,
}
