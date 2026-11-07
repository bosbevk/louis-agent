using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Info = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Accounts.Contracts.AccountInfo>,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>;
using Changed = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Accounts.Contracts.AccountInfo>,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.ValidationProblem>;
using Gone = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>;

namespace LouisAgent.Accounts.Endpoints;

/// <summary>
/// What Identity's own routes don't do: who am I, change my name, sign me out, delete me, and which providers this
/// instance signs in through. Each answers a record from the contracts, so the browser app reads what's written.
/// </summary>
/// <remarks>
/// One handler class for the group's routes, each a named method returning <c>TypedResults</c> (ADR 0045, 0051),
/// with the checks first as guard clauses and the happy path last. The methods are static and take Identity's
/// managers as parameters, which the routing fills in per request, so a route points at a method by name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class AccountHandlers
{
    /// <summary>The longest name a person may go by, as the column allows.</summary>
    public const int MaxDisplayName = 256;

    /// <summary>Who the cookie says, with what the profile page shows.</summary>
    public static async Task<Info> Me(ClaimsPrincipal principal, UserManager<AccountUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        return TypedResults.Json(await InfoOf(user, users), AccountJson.Web);
    }

    /// <summary>Changes the name the person goes by; blank clears it.</summary>
    public static async Task<Changed> UpdateProfile(ClaimsPrincipal principal, ProfileBody body,
        UserManager<AccountUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        string? name = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim();
        if (name?.Length > MaxDisplayName)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(ProfileBody.DisplayName)] =
                    [MessageText.Format(AccountUserText.DisplayNameTooLong, new { Max = MaxDisplayName })]
            });

        user.DisplayName = name;
        await users.UpdateAsync(user);
        return TypedResults.Json(await InfoOf(user, users), AccountJson.Web);
    }

    /// <summary>Ends the sign-in: the cookie is cleared.</summary>
    public static async Task<NoContent> Logout(SignInManager<AccountUser> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    /// <summary>Deletes the person's own account and signs them out (POPIA: their data goes when they say so).</summary>
    public static async Task<Gone> DeleteAccount(ClaimsPrincipal principal, UserManager<AccountUser> users,
        SignInManager<AccountUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        await signIn.SignOutAsync();
        await users.DeleteAsync(user);
        return TypedResults.NoContent();
    }

    /// <summary>The providers a button may show: none until F61.3 brings them.</summary>
    public static JsonHttpResult<ProvidersReply> Providers() =>
        TypedResults.Json(new ProvidersReply([]), AccountJson.Web);

    /// <summary>The account as the browser sees it.</summary>
    private static async Task<AccountInfo> InfoOf(AccountUser user, UserManager<AccountUser> users) => new(
        user.Id,
        user.TenantId,
        user.Email ?? "",
        user.DisplayName,
        user.EmailConfirmed,
        (await users.GetLoginsAsync(user)).Select(login => login.LoginProvider).ToList(),
        await users.HasPasswordAsync(user));
}
