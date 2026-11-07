using Microsoft.AspNetCore.Authentication.Cookies;

namespace LouisAgent.Accounts.Config;

/// <summary>
/// How the sign-in cookie behaves: a script can't read it, a cross-site form doesn't send it, and a call without it
/// gets a 401, not a redirect to a login page, since the API answers JSON and the browser app draws the page itself.
/// </summary>
public static class AccountCookies
{
    /// <summary>The rules on Identity's application cookie.</summary>
    public static void Configure(CookieAuthenticationOptions cookie, AccountsOptions options)
    {
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        // Secure where the request is, so the PC's http://127.0.0.1 works and a hosted https site is never without it.
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        cookie.Cookie.Domain = options.CookieDomain;
        cookie.SlidingExpiration = true;
        cookie.ExpireTimeSpan = TimeSpan.FromDays(14);

        // An API answers with a status, never with a page.
        cookie.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        cookie.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }
}
