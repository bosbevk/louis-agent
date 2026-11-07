using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.Endpoints;
using LouisAgent.Accounts.Endpoints.Confirmation;
using LouisAgent.Accounts.Mail;
using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Accounts;

/// <summary>
/// The accounts as building blocks your program calls beside the API's: the services, the middleware that finds the
/// tenant and the person, and the <c>/account</c> routes. Without <c>ACCOUNTS_SQL_URL</c> every call is a quiet no:
/// the host runs with no accounts, as it did before.
/// </summary>
/// <remarks>
/// Extension Methods, as <c>ApiStartup</c>: a program reads as the app it is, and this library names no site. The
/// routes fail closed: they refuse to map until the middleware has been put in, so a route can't run without a tenant
/// or a person on the request.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class AccountsStartup
{
    /// <summary>Marks that the middleware is in, for the routes to see.</summary>
    private const string Used = "LouisAgent.Accounts.Used";

    /// <summary>Where every account route lives.</summary>
    public const string Prefix = "/account";

    /// <summary>
    /// Identity on the accounts database, inside the request's tenant, signing in with a cookie; nothing when the
    /// options say there's no database.
    /// </summary>
    public static WebApplicationBuilder AddLouisAgentAccounts(this WebApplicationBuilder builder,
        AccountsOptions options)
    {
        var services = builder.Services;
        services.AddSingleton(options);
        if (!options.Enabled) return builder;

        // The database, with a retry for a server that's waking up (Azure SQL's serverless tier pauses).
        services.AddDbContext<AccountsDbContext>(db =>
            db.UseSqlServer(options.SqlUrl, sql => sql.EnableRetryOnFailure()));

        // The request's tenant, and the rule that finds it.
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ITenantResolver>(sp =>
            new HostTenantResolver(sp.GetRequiredService<AccountsDbContext>(), options.DefaultTenant));

        // Identity: the cookie it signs in with, its managers over our tenant-aware store, and its JSON routes.
        services.AddAuthorization();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(cookie => AccountCookies.Configure(cookie, options));
        services.AddIdentityCore<AccountUser>(identity =>
            {
                identity.User.RequireUniqueEmail = true;
                identity.SignIn.RequireConfirmedEmail = true;
            })
            .AddUserStore<AccountUserStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        // The mail seam, after Identity's own no-op sender so this one is the one resolved; and the cookie's claims,
        // with the tenant on them.
        services.AddTransient<IEmailSender<AccountUser>, LogEmailSender>();
        services.AddScoped<IUserClaimsPrincipalFactory<AccountUser>, AccountClaimsFactory>();
        services.TryAddSingleton(TimeProvider.System);
        return builder;
    }

    /// <summary>
    /// Brings the database up to date, then puts in the middleware: the tenant from the host, then who the cookie
    /// says, then what they may do. Put it before the API's key check, which reads the person it finds.
    /// </summary>
    public static WebApplication UseLouisAgentAccounts(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AccountsOptions>();
        if (!options.Enabled)
        {
            app.Logger.Log(LogLevel.Warning, AccountLogMessage.NoAccounts, tag: "ACCOUNTS");
            return app;
        }

        // The migrations run here, before the app listens, so a request never meets a half-built schema.
        using (var scope = app.Services.CreateScope())
            AccountsMigrator.MigrateAsync(scope.ServiceProvider.GetRequiredService<AccountsDbContext>(),
                options.DefaultTenant, app.Logger).GetAwaiter().GetResult();

        app.UseTenant();
        app.UseAuthentication();
        app.UseAuthorization();
        ((IApplicationBuilder)app).Properties[Used] = true;
        return app;
    }

    /// <summary>
    /// Every account route under <c>/account</c>: Identity's own (register, login, confirm, forgot, reset, info) and
    /// ours. It refuses to map if your program never called <see cref="UseLouisAgentAccounts"/>.
    /// </summary>
    public static WebApplication MapLouisAgentAccounts(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AccountsOptions>();
        if (!options.Enabled) return app;

        // Fail closed: a route without the tenant middleware would land in no tenant at all.
        if (!((IApplicationBuilder)app).Properties.ContainsKey(Used))
            throw new InvalidOperationException(MessageText.Format(AccountErrorMessage.UseBeforeMap));

        var account = app.MapGroup(Prefix);
        account.MapIdentityApi<AccountUser>().WithConfirmationRedirect();
        account.MapAccountRoutes();
        return app;
    }
}
