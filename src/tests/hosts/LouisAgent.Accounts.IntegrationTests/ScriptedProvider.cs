using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// A provider a test can script: a route the test host alone maps, which signs an external identity into Identity's
/// external cookie exactly as a provider's handler would (the id, the email, the name, the provider's name, and the
/// anti-forgery id when linking). The callback then runs unchanged, on the same cookie it reads in production.
/// </summary>
internal static class ScriptedProvider
{
    /// <summary>Maps <c>/test/external?provider=&amp;key=&amp;email=&amp;name=&amp;xsrf=</c>.</summary>
    public static void Map(WebApplication app) => app.MapGet("/test/external", SignInAsync);

    /// <summary>Writes the external cookie with what a provider would have said.</summary>
    private static async Task<IResult> SignInAsync(HttpContext context, string provider, string key, string? email,
        string? name, string? xsrf)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, key) };
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));
        if (name is not null) claims.Add(new Claim(ClaimTypes.Name, name));
        var properties = new AuthenticationProperties { Items = { ["LoginProvider"] = provider } };
        if (xsrf is not null) properties.Items["XsrfId"] = xsrf;

        await context.SignInAsync(IdentityConstants.ExternalScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, provider)), properties);
        return Results.NoContent();
    }
}
