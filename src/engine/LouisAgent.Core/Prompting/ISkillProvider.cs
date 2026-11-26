namespace LouisAgent.Core.Prompting;

/// <summary>
/// Where a piece of the system prompt comes from: a file, a text built in code, or several at once. The engine sends
/// what they give as each conversation's system prompt.
/// </summary>
public interface ISkillProvider
{
    /// <summary>The prompt text this source gives.</summary>
    string Documentation { get; }
}
