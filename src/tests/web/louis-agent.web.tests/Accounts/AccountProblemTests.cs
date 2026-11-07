using LouisAgent.Web.Accounts;

namespace LouisAgent.Web.Tests.Accounts;

/// <summary>A problem document reads as its code and its messages; anything else reads as nothing.</summary>
public class AccountProblemTests
{
    [Test]
    public void ARefusedSignIn_HasItsCode()
    {
        var problem =
            AccountProblem.Read(
                """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"detail":"LockedOut"}""");

        Assert.That(problem, Is.EqualTo(new AccountProblem("LockedOut", [])));
    }

    [Test]
    public void ABodyThatDidNotPass_HasEveryMessage_InOrder()
    {
        var problem = AccountProblem.Read(
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"PasswordTooShort":["Passwords must be at least 6 characters."],"PasswordRequiresDigit":["Passwords must have at least one digit ('0'-'9')."]}}""");

        Assert.That(problem.Errors,
            Is.EqualTo(new[]
            {
                "Passwords must be at least 6 characters.", "Passwords must have at least one digit ('0'-'9')."
            }));
        Assert.That(problem.Detail, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[1,2]")]
    [TestCase("""{"detail":5,"errors":"x"}""")]
    public void AnythingElse_ReadsAsNothing(string? body) =>
        Assert.That(AccountProblem.Read(body), Is.EqualTo(AccountProblem.None));
}
