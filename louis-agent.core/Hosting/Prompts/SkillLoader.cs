namespace LouisAgent.Core.Hosting.Prompts;

using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.Logging;

/// <summary>The system prompt assembled from skill files (S, CL15, from AgentHost): the principles, the role, the guides, the function skills.</summary>
public static class SkillLoader
{
    private static readonly ILogger Log = AgentLogging.For<CompositeSkillProvider>();

    /// <summary>The toolset the function skill files come with (ExecuteSkill runs their procedures).</summary>
    private const string SkillsToolset = "skills";

    /// <summary>The system prompt with every toolset's guide (hosts that load every toolset, and tests).</summary>
    public static ISkillProvider Load(AgentOptions agent) => Load(agent, ToolsetCatalog.All);

    /// <summary>
    /// Loads the system prompt for a toolset profile (F5): role, default.md (the router), the guide of each of
    /// <paramref name="toolsets"/> and no other (docs/CLAUDE.md, *Adding a toolset*), then, when the profile has the skills
    /// toolset, every function skill the agent wrote (agent/functions/*.md in the skills folder; CL9). Our own skill files are plain markdown read by
    /// <see cref="MarkdownSkillProvider"/>; a provider class of its own is only for skills we don't control.
    /// Built-in files are read only from the assembly (<see cref="SkillFileSource"/>, CL8 step 4); only what the agent
    /// wrote (its function files) comes from the skills folder (<see cref="SkillFolder"/>).
    /// </summary>
    /// <param name="role">The session's role (F21): at level 0 a mode, one file of voice and routing (CL9:
    /// <c>acme/voice-en</c>); below it a role. Null: the default mode (<see cref="RoleCatalog.DefaultVoice"/>), and below
    /// level 0 only its voice, since a sub-session's toolsets were chosen for it (F21). <see cref="RoleException"/>
    /// for a name that isn't valid or has no file there.</param>
    /// <param name="level">The session's level (CL8): where its role is.</param>
    public static ISkillProvider Load(AgentOptions agent, IEnumerable<ToolsetDefinition> toolsets, string? role = null,
        RoleLevel level = RoleLevel.Agent)
    {
        var composite = new CompositeSkillProvider();

        // The principles first, for every agent at every level (ADR 0020); then the role, so who the agent is (and,
        // for a mode, how it routes) frames everything after it
        AddBuiltIn(agent, composite, SkillFiles.Principles);
        string roleFile = RoleFiles.FileOf(agent, role, level);
        // A minion's base ({product}-minion.md) frames its role (F30): what every minion is, then what this one does
        if (level == RoleLevel.Minion && role is not null && RoleCatalog.MinionBaseOf(role) is { } minionBase)
            AddBuiltIn(agent, composite, RoleCatalog.FileName(minionBase, RoleLevel.Minion));
        if (role is null && level != RoleLevel.Agent && Read(agent, roleFile) is { } mode)
            composite.AddProvider(MarkdownSkillProvider.FromText(RoleCatalog.ModeParts(mode.Text).Voice));
        else
            AddBuiltIn(agent, composite, roleFile);
        // A language mode ({product}/voice-{xx}) says how it talks; its product's shared voice.md routes (CL9 step 6)
        if (level == RoleLevel.Agent && role is not null && RoleCatalog.LanguageOf(role) is not null)
            AddBuiltIn(agent, composite, RoleCatalog.FileName(RoleCatalog.SharedOf(role), RoleLevel.Agent));

        // The guides of this profile's toolsets only, so the prompt never describes tools the session doesn't have
        var profile = toolsets.ToList();
        foreach (string fileName in ToolsetCatalog.GuideFiles(profile))
            AddBuiltIn(agent, composite, fileName);

        // Function skill files are procedures ExecuteSkill runs: they come with the skills toolset, like a guide with its
        // tools. A profile without it (a product's chats with only their own toolsets) gets its mode and its guides, no skill file.
        if (profile.All(t => t.Name != SkillsToolset)) return composite;

        // Every function skill the agent wrote (CL9: no setting picks one; none is built in any more)
        foreach (string file in SkillFolder.Discover(agent).Where(f => !SkillFiles.IsBuiltIn(SkillFiles.Function(Path.GetFileNameWithoutExtension(f)))))
        {
            composite.AddProvider(new MarkdownSkillProvider(file));
            Log.Log(LogLevel.Information, LogMessage.SkillsFileDiscovered, new { File = Path.GetFileName(file) });
        }

        return composite;
    }

    /// <summary>A skill file as the model reads it: a built-in one without its role header (F33), which is for the loader.</summary>
    internal static (string Text, bool FromDisk)? Read(AgentOptions agent, string fileName) =>
        SkillFolder.Read(agent, fileName) is { } file ? (file.FromDisk ? file.Text : RoleHeader.Split(file.Text).Body, file.FromDisk) : null;

    /// <summary>Adds a skill file to the prompt (<see cref="Read"/>); a missing one is skipped.</summary>
    private static void AddBuiltIn(AgentOptions agent, CompositeSkillProvider composite, string fileName)
    {
        if (Read(agent, fileName) is not { } file) return;
        composite.AddProvider(MarkdownSkillProvider.FromText(file.Text));
        Log.Log(LogLevel.Information, file.FromDisk ? LogMessage.SkillProviderLoaded : LogMessage.SkillFileEmbedded,
            new { Provider = fileName });
    }
}
