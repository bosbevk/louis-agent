using LouisAgent.Accounts.Tenants;

namespace LouisAgent.Accounts.Tests.Tenants;

/// <summary>A host name reads the same whatever case, port or www. it came with.</summary>
public class TenantHostTests
{
    [TestCase("acme.example", "acme.example")]
    [TestCase("ACME.Example", "acme.example")]
    [TestCase("acme.example:8080", "acme.example")]
    [TestCase("www.acme.example", "acme.example")]
    [TestCase("WWW.ACME.EXAMPLE:443", "acme.example")]
    [TestCase(" localhost:5071 ", "localhost")]
    [TestCase("[::1]:5071", "[::1]")]
    [TestCase("[::1]", "[::1]")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Normalize_GivesTheOneFormATenantRowNames(string? host, string expected) =>
        Assert.That(TenantHost.Normalize(host), Is.EqualTo(expected));

    /// <summary>Only a leading www. goes; a host that merely contains it stays whole.</summary>
    [Test]
    public void Normalize_LeavesWwwInsideAName()
    {
        Assert.That(TenantHost.Normalize("mywww.example"), Is.EqualTo("mywww.example"));
    }
}
