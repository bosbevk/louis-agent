using LouisAgent.Api.Sessions.Requests;
using LouisAgent.SessionStream.Wire.Requests;
using System.Text.Json;

namespace LouisAgent.Api.Tests.Sessions.Requests;

/// <summary>
/// <c>POST /v1/sessions</c> takes <c>role</c> (CL14 step 1, CL20 step 5). <c>personality</c>, its name before CL14,
/// was read for a client one deploy behind; that client called the unversioned routes, which are gone, so it isn't
/// read any more.
/// </summary>
public class CreateSessionRequestTests
{
    /// <summary>An opening that asks for this role, or for none.</summary>
    private static CreateSessionRequest Request(string? role = null) =>
        new(null, null, null, Role: role);

    /// <summary>The role you ask for is the one you get.</summary>
    [Test]
    public void TheRole_IsTheNewWord() =>
        Assert.That(Request(role: SampleRole.Alpha).RoleOrNull(), Is.EqualTo(SampleRole.Alpha));

    /// <summary>The old word is ignored, so it asks for no role.</summary>
    [Test]
    public void APersonality_IsNotRead() =>
        Assert.That(
            JsonSerializer.Deserialize<CreateSessionRequest>(SampleRole.Fill("""{"personality":"<RoleAlpha>"}"""),
                JsonSerializerOptions.Web)!.RoleOrNull(),
            Is.Null);

    /// <summary>Asking for nothing gets the default voice.</summary>
    [Test]
    public void Neither_IsNoRole_TheDefault() =>
        Assert.That(Request().RoleOrNull(), Is.Null);
}
