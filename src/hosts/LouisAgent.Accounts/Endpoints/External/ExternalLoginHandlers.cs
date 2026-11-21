using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Accounts.External;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.Endpoints.External;

/// <summary>
/// Signing in through someone else (F61.3): the challenge sends the browser to the provider, the callback reads what
/// the provider said from Identity's external cookie and signs the person in, making the account in the current
/// tenant if it's their first time, or links the provider to the signed-in account when asked to.
/// </summary>
/// <remarks>
/// Named static methods on Identity's <c>SignInManager</c>, the way Identity's own scaffolded pages do it (ADR
/// 0051). A browser lands here from a provider, so every answer is a redirect, never JSON: to the page it came from
/// on a yes, to the sign-in page with <c>?failed=external</c> on a no.
/// </remarks>
public static class ExternalLoginHandlers
{
    /// <summary>The route the provider comes back to, through Identity's external cookie.</summary>
    public const string CallbackPath = "/account/external/callback";

    /// <summary>Where a provider that said no, or nothing, lands: the sign-in page says it didn't work.</summary>
    private static string Failed => $"/{AccountPages.Login}?failed=external";

    /// <summary>
    /// Sends the browser to the provider. With <c>link=true</c> a signed-in person is linking the provider to their
    /// account, and the callback checks it's still them.
    /// </summary>
    public static Results<ChallengeHttpResult, NotFound, UnauthorizedHttpResult> Challenge(string provider,
        ClaimsPrincipal principal, SignInManager<AccountUser> signIn, UserManager<AccountUser> users,
        IReadOnlyList<IExternalProvider> providers, string? returnUrl = null, bool link = false)
    {
        if (!providers.Any(p => p.IsConfigured && p.Name.Wire() == provider)) return TypedResults.NotFound();
        string? userId = link ? users.GetUserId(principal) : null;
        if (link && userId is null) return TypedResults.Unauthorized();

        string back = $"{CallbackPath}?returnUrl={Uri.EscapeDataString(LocalPath.Of(returnUrl))}&link={link}";
        var properties = signIn.ConfigureExternalAuthenticationProperties(provider, back, userId);
        return TypedResults.Challenge(properties, [provider]);
    }

    /// <summary>What the provider said, turned into a sign-in, a new account, or a linked login.</summary>
    public static async Task<RedirectHttpResult> Callback(ClaimsPrincipal principal,
        SignInManager<AccountUser> signIn, UserManager<AccountUser> users, CurrentTenant tenant,
        TimeProvider time, string? returnUrl = null, bool link = false)
    {
        string? userId = link ? users.GetUserId(principal) : null;
        var info = await signIn.GetExternalLoginInfoAsync(userId);
        if (info is null) return TypedResults.Redirect(Failed);

        if (link) return await LinkAsync(info, principal, signIn, users);

        // Someone who signed in this way before: straight in.
        var known = await signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true,
            bypassTwoFactor: true);
        if (known.Succeeded) return await Done(returnUrl, signIn);
        if (known.IsLockedOut) return TypedResults.Redirect(Failed);

        // First time: the provider's email finds an existing account in this tenant, or makes one, confirmed by the
        // provider having sent the person here.
        string? email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return TypedResults.Redirect(Failed);
        var user = await users.FindByEmailAsync(email) ?? await MakeAsync(info, email, users, tenant, time);
        if (user is null) return TypedResults.Redirect(Failed);

        var linked = await users.AddLoginAsync(user, info);
        if (!linked.Succeeded) return TypedResults.Redirect(Failed);
        await signIn.SignInAsync(user, isPersistent: true);
        return await Done(returnUrl, signIn);
    }

    /// <summary>Takes a provider off the account; refused when it's the last way in and there's no password.</summary>
    public static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ValidationProblem>> Unlink(
        string provider, ClaimsPrincipal principal, UserManager<AccountUser> users, SignInManager<AccountUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();
        var logins = await users.GetLoginsAsync(user);
        var login = logins.FirstOrDefault(l => l.LoginProvider == provider);
        if (login is null) return TypedResults.NotFound();
        if (logins.Count == 1 && !await users.HasPasswordAsync(user))
            return AccountProblems.Of(nameof(provider), Messages.AccountUserText.LastWayIn);

        await users.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        await signIn.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    /// <summary>A password for an account that came in through a provider and has none.</summary>
    public static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> SetPassword(
        ClaimsPrincipal principal, SetPasswordBody body, UserManager<AccountUser> users,
        SignInManager<AccountUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();
        if (await users.HasPasswordAsync(user))
            return AccountProblems.Of(nameof(body.NewPassword), Messages.AccountUserText.HasPasswordAlready);

        var set = await users.AddPasswordAsync(user, body.NewPassword);
        if (!set.Succeeded) return AccountProblems.Of(set);
        await signIn.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    /// <summary>Attaches the provider to the signed-in account, then back to the profile.</summary>
    private static async Task<RedirectHttpResult> LinkAsync(ExternalLoginInfo info, ClaimsPrincipal principal,
        SignInManager<AccountUser> signIn, UserManager<AccountUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Redirect(Failed);
        var linked = await users.AddLoginAsync(user, info);
        await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
        if (!linked.Succeeded) return TypedResults.Redirect($"/{AccountPages.Profile}?failed=link");
        await signIn.RefreshSignInAsync(user);
        return TypedResults.Redirect($"/{AccountPages.Profile}");
    }

    /// <summary>A new account from what the provider said: in this tenant, confirmed, named as the provider names them.</summary>
    private static async Task<AccountUser?> MakeAsync(ExternalLoginInfo info, string email,
        UserManager<AccountUser> users, CurrentTenant tenant, TimeProvider time)
    {
        var user = AccountUser.New(tenant.Id, email, time.GetUtcNow());
        user.EmailConfirmed = true;
        user.DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name);
        var made = await users.CreateAsync(user);
        return made.Succeeded ? user : null;
    }

    /// <summary>The external cookie has done its work; back to where the person was.</summary>
    private static async Task<RedirectHttpResult> Done(string? returnUrl, SignInManager<AccountUser> signIn)
    {
        await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
        return TypedResults.Redirect(LocalPath.Of(returnUrl));
    }
}
