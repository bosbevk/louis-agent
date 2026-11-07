using System.Net;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Components.TwoFactor;

namespace LouisAgent.Web.Accounts.Tests.Components.TwoFactor;

/// <summary>
/// The two-factor section over a scripted API: off, it shows the key and the app's link; the first code turns it
/// on and the recovery codes show once; on, it turns off again.
/// </summary>
public class TwoFactorSectionTests
{
    private const string Off =
        """{"sharedKey":"ABCDEFGHIJKLMNOP","recoveryCodesLeft":0,"recoveryCodes":null,"isTwoFactorEnabled":false,"isMachineRemembered":false}""";

    private const string On =
        """{"sharedKey":"ABCDEFGHIJKLMNOP","recoveryCodesLeft":10,"recoveryCodes":["aaaaa-bbbbb","ccccc-ddddd"],"isTwoFactorEnabled":true,"isMachineRemembered":false}""";

    private const string OnQuiet =
        """{"sharedKey":"ABCDEFGHIJKLMNOP","recoveryCodesLeft":7,"recoveryCodes":null,"isTwoFactorEnabled":true,"isMachineRemembered":false}""";

    /// <summary>The API as scripted: a body that enables answers on with the codes; otherwise the state given.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request, string state)
    {
        string body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
        return body.Contains("\"enable\":true", StringComparison.Ordinal)
            ? AccountTestContext.Reply(HttpStatusCode.OK, On)
            : AccountTestContext.Reply(HttpStatusCode.OK, state);
    }

    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", "Ann", true, [], true);

    private static IRenderedComponent<TwoFactorSection> Render(BunitContext ctx) =>
        ctx.Render<TwoFactorSection>(p => p.Add(s => s.Me, Ann));

    [Test]
    public void Off_ShowsTheKey_AndTheLinkForTheApp()
    {
        var (ctx, _) = AccountTestContext.New(request => Answer(request, Off));
        using var __ = ctx;

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".account-key").TextContent, Is.EqualTo("abcd efgh ijkl mnop"));
            Assert.That(cut.Find("a").GetAttribute("href"),
                Is.EqualTo("otpauth://totp/localhost:ann%40x.test?secret=ABCDEFGHIJKLMNOP&issuer=localhost&digits=6"));
            Assert.That(cut.Find("button").TextContent, Is.EqualTo("Turn on"));
        }));
    }

    [Test]
    public void TheFirstCode_TurnsItOn_AndShowsTheRecoveryCodesOnce()
    {
        var (ctx, handler) = AccountTestContext.New(request => Answer(request, Off));
        using var _ = ctx;
        var cut = Render(ctx);
        cut.WaitForElement("form");

        cut.Find("input[type=text]").Change("123456");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("is on"));
            Assert.That(cut.FindAll(".account-codes li").Select(li => li.TextContent),
                Is.EqualTo(new[] { "aaaaa-bbbbb", "ccccc-ddddd" }));
            Assert.That(cut.Find("p").TextContent, Is.Not.Empty);
        }));
        Assert.That(handler.Requests.Last().Body,
            Does.Contain("\"enable\":true").And.Contain("\"twoFactorCode\":\"123456\""));
    }

    [Test]
    public void On_SaysHowManyCodesAreLeft_AndTurnsOff()
    {
        var (ctx, handler) = AccountTestContext.New(request => Answer(request, OnQuiet));
        using var _ = ctx;
        var cut = Render(ctx);
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("7 recovery codes left.")));

        cut.FindAll("button").Single(b => b.TextContent == "Turn off").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests.Last().Body, Does.Contain("\"enable\":false")));
        Assert.That(cut.FindAll(".account-codes"), Is.Empty, "no codes without a call that made them");
    }
}
