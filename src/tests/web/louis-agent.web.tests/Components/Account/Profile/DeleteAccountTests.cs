using System.Net;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Components.Account.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Account.Profile;

/// <summary>Deleting takes the person's own email typed out; then the account goes and the state is cleared.</summary>
public class DeleteAccountTests
{
    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", "Ann", true, [], true);

    [Test]
    public void TheWrongEmail_KeepsTheButtonOff_AndCallsNothing()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.NoContent);
        using var _ = ctx;
        var cut = ctx.Render<DeleteAccount>(p => p.Add(d => d.Me, Ann));

        cut.Find("input").Change("bob@x.test");
        cut.Find("form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("button").HasAttribute("disabled"), Is.True);
            Assert.That(handler.Requests, Is.Empty);
        });
    }

    [Test]
    public void TheRightEmail_DeletesTheAccount_AndClearsTheState()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.NoContent);
        using var _ = ctx;
        var state = ctx.Services.GetRequiredService<AccountState>();
        state.Set(Ann);
        bool deleted = false;
        var cut = ctx.Render<DeleteAccount>(p => p.Add(d => d.Me, Ann).Add(d => d.OnDeleted, () => deleted = true));

        cut.Find("input").Change(" ANN@x.test ");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(deleted, Is.True));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Delete));
            Assert.That(handler.Requests.Single().Url, Is.EqualTo("http://localhost/account"));
            Assert.That(state.SignedIn, Is.False);
        });
    }
}
