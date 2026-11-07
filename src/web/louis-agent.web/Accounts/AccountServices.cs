using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Web.Accounts;

/// <summary>
/// What a site's program registers for accounts: the client, the state, Blazor's view of it, and the pages' words in
/// English unless the site registered its own first.
/// </summary>
public static class AccountServices
{
    /// <summary>One call in a site's <c>Program.cs</c>, after its <c>HttpClient</c> and its own <see cref="AccountText"/>.</summary>
    public static IServiceCollection AddLouisAgentAccounts(this IServiceCollection services)
    {
        services.AddScoped<AccountApi>();
        services.AddScoped<AccountState>();
        services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
        services.AddAuthorizationCore();
        services.TryAddSingleton(new AccountText());
        return services;
    }
}
