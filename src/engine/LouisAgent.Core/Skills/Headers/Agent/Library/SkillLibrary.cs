using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Prompts;
using LouisAgent.Choices;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Messages.Holes.Drafts;
using LouisAgent.Composition.Messages;
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

    private readonly string _skillsFolder;

    private SkillLibrary(string skillsFolder)
    {
        _skillsFolder = skillsFolder;
        Approved = SkillsIn(skillsFolder, SkillState.Approved);
    }

    /// <summary>The approved skills, by name; read again when one is approved.</summary>
    public IReadOnlyList<Skill> Approved { get; private set; }

    /// <summary>
    /// The approved skills changed, by an approval or a reload: the door's listing is different, which a host that
    /// publishes the engine's tools, as the MCP server does, republishes.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The skill a chat may use by this name (F97.1): an approved one, or a draft only when the chat made it, read
    /// fresh from its folder, so another chat's draft is never found.
    /// </summary>
    /// <param name="name">The skill's name.</param>
    /// <param name="ownDrafts">The names of the drafts the asking chat made.</param>
    public Skill? Find(string name, IReadOnlySet<string> ownDrafts)
    {
        if (Approved.FirstOrDefault(skill => skill.Name == name) is { } approved) return approved;
        if (!ownDrafts.Contains(name)) return null;

        string draft = Path.Combine(_skillsFolder, SkillFiles.DraftSkill(name));
        return Directory.Exists(draft) ? SkillReader.Read(draft, SkillState.Drafted) : null;
    }

    /// <summary>The drafts waiting in the skills folder, by name, for <c>/skills</c>.</summary>
    public IReadOnlyList<Skill> Drafts =>
        SkillsIn(Path.Combine(_skillsFolder, SkillFiles.DraftsFolder), SkillState.Drafted);

    /// <summary>
    /// Approves a draft (F97.1): its folder moves up a level, replacing an approved skill of its name, so it is
    /// approved at once, for every chat; none when there is no draft by this name.
    /// </summary>
    public string? Approve(string name)
    {
        string draft = Path.Combine(_skillsFolder, SkillFiles.DraftSkill(name));
        if (!Directory.Exists(draft)) return null;

        string approved = Path.Combine(_skillsFolder, SkillFiles.ApprovedSkill(name));
        if (Directory.Exists(approved)) Directory.Delete(approved, recursive: true);
        Directory.Move(draft, approved);
        Reload();

        var done = new DraftApprovedHoles(name);
        return MessageText.Format(UserText.DraftApproved, done);
    }

    /// <summary>Rejects a draft: its folder is deleted; none when there is no draft by this name.</summary>
    public string? Reject(string name)
    {
        string draft = Path.Combine(_skillsFolder, SkillFiles.DraftSkill(name));
        if (!Directory.Exists(draft)) return null;

        Directory.Delete(draft, recursive: true);
        var done = new DraftRejectedHoles(name);
        return MessageText.Format(UserText.DraftRejected, done);
    }

    /// <summary>
    /// Reads the approved skills again, for a skill dropped in by hand or changed on disk (F97.1): reachable at once
    /// through the door, and listed in the next chat.
    /// </summary>
    public void Reload()
    {
        Approved = SkillsIn(_skillsFolder, SkillState.Approved);
        Changed?.Invoke();
    }

    /// <summary>The library of this skills folder; an empty one when the folder isn't there.</summary>
    public static SkillLibrary Read(string skillsFolder) => new(skillsFolder);

    /// <summary>
    /// The skills in a folder, by name: every folder holding a <c>SKILL.md</c>, the drafts folder left out; a skill
    /// that won't read is logged and left out.
    /// </summary>
    private static IReadOnlyList<Skill> SkillsIn(string folder, SkillState state)
    {
        if (!Directory.Exists(folder)) return [];

        return Directory.EnumerateDirectories(folder)
            .Where(skill => Path.GetFileName(skill) != SkillFiles.DraftsFolder)
            .Where(skill => File.Exists(Path.Combine(skill, PromptFileName.Skill.Wire())))
            .Order(StringComparer.Ordinal)
            .Select(skill => ReadOrLeaveOut(skill, state))
            .OfType<Skill>()
            .ToList();
    }

    /// <summary>The skill in this folder, or none when it won't read, its reason logged.</summary>
    private static Skill? ReadOrLeaveOut(string folder, SkillState state)
    {
        try
        {
            return SkillReader.Read(folder, state);
        }
        catch (SkillRefusedException refused)
        {
            var leftOut = new SkillLeftOutHoles(folder, refused.Message);
            Log.Log(LogLevel.Warning, SkillLogMessage.SkillLeftOut, leftOut);
            return null;
        }
    }
}
