namespace LouisAgent.Core.Skills.Roles;

using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Messages;

/// <summary>Which roles this instance offers, and the file of one (F29, CL8, CL15; S, from AgentHost).</summary>
public static class RoleFiles
{
    /// <summary>
    /// The skill file of <paramref name="role"/>: the default mode's when none is named, else its file in
    /// <paramref name="level"/>'s folder, which must be built in. A name from another level is refused as an unknown one
    /// (CL8), so a role never opens at the wrong level; at level 0 only a mode is a name (CL9), never a function skill.
    /// </summary>
    internal static string FileOf(AgentOptions agent, string? role, RoleLevel level)
    {
        if (string.IsNullOrEmpty(role))
        {
            // A minion with no role would be the coding agent's voice with one tool (F30): it always opens with a role.
            if (level == RoleLevel.Minion)
                throw new RoleException(MessageText.Format(SkillErrorMessage.MinionNeedsRole,
                    new { Available = string.Join(", ", Enabled(agent, level)) }));
            return RoleCatalog.FileName(RoleCatalog.DefaultVoice, RoleLevel.Agent);
        }
        if (!RoleCatalog.IsValidName(role))
            throw new RoleException(MessageText.Format(SkillErrorMessage.RoleNameInvalid, new { Name = role }));
        string file = RoleCatalog.FileName(role, level);
        // A role that's off (F29) is refused as an unknown one: the list names what this instance may use. At level
        // 0 only a mode is a name (CL9); at level 2 a role on the base (F30), never the base itself.
        bool offered = level == RoleLevel.Specialist || RoleCatalog.Available(level).Contains(role);
        if (!offered || !IsEnabled(agent, role) || SkillFolder.Read(agent, file) is null)
            throw new RoleException(MessageText.Format(SkillErrorMessage.UnknownRole,
                new { Name = role, Available = string.Join(", ", Enabled(agent, level)) }));
        return file;
    }

    /// <summary>
    /// Start-up's check of the roles (CL15): every <c>ROLES_ENABLED</c> name must be a role this instance has, at some
    /// level, and <paramref name="defaultMode"/>, the mode of a session that names none, must have its file. Before the
    /// cut an unknown name was dropped in silence (it simply never matched), and a session with no role was handed the
    /// default before any check, so an identity missing from <c>IDENTITIES</c> would have shown as a 400 at the first chat.
    /// </summary>
    internal static void Check(AgentOptions agent, string defaultMode)
    {
        var known = Enum.GetValues<RoleLevel>().SelectMany(RoleCatalog.Available).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = agent.RolesEnabled.Where(r => !known.Contains(r)).ToList();
        if (unknown.Count > 0)
            throw new RoleException(MessageText.Format(SkillErrorMessage.UnknownRolesEnabled,
                new { Names = string.Join(", ", unknown), Available = string.Join(", ", known.Order(StringComparer.Ordinal)) }));

        string file = RoleCatalog.FileName(defaultMode, RoleLevel.Agent);
        if (SkillFolder.Read(agent, file) is null)
            throw new RoleException(MessageText.Format(SkillErrorMessage.DefaultModeMissing, new { Mode = defaultMode, File = file }));
    }

    /// <summary>
    /// The roles this instance offers at <paramref name="level"/>: the built-in ones, or only the enabled ones when
    /// the list is set (F29).
    /// </summary>
    internal static IReadOnlyList<string> Enabled(AgentOptions agent, RoleLevel level) =>
        RoleCatalog.Available(level).Where(name => IsEnabled(agent, name)).ToList();

    private static bool IsEnabled(AgentOptions agent, string role) =>
        agent.RolesEnabled.Count == 0 || agent.RolesEnabled.Contains(role, StringComparer.OrdinalIgnoreCase);
}
