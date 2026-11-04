using LouisAgent.Testing;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared;

namespace LouisAgent.Api.Tests;

// CL14 step 3: a delegation card shows the role the model passed, StartSubSession's "role"; a chat recorded before CL14
// (its calls said "personality") still shows it.
public class DelegationRoleTests
{
    private static Delegation? Card(string input) =>
        Delegation.From(new Block { Kind = BlockKind.Tool, ToolName = Delegation.ToolName, ToolInput = input });

    [Test]
    public void ACall_ShowsItsRole() =>
        Assert.That(
            Card(SampleRole.Fill("""{"toolsets":["web"],"message":"Wat kook ek?","role":"<RoleAlpha>"}"""))!.Role,
            Is.EqualTo(SampleRole.Alpha));

    [Test]
    public void ACallRecordedBeforeCL14_ShowsItsPersonality() =>
        Assert.That(
            Card(SampleRole.Fill("""{"toolsets":["web"],"message":"Wat kook ek?","personality":"<RoleAlpha>"}"""))!
                .Role, Is.EqualTo(SampleRole.Alpha));
}
