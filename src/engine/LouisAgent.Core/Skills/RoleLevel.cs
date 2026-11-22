namespace LouisAgent.Core.Skills;

/// <summary>
/// The level a session runs at (CL8), which is its depth: the agent the user talks to, a subagent it delegates to, a
/// minion a subagent hands one task to (F30). A prompt's header names its level (ADR 0133), and only level 0 has the
/// router.
/// </summary>
/// <remarks>
/// At the top of <c>Skills/</c> since CL27, not in <c>Roles/</c>: a prompt's header names its level too, and the
/// skills' folders use only the top (<c>ArchitectureTests</c>), so the level sits where both can reach it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0133-subagents-live-inside-their-agent.md">ADR 0133: subagents live inside their agent</seealso>
public enum RoleLevel
{
    /// <summary>
    /// Level 0: the agent the user talks to (a product's chat, the CLI, Rider's chat); its voice is in
    /// <c>agents/</c>.
    /// </summary>
    Agent = 0,

    /// <summary>
    /// Level 1: a subagent a level-0 session delegates to, on the same loop with less context (CL68.2, Louis: "So it
    /// is actually a SubAgent").
    /// </summary>
    Subagent = 1,

    /// <summary>Level 2: a minion a subagent hands one task to (F30); its function is in <c>minions/</c>.</summary>
    Minion = 2,
}
