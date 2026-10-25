using LouisAgent.Api.Sessions.Requests;
using System.Text.Json;

namespace LouisAgent.Api.Tests.Sessions.Requests;

// CL14 step 1, CL20 step 5: POST /v1/sessions takes "role". "personality", its name before CL14, was read for a client one deploy
// behind; that client calls the unversioned routes, which are gone, so it is not read any more.
public class CreateSessionRequestTests
{
    private static CreateSessionRequest Request(string? role = null) =>
        new(null, null, null, Role: role);

    [Test]
    public void TheRole_IsTheNewWord() =>
        Assert.That(Request(role: "puls-cook").RoleOrNull(), Is.EqualTo("puls-cook"));

    [Test]
    public void APersonality_IsNotRead() =>
        Assert.That(JsonSerializer.Deserialize<CreateSessionRequest>("""{"personality":"puls-cook"}""", JsonSerializerOptions.Web)!.RoleOrNull(),
            Is.Null);

    [Test]
    public void Neither_IsNoRole_TheDefault() =>
        Assert.That(Request().RoleOrNull(), Is.Null);
}
