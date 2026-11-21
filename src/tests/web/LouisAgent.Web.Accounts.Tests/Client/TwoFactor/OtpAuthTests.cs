using LouisAgent.Web.Accounts.Client.TwoFactor;

namespace LouisAgent.Web.Accounts.Tests.Client.TwoFactor;

/// <summary>The link an authenticator app opens, and the key as a person reads it.</summary>
public class OtpAuthTests
{
    [Test]
    public void TheLink_NamesTheSiteAndTheAccount_AndCarriesTheKey() =>
        Assert.That(OtpAuth.Link("mypuls.co.za", "ann@x.test", "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"),
            Is.EqualTo(
                "otpauth://totp/mypuls.co.za:ann%40x.test?secret=ABCDEFGHIJKLMNOPQRSTUVWXYZ234567&issuer=mypuls.co.za&digits=6"));

    [Test]
    public void TheKey_ReadsInLowercaseGroupsOfFour() =>
        Assert.That(OtpAuth.Readable("ABCDEFGHIJKLMNOPQR"), Is.EqualTo("abcd efgh ijkl mnop qr"));
}
