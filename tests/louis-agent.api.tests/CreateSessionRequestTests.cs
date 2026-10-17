using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Api.Tests;

// CL14 step 1: POST /sessions takes "role"; "personality", the word before CL14, is still read for a client one deploy
// behind, until the TODO's line removes it. Expand, then contract: the API accepts both before any caller changes.
public class CreateSessionRequestTests
{
    private static CreateSessionRequest Request(string? role = null, string? personality = null) =>
        new(null, null, null, Role: role, Personality: personality);

    [Test]
    public void TheRole_IsTheNewWord() =>
        Assert.That(Request(role: "puls-cook").RoleOrAlias(), Is.EqualTo("puls-cook"));

    [Test]
    public void ThePersonality_IsStillRead_WhenNoRole() =>
        Assert.That(Request(personality: "puls-cook").RoleOrAlias(), Is.EqualTo("puls-cook"));

    [Test]
    public void BothTheSame_IsFine() =>
        Assert.That(Request("puls-cook", "puls-cook").RoleOrAlias(), Is.EqualTo("puls-cook"));

    [Test]
    public void BothDiffering_IsRefused_NamingTheTwo() =>
        Assert.That(() => Request("puls-cook", "puls/voice-af").RoleOrAlias(),
            Throws.TypeOf<RoleException>().With.Message.Contains("puls-cook").And.Message.Contains("puls/voice-af"));

    [Test]
    public void Neither_IsNoRole_TheDefault() =>
        Assert.That(Request().RoleOrAlias(), Is.Null);
}
