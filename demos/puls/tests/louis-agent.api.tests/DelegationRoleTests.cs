using LouisAgent.Testing;
using LouisAgent.Web.Chats;

namespace LouisAgent.Api.Tests;

/// <summary>
/// A delegation card shows the role the model passed to StartSubSession; a chat recorded when calls said "personality"
/// still shows it.
/// </summary>
public class DelegationRoleTests
{
    /// <summary>The card for a call with this input.</summary>
    private static Delegation? Card(string input) =>
        Delegation.From(new Block { Kind = BlockKind.Tool, ToolName = Delegation.ToolName, ToolInput = input });

    /// <summary>A call shows its role.</summary>
    [Test]
    public void ACall_ShowsItsRole() =>
        Assert.That(
            Card(SampleRole.Fill("""{"toolsets":["web"],"message":"Wat kook ek?","role":"<RoleAlpha>"}"""))!.Role,
            Is.EqualTo(SampleRole.Alpha));

    /// <summary>An old call shows its "personality" as the role.</summary>
    [Test]
    public void ACallRecordedBeforeCL14_ShowsItsPersonality() =>
        Assert.That(
            Card(SampleRole.Fill("""{"toolsets":["web"],"message":"Wat kook ek?","personality":"<RoleAlpha>"}"""))!
                .Role, Is.EqualTo(SampleRole.Alpha));
}
