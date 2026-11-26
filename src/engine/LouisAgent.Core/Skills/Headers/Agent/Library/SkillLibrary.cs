using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Prompts;
using LouisAgent.Choices;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Skills.Headers.Agent.Reading;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Library;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Skills.Headers.Agent.Library;

/// <summary>
/// The skills an engine has (F97.1): every skill folder in the skills folder but <c>drafts/</c>, approved, in name
/// order, so what a chat is shown is the same on every run and its cached prompt holds. A folder with no
/// <c>SKILL.md</c> isn't a skill and is passed over; a skill that won't read is logged with its reason and left out, so
/// one broken skill never keeps the others from a chat.
/// </summary>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
public sealed class SkillLibrary
{
    private static readonly ILogger Log = AgentLogging.For<SkillLibrary>();

    private SkillLibrary(IReadOnlyList<Skill> approved) => Approved = approved;

    /// <summary>The approved skills, by name.</summary>
    public IReadOnlyList<Skill> Approved { get; }

    /// <summary>The library of this skills folder; an empty one when the folder isn't there.</summary>
    public static SkillLibrary Read(string skillsFolder)
    {
        if (!Directory.Exists(skillsFolder)) return new SkillLibrary([]);

        var approved = Directory.EnumerateDirectories(skillsFolder)
            .Where(folder => Path.GetFileName(folder) != SkillFiles.DraftsFolder)
            .Where(folder => File.Exists(Path.Combine(folder, PromptFileName.Skill.Wire())))
            .Order(StringComparer.Ordinal)
            .Select(ReadOrLeaveOut)
            .OfType<Skill>()
            .ToList();
        return new SkillLibrary(approved);
    }

    /// <summary>The approved skill in this folder, or none when it won't read, its reason logged.</summary>
    private static Skill? ReadOrLeaveOut(string folder)
    {
        try
        {
            return SkillReader.Read(folder, SkillState.Approved);
        }
        catch (SkillRefusedException refused)
        {
            var leftOut = new SkillLeftOutHoles(folder, refused.Message);
            Log.Log(LogLevel.Warning, SkillLogMessage.SkillLeftOut, leftOut);
            return null;
        }
    }
}
