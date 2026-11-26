using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The door's tool with the approved skills listed after its description (F97.1), read when the tool is described: a
/// chat freezes its tools at its first turn (FX4), so the list it sees is the one from when it started, and a skill
/// approved later is listed in the next chat.
/// </summary>
/// <remarks>
/// A Decorator on Microsoft.Extensions.AI's <see cref="DelegatingAIFunction"/>, as <c>FrozenTool</c> is: the call is
/// the inner function's, and only the description is added to, so the listing never sits in the code's attribute.
/// </remarks>
internal sealed class ListedSkillsFunction(AIFunction door, Func<string> listing) : DelegatingAIFunction(door)
{
    /// <summary>The door's description, then one line for each approved skill.</summary>
    public override string Description => $"{InnerFunction.Description}\n{listing()}";
}
