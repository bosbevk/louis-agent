using LouisAgent.Choices;

namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// How an identity's skill reaches the model (F129, Louis's pick: both): always, in the prompt beside its voice, or on
/// demand, listed by name and read with <c>ReadSkill</c> when a question needs it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum SkillLoad
{
    [WireName("always")] Always,
    [WireName("on-demand")] OnDemand,
}
