using LouisAgent.Accounts.Endpoints.External;
using LouisAgent.Accounts.Endpoints.Passkeys;

namespace LouisAgent.Accounts.Endpoints;

/// <summary>The routes of ours beside Identity's, on the <c>/account</c> group.</summary>
public static class AccountRoutes
{
    /// <summary>Maps our routes: the ones about a person need the cookie, the providers' list is open.</summary>
    public static IEndpointRouteBuilder MapAccountRoutes(this IEndpointRouteBuilder account)
    {
        account.MapGet("/me", AccountHandlers.Me).RequireAuthorization();
        account.MapPut("/profile", AccountHandlers.UpdateProfile).RequireAuthorization();
        account.MapPost("/logout", AccountHandlers.Logout).RequireAuthorization();
        account.MapDelete("", AccountHandlers.DeleteAccount).RequireAuthorization();
        account.MapGet("/providers", AccountHandlers.Providers);

        // Through a provider (F61.3): the challenge and the callback are open, since a browser arrives from the
        // provider with no cookie of ours yet; unlinking and a first password are the person's own.
        account.MapGet("/external/{provider}", ExternalLoginHandlers.Challenge);
        account.MapGet("/external/callback", ExternalLoginHandlers.Callback);
        account.MapDelete("/external/{provider}", ExternalLoginHandlers.Unlink).RequireAuthorization();
        account.MapPost("/password", ExternalLoginHandlers.SetPassword).RequireAuthorization();

        // Passkeys (F61.5): making and keeping one is the person's own; the options to sign in, and the sign-in, are
        // open, since the person has no cookie of ours yet.
        var passkeys = account.MapGroup("/passkeys");
        passkeys.MapPost("/options", PasskeyHandlers.CreationOptions).RequireAuthorization();
        passkeys.MapPost("", PasskeyHandlers.Add).RequireAuthorization();
        passkeys.MapGet("", PasskeyHandlers.List).RequireAuthorization();
        passkeys.MapDelete("/{id}", PasskeyHandlers.Remove).RequireAuthorization();
        passkeys.MapPost("/request-options", PasskeyHandlers.RequestOptions);
        passkeys.MapPost("/login", PasskeyHandlers.Login);
        return account;
    }
}
