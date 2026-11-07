using System.Net;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Components.Account.Profile;

namespace LouisAgent.Web.Tests.Components.Account.Profile;

/// <summary>An account without a password gets a section to set one; one with a password sees nothing.</summary>
public class SetPasswordTests
{
    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", "Ann", true, ["google"], false);

    [Test]
    public void AnAccountWithAPassword_SeesNothing()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.NoContent);
        using var __ = ctx;

        var cut = ctx.Render<SetPassword>(p => p.Add(s => s.Me, Ann with { HasPassword = true }));

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void AFirstPassword_IsPosted_Once_TheTwoAgree()
    {
        var (ctx, handler) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.NoContent));
        using var _ = ctx;
        var cut = ctx.Render<SetPassword>(p => p.Add(s => s.Me, Ann));

        foreach (var field in cut.FindAll("input[type=password]")) field.Input("Long-enough-1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("is set")));
        Assert.That(handler.Requests.First().Body, Is.EqualTo("""{"newPassword":"Long-enough-1"}"""));
    }
}
