using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent.Messages;

namespace LouisAgent.Core.Agent.EngineTools;

/// <summary>
/// The system prompt's skill documentation, and the <see cref="ReloadSkills"/> tool that reads it again for the next
/// chat. The engine sends <see cref="Documentation"/> as each conversation's system prompt. Since F97.1 a skill is
/// run only through the door, <c>UseSkill</c>, so nothing here runs one.
/// </summary>
internal sealed class SkillTools
{
    /// <summary>The prompt as the provider gives it, read once when the engine is built.</summary>
    public SkillTools(ISkillProvider skillProvider) =>
        Documentation = AgentEngine.Normalise(skillProvider.Documentation);

    /// <summary>Combined default instructions and skill documentation used as the system prompt.</summary>
    public string Documentation { get; private set; }

    /// <summary>
    /// Rebuilds the prompt's skills from disk for <see cref="ReloadSkills"/>; null disables reloading.
    /// </summary>
    public Func<ISkillProvider>? SkillReloader { get; set; }

    /// <summary>
    /// Reads the prompt's skill files again, so the next chat's system prompt has them. The engine reads its skills
    /// folder again beside it (<c>ReloadingLibraryFunction</c>).
    /// </summary>
    [Description("""
                 Reads the skills folder and the skill files from disk again. Use it after a skill was added or
                 edited outside the agent (by the user, or by another tool): UseSkill reaches it now, and the next
                 chat lists it. Returns an error if the host wasn't configured with a skill reloader.
                 """)]
    public string ReloadSkills()
    {
        if (SkillReloader is null) return MessageText.Format(AgentToolMessage.SkillReloadUnavailable);

        var provider = new CompositeSkillProvider(SkillReloader());
        Documentation = AgentEngine.Normalise(provider.Documentation);
        return MessageText.Format(AgentToolMessage.SkillsReloaded);
    }
}
