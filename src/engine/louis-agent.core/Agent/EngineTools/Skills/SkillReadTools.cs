using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Agent.EngineTools.Skills.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Headers.Identities;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// Hands the model one of its identity's on-demand skills (F129), by the name its prompt lists. A session only has
/// this when its identity has such a skill, and only that identity's, so it can't read another identity's skills.
/// </summary>
/// <remarks>
/// Progressive disclosure: the prompt carries each skill's name and the line that says when it's needed, and this
/// tool is the step that fetches the rest just in time, as Anthropic's Agent Skills are read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0114-an-identitys-skills-load-always-or-on-demand.md">ADR 0114: an identity's skills load always or on demand</seealso>
public sealed class SkillReadTools(IEnumerable<IdentitySkill> skills)
{
    private readonly IReadOnlyList<IdentitySkill> _onDemand = skills.Where(s => s.Load == SkillLoad.OnDemand).ToList();

    /// <summary>
    /// Whether there's a skill to read on demand, which is when a session gets the tool. Internal, since every public
    /// method here is a tool.
    /// </summary>
    internal bool HasAny => _onDemand.Count > 0;

    [Description(
        "Reads one of your skills that isn't in your prompt: the list 'Skills you read when you need them' names " +
        "each and says when it's needed. Read it before you answer what it covers.")]
    public string ReadSkill(
        [Description("The skill's name as the list writes it, such as identity/skill.")]
        string name) =>
        _onDemand.FirstOrDefault(s => s.Name == name) is { } skill
            ? skill.Body
            : MessageText.Format(SkillReadToolMessage.SkillUnknown, new
            {
                Name = name,
                Available = string.Join(", ", _onDemand.Select(s => s.Name))
            });
}
