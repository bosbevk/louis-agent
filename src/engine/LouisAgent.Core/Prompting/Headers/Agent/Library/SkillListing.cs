using LouisAgent.Composition.Messages;
using LouisAgent.Core.Prompting.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Library;

namespace LouisAgent.Core.Prompting.Headers.Agent.Library;

/// <summary>
/// The approved skills as the model is shown them (F97.1): one line each, the name it calls and the line that says
/// what the skill does and when, in the order given. The rest of a skill, its instructions and its parameters, the
/// model reads when it needs it, as the Agent Skills spec loads a skill's body only on demand; so the listing stays
/// short however many skills there are.
/// </summary>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification, progressive disclosure</seealso>
public static class SkillListing
{
    /// <summary>One line a skill; one line saying there are none, when there are none.</summary>
    public static string LinesFor(IReadOnlyList<Skill> skills)
    {
        if (skills.Count == 0) return MessageText.Format(SkillToolMessage.NoSkillsApproved);

        return string.Join('\n', skills.Select(Line));
    }

    /// <summary>A skill's line.</summary>
    private static string Line(Skill skill)
    {
        var listed = new SkillListedHoles(skill.Name, skill.Description);
        return MessageText.Format(SkillToolMessage.SkillListed, listed);
    }
}
