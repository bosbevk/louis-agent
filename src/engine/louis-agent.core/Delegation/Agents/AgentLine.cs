using LouisAgent.Composition.Agents;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// Finds the conversation this chat already has with an agent, by its address, so asking it again carries on there
/// instead of starting over.
/// </summary>
internal static class AgentLine
{
    /// <summary>The asked agent this chat keeps at <paramref name="address"/>, if there is one.</summary>
    public static OpenSubSession? Pick(SubSessionRegistry kept, AgentAddress address) =>
        kept.Open.FirstOrDefault(open => open.Kind == OpenSubSession.AgentKind && open.Address == address);
}
