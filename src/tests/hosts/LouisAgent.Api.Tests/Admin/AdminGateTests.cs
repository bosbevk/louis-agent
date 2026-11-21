
using LouisAgent.Api.Admin;

namespace LouisAgent.Api.Tests.Admin;

// F27 step 1 (F27-S1): the admin endpoints are closed without a key and never open by accident.
public class AdminGateTests
{
    [Test]
    public void TheAdminKey_FallsBackToTheAgentKey_AndClosesWithNeither()
    {
        Assert.That(AdminGate.Key(adminKey: "admin-1", agentKey: "agent-1"), Is.EqualTo("admin-1"));
        Assert.That(AdminGate.Key(adminKey: " ", agentKey: "agent-1"), Is.EqualTo("agent-1"));
        Assert.That(AdminGate.Key(adminKey: null, agentKey: null), Is.Null, "closed");
    }

    [TestCase("admin-1", "admin-1", AdminAccess.Allowed)]
    [TestCase("admin-1", "wrong", AdminAccess.Unauthorized)]
    [TestCase("admin-1", "", AdminAccess.Unauthorized)]
    [TestCase(null, "anything", AdminAccess.Closed)]
    public void Access_IsDecidedByTheKeyPresented(string? configured, string presented, AdminAccess expected)
    {
        Assert.That(AdminGate.Check(configured, presented), Is.EqualTo(expected));
    }
}
