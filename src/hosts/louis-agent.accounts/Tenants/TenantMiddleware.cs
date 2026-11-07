namespace LouisAgent.Accounts.Tenants;

/// <summary>Fills in the request's <see cref="CurrentTenant"/> from its host, before any account route runs.</summary>
public static class TenantMiddleware
{
    /// <summary>Resolves the tenant for every request; put it before authentication, so the store sees it.</summary>
    public static IApplicationBuilder UseTenant(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var resolver = context.RequestServices.GetRequiredService<ITenantResolver>();
            var current = context.RequestServices.GetRequiredService<CurrentTenant>();
            current.Set(await resolver.ResolveAsync(context.Request.Host.Value, context.RequestAborted));
            await next(context);
        });
}
