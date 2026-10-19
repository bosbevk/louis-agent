using LouisAgent.Web.Shared.Chat;
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
        Assert.That(Card("""{"toolsets":["web"],"message":"Wat kook ek?","role":"puls-cook"}""")!.Role, Is.EqualTo("puls-cook"));

    [Test]
    public void ACallRecordedBeforeCL14_ShowsItsPersonality() =>
        Assert.That(Card("""{"toolsets":["web"],"message":"Wat kook ek?","personality":"puls-cook"}""")!.Role, Is.EqualTo("puls-cook"));
}
