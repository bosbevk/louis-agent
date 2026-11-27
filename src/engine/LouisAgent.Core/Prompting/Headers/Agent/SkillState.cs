namespace LouisAgent.Core.Prompting.Headers.Agent;

/// <summary>
/// Where a skill stands (F97.1): a draft only the chat that made it may use, or approved for every chat. The folder it
/// sits in says which, never a line inside it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0033-what-the-agent-writes-has-a-scope.md">ADR 0033: what the agent writes has a scope</seealso>
public enum SkillState
{
    Drafted,
    Approved,
}
