using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// The line under an asked agent's answer says what it did (F127.3.1 step 9), and since F127.4.2 what each call gave
/// back (Louis's pick 6: "each tool call's own one-line result"): every call in order, with the first line of its
/// result, so the asker retells it right without reading the whole stream.
/// </summary>
public class DidLineTests
{
    /// <summary>A call that worked, and what it gave back.</summary>
    private static AgentWork.ToolCall Ok(string name, string result = "") => new(name, false, result);

    /// <summary>A call that failed, and why.</summary>
    private static AgentWork.ToolCall Failed(string name, string result) => new(name, true, result);

    /// <summary>An answer with no tool says so.</summary>
    [Test]
    public void NoCalls_SaysNoTools() =>
        Assert.That(DidLine.Of([]), Is.EqualTo("no tools"));

    /// <summary>One call is its name and what it gave back.</summary>
    [Test]
    public void OneCall_IsItsNameAndItsResult() =>
        Assert.That(DidLine.Of([Ok("SendLead", "Sent: the owner has the lead.")]),
            Is.EqualTo("SendLead: Sent: the owner has the lead."));

    /// <summary>
    /// Each call is told in the order made, a repeated tool once per call, since each gave its own result.
    /// </summary>
    [Test]
    public void EachCall_IsToldInOrder() =>
        Assert.That(
            DidLine.Of(
                [Ok("ShowPage", "Shown: /rooms/garden"), Ok("ShowCard", "Shown"), Ok("ShowPage", "Shown: /stay")]),
            Is.EqualTo("ShowPage: Shown: /rooms/garden; ShowCard: Shown; ShowPage: Shown: /stay"));

    /// <summary>A failed call says it failed, with its reason.</summary>
    [Test]
    public void AFailedCall_SaysSo_WithItsReason() =>
        Assert.That(DidLine.Of([Failed("SendLead", "Error: not sent, the mail service refused.")]),
            Is.EqualTo("SendLead failed: Error: not sent, the mail service refused."));

    /// <summary>Only the first line of a result is told, cut at 80 characters.</summary>
    [Test]
    public void AResult_IsItsFirstLine_CutShort()
    {
        string line = DidLine.Of([Ok("FetchUrl", new string('a', 100) + "\nsecond line")]);

        Assert.Multiple(() =>
        {
            Assert.That(line, Is.EqualTo("FetchUrl: " + new string('a', 79) + "…"));
            Assert.That(line, Does.Not.Contain("second line"));
        });
    }

    /// <summary>A call that gave nothing back is its name alone.</summary>
    [Test]
    public void ACallWithNoResult_IsItsName() =>
        Assert.That(DidLine.Of([Ok("GetToday")]), Is.EqualTo("GetToday"));
}
