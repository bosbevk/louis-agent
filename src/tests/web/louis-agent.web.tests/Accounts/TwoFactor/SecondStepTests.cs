using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.TwoFactor;

namespace LouisAgent.Web.Tests.Accounts.TwoFactor;

/// <summary>One box, two kinds of code: the digits go as the app's code, a dashed code as a recovery code.</summary>
public class SecondStepTests
{
    [Test]
    public void SixDigits_GoAsTheAppsCode() =>
        Assert.That(SecondStep.Of("ann@x.test", "pw", " 123456 "),
            Is.EqualTo(new LoginBody("ann@x.test", "pw", TwoFactorCode: "123456")));

    [Test]
    public void ADashedCode_GoesAsARecoveryCode() =>
        Assert.That(SecondStep.Of("ann@x.test", "pw", "QW3RT-7YUIO"),
            Is.EqualTo(new LoginBody("ann@x.test", "pw", TwoFactorRecoveryCode: "QW3RT-7YUIO")));
}
