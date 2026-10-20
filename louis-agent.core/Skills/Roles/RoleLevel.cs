namespace LouisAgent.Core.Skills.Roles;

/// <summary>
/// The level a session runs at (CL8, ADR 0017), which is its depth: the agent the user talks to, a specialist it
/// delegates to, a minion a specialist hands one task to (F30). Each level reads its role from its own folder
/// (<see cref="RoleCatalog.FolderOf"/>), and only level 0 has the router.
/// </summary>
public enum RoleLevel
{
    /// <summary>Level 0: the agent the user talks to (a product's chat, the CLI, Rider's chat); its voice is in <c>agent/</c>.</summary>
    Agent = 0,

    /// <summary>Level 1: a specialist a level-0 session delegates to; its role is in <c>specialists/</c>.</summary>
    Specialist = 1,

    /// <summary>Level 2: a minion a specialist hands one task to (F30); its function is in <c>minions/</c>.</summary>
    Minion = 2,
}
