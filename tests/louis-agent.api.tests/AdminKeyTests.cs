using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Api.Tests;

// F31 review of PR #55: saving the admin key in the portal overwrote Puls's chat key, so the chat got 401s whenever
// ADMIN_API_KEY differs from AGENT_API_KEY. The browser keeps two keys, and each request carries the one its path needs.
public class AdminKeyTests
{
    private static Settings Both() => new() { ApiKey = "chat-key", AdminKey = "admin-key" };

    [Test]
    public void TheChat_KeepsItsOwnKey_WhenAnAdminKeyIsSaved()
    {
        Assert.That(Both().KeyFor(new Uri("sessions", UriKind.Relative)), Is.EqualTo("chat-key"));
        Assert.That(Both().KeyFor(new Uri("http://127.0.0.1:5070/sessions/abc/messages")), Is.EqualTo("chat-key"));
    }

    [Test]
    public void TheAdminPortal_SendsTheAdminKey()
    {
        Assert.That(Both().KeyFor(new Uri("admin/benchmarks", UriKind.Relative)), Is.EqualTo("admin-key"));
        Assert.That(Both().KeyFor(new Uri("/admin/benchmarks/runs/x", UriKind.Relative)), Is.EqualTo("admin-key"));
        Assert.That(Both().KeyFor(new Uri("http://127.0.0.1:5070/admin/benchmarks")), Is.EqualTo("admin-key"));
    }

    [Test]
    public void WithoutAnAdminKey_ThePortalSendsTheChatKey()
    {
        // The server's admin key falls back to AGENT_API_KEY (AdminGate.Key), so the chat's key opens it then.
        var chatOnly = new Settings { ApiKey = "chat-key" };
        Assert.That(chatOnly.KeyFor(new Uri("admin/benchmarks", UriKind.Relative)), Is.EqualTo("chat-key"));
    }

    [Test]
    public void APathThatOnlyStartsLikeAdmin_IsNotTheAdminPortal() =>
        Assert.That(Both().KeyFor(new Uri("administer", UriKind.Relative)), Is.EqualTo("chat-key"));
}
