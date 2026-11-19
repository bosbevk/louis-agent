using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Api.Sessions;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>Who opened a session comes from the cookie's claims, and only from a signed-in person's.</summary>
public class SessionOpenerTests
{
    private static ClaimsPrincipal Principal(bool authenticated, params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticated ? "cookie" : null));

    [Test]
    public void ASignedInPerson_IsTheOpener_WithTheirTenant()
    {
        var principal = Principal(true, new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim(AccountClaims.Tenant, "home"));

        Assert.That(SessionOpener.Of(principal), Is.EqualTo(new SessionOpener("u1", "home")));
    }

    [Test]
    public void NobodySignedIn_IsNoOpener()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SessionOpener.Of(null), Is.Null);
            Assert.That(SessionOpener.Of(Principal(false, new Claim(ClaimTypes.NameIdentifier, "u1"))), Is.Null);
            Assert.That(SessionOpener.Of(Principal(true)), Is.Null, "signed in with no id is nobody");
        });
    }

    /// <summary>A cookie from before the tenant claim existed names an empty tenant, never a wrong one.</summary>
    [Test]
    public void ACookieWithoutATenant_HasAnEmptyOne()
    {
        var principal = Principal(true, new Claim(ClaimTypes.NameIdentifier, "u1"));

        Assert.That(SessionOpener.Of(principal), Is.EqualTo(new SessionOpener("u1", "")));
    }
}
