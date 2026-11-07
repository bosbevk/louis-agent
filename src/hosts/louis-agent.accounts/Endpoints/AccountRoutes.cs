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
        return account;
    }
}
