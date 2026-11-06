
using LouisAgent.Web.Storage;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// Saving the admin key in the portal once overwrote the chat's key, so the chat got 401s whenever the two differed.
/// The browser keeps two keys, and each request carries the one its path needs.
/// </summary>
public class AdminKeyTests
{
    /// <summary>Settings holding both keys.</summary>
    private static ChatSettings Both() => new() { ApiKey = "chat-key", AdminKey = "admin-key" };

    /// <summary>Saving an admin key leaves the chat's alone.</summary>
    [Test]
    public void TheChat_KeepsItsOwnKey_WhenAnAdminKeyIsSaved()
    {
        Assert.That(Both().KeyFor(new Uri("v1/sessions", UriKind.Relative)), Is.EqualTo("chat-key"));
        Assert.That(Both().KeyFor(new Uri("http://127.0.0.1:5070/v1/sessions/abc/messages")), Is.EqualTo("chat-key"));
    }

    /// <summary>The admin routes carry the admin key.</summary>
    [Test]
    public void TheAdminPortal_SendsTheAdminKey()
    {
        Assert.That(Both().KeyFor(new Uri("v1/admin/benchmarks", UriKind.Relative)), Is.EqualTo("admin-key"));
        Assert.That(Both().KeyFor(new Uri("/v1/admin/benchmarks/runs/x", UriKind.Relative)), Is.EqualTo("admin-key"));
        Assert.That(Both().KeyFor(new Uri("http://127.0.0.1:5070/v1/admin/benchmarks")), Is.EqualTo("admin-key"));
    }

    /// <summary>With no admin key, the portal falls back on the chat's.</summary>
    [Test]
    public void WithoutAnAdminKey_ThePortalSendsTheChatKey()
    {
        // The server's admin key falls back to AGENT_API_KEY (AdminGate.Key), so the chat's key opens it then.
        var chatOnly = new ChatSettings { ApiKey = "chat-key" };
        Assert.That(chatOnly.KeyFor(new Uri("v1/admin/benchmarks", UriKind.Relative)), Is.EqualTo("chat-key"));
    }

    /// <summary>A path that only starts like the admin's still carries the chat's key.</summary>
    [Test]
    public void APathThatOnlyStartsLikeAdmin_IsNotTheAdminPortal() =>
        Assert.That(Both().KeyFor(new Uri("v1/administer", UriKind.Relative)), Is.EqualTo("chat-key"));
}
