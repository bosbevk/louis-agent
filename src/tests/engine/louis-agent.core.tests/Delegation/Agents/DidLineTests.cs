using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// The line under an asked agent's answer says what it did in a few words (F127.3.1 step 9): each tool once, in the
/// order first called, with how often and how many failed.
/// </summary>
public class DidLineTests
{
    /// <summary>A call that worked.</summary>
    private static AgentWork.ToolCall Ok(string name) => new(name, false);

    /// <summary>A call that failed.</summary>
    private static AgentWork.ToolCall Failed(string name) => new(name, true);

    /// <summary>An answer with no tool says so.</summary>
    [Test]
    public void NoCalls_SaysNoTools() =>
        Assert.That(DidLine.Of([]), Is.EqualTo("no tools"));

    /// <summary>One call is its name.</summary>
    [Test]
    public void OneCall_IsItsName() =>
        Assert.That(DidLine.Of([Ok("SendLead")]), Is.EqualTo("SendLead"));

    /// <summary>Several are each named once, in the order first called, a repeated one with how often.</summary>
    [Test]
    public void SeveralCalls_AreNamedOnce_InOrder_WithHowOften() =>
        Assert.That(DidLine.Of([Ok("FetchUrl"), Ok("GetToday"), Ok("FetchUrl")]), Is.EqualTo("FetchUrl ×2, GetToday"));

    /// <summary>A failed call says it failed, and a repeated one how many of its calls did.</summary>
    [Test]
    public void AFailedCall_SaysSo() =>
        Assert.That(DidLine.Of([Failed("SendLead"), Ok("FetchUrl"), Failed("FetchUrl"), Ok("FetchUrl")]),
            Is.EqualTo("SendLead (failed), FetchUrl ×3 (1 failed)"));
}
