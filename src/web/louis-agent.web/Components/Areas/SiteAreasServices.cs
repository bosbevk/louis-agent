using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Web.Components.Areas;

/// <summary>
/// Where a site names its areas (F127.6), in its Program.cs: one call, each area registering what it needs, so a new
/// site is a few lines and can't forget a part.
/// </summary>
/// <remarks>
/// The chat's plumbing comes with every site, whatever it names: a guide in a corner runs on it with no chat portal
/// (louis-agent.dev's). It is added only where the site hasn't registered its own, so a site's own words stay.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class SiteAreasServices
{
    /// <summary>Registers the chat's plumbing and the areas the site names in <paramref name="build"/>.</summary>
    public static IServiceCollection AddSiteAreas(this IServiceCollection services, Action<SiteAreasBuilder> build)
    {
        services.TryAddSingleton<ChatSettings>();
        services.TryAddSingleton<ChatStore>();
        services.TryAddScoped<AgentApi>();
        services.TryAddScoped<CurrencyState>();
        services.TryAddSingleton(new ChatText());

        var areas = new SiteAreasBuilder();
        build(areas);
        areas.Register(services);
        return services;
    }
}
