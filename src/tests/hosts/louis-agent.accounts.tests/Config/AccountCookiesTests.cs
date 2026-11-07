using LouisAgent.Accounts.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace LouisAgent.Accounts.Tests.Config;

/// <summary>The sign-in cookie can't be read by a script or sent by a cross-site form, and a missing one is a 401.</summary>
public class AccountCookiesTests
{
    private static CookieAuthenticationOptions Configured(string? domain = null)
    {
        var cookie = new CookieAuthenticationOptions();
        AccountCookies.Configure(cookie, new AccountsOptions("Server=sql", "site", domain));
        return cookie;
    }

    [Test]
    public void TheCookie_IsHttpOnly_Lax_AndSecureWhereTheRequestIs()
    {
        var cookie = Configured(".example.test").Cookie;

        Assert.Multiple(() =>
        {
            Assert.That(cookie.HttpOnly, Is.True);
            Assert.That(cookie.SameSite, Is.EqualTo(SameSiteMode.Lax));
            Assert.That(cookie.SecurePolicy, Is.EqualTo(CookieSecurePolicy.SameAsRequest));
            Assert.That(cookie.Domain, Is.EqualTo(".example.test"));
        });
    }

    /// <summary>A call that needs a person and brings none gets a status, not a page to log in on.</summary>
    [Test]
    public async Task AMissingCookie_IsA401_NotARedirect()
    {
        var options = Configured();
        var context = new DefaultHttpContext();

        await options.Events.OnRedirectToLogin(new RedirectContext<CookieAuthenticationOptions>(context,
            new AuthenticationScheme("x", null, typeof(CookieAuthenticationHandler)), options,
            new AuthenticationProperties(), "/login"));

        Assert.That(context.Response.StatusCode, Is.EqualTo(401));
        Assert.That(context.Response.Headers.Location.ToString(), Is.Empty);
    }

    [Test]
    public async Task ARefusedCall_IsA403()
    {
        var options = Configured();
        var context = new DefaultHttpContext();

        await options.Events.OnRedirectToAccessDenied(new RedirectContext<CookieAuthenticationOptions>(context,
            new AuthenticationScheme("x", null, typeof(CookieAuthenticationHandler)), options,
            new AuthenticationProperties(), "/denied"));

        Assert.That(context.Response.StatusCode, Is.EqualTo(403));
    }
}
