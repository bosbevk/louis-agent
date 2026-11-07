using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts;

/// <summary>What a site's program registers for accounts: the client, the state and Blazor's view of it.</summary>
public static class AccountServices
{
    /// <summary>One call in a site's <c>Program.cs</c>, after its <c>HttpClient</c>.</summary>
    public static IServiceCollection AddLouisAgentAccounts(this IServiceCollection services)
    {
        services.AddScoped<AccountApi>();
        services.AddScoped<AccountState>();
        services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
        services.AddAuthorizationCore();
        return services;
    }
}
