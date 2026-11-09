using System.ComponentModel;

namespace LouisAgent.Core.Agent.EngineTools.Skills.Messages;

/// <summary>
/// What the model reads about an identity's on-demand skills (F129): the list in the prompt, and what
/// <c>ReadSkill</c> answers.
/// </summary>
public enum SkillReadToolMessage
{
    [Description("# Skills you read when you need them")]
    OnDemandHeading,

    [Description(
        "These are yours, but not in this prompt. When a question needs one, read it first with ReadSkill and its " +
        "name; the line after each name says when.")]
    OnDemandIntro,

    [Description("- `{Name}`: {Description}")]
    OnDemandSkill,
}
