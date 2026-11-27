using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The model's tool for reading the skills folder again (F97.1): a skill added or changed by hand is reached through
/// <c>UseSkill</c> at once, and the next chat lists it.
/// </summary>
/// <remarks>
/// The reload is the engine's library's, handed in, so this class knows nothing of where the folder is. The system
/// prompt isn't read again: all of it is built in, so a reload would give the same text.
/// </remarks>
internal sealed class SkillReloadTools(Action reloadLibrary)
{
    /// <summary>Reads the skills folder again.</summary>
    [Description("""
                 Reads the skills folder from disk again. Use it after a skill was added or edited outside the agent
                 (by the user, or by another tool): UseSkill reaches it now, and the next chat lists it.
                 """)]
    public string ReloadSkills()
    {
        reloadLibrary();
        return MessageText.Format(AgentToolMessage.SkillsReloaded);
    }
}
