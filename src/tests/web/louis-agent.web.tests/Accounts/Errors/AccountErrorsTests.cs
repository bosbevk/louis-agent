using System.Net;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Errors;
using LouisAgent.Web.Text;

namespace LouisAgent.Web.Tests.Accounts.Errors;

/// <summary>Identity's codes become the site's words; a body that didn't pass lists its messages.</summary>
public class AccountErrorsTests
{
    private static readonly AccountText Text = new();

    private static AccountApiException Refused(string? detail, params string[] errors) =>
        new(HttpStatusCode.Unauthorized, new AccountProblem(detail, errors));

    [TestCase("Failed", "That email and password don't match.")]
    [TestCase("LockedOut", "Too many tries: this account is locked for a while.")]
    [TestCase("NotAllowed", "Confirm your email first: open the link we mailed you.")]
    [TestCase("RequiresTwoFactor", "Now the code from your authenticator app, or one of your recovery codes.")]
    public void ACode_IsSaidInTheSitesWords(string code, string expected) =>
        Assert.That(AccountErrors.Of(Refused(code), Text), Is.EqualTo(new AccountErrorView(expected, [])));

    [Test]
    public void TheMessagesOfABodyThatDidNotPass_AreListed()
    {
        var view = AccountErrors.Of(Refused(null, "Too short.", "No digit."), Text);

        Assert.Multiple(() =>
        {
            Assert.That(view.Message, Is.Null);
            Assert.That(view.Details, Is.EqualTo(new[] { "Too short.", "No digit." }));
        });
    }

    [Test]
    public void AnythingElse_IsSomethingWentWrong()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AccountErrors.Of(Refused("Unknown"), Text).Message, Is.EqualTo(Text.SomethingWentWrong));
            Assert.That(AccountErrors.Of(new HttpRequestException("down"), Text).Message,
                Is.EqualTo(Text.SomethingWentWrong));
        });
    }
}
