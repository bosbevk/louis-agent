using LouisAgent.Web.Accounts.Client.Navigation;

namespace LouisAgent.Web.Accounts.Tests.Client.Navigation;

/// <summary>After a sign-in the person goes to one of our pages, never to an address another site put in the query.</summary>
public class SafeReturnTests
{
    [TestCase("chat", "chat")]
    [TestCase("/chat", "chat")]
    [TestCase("gesels/sess_1?x=1", "gesels/sess_1?x=1")]
    [TestCase(" /profile ", "profile")]
    public void OneOfOurPages_IsFollowed(string given, string expected) =>
        Assert.That(SafeReturn.Of(given), Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("/")]
    [TestCase("//evil.test/x")]
    [TestCase("https://evil.test")]
    [TestCase("javascript:alert(1)")]
    [TestCase("\\\\evil.test")]
    public void AnythingElse_IsHome(string? given) => Assert.That(SafeReturn.Of(given), Is.EqualTo("./"));
}
