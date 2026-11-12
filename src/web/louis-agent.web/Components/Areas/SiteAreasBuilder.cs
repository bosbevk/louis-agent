using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Web.Components.Areas;

/// <summary>
/// Collects the areas a site names (F127.6): the chat portal, the admin portal and the showroom are the framework's
/// web's own, and a library above it adds its own area through <see cref="Add"/>. An area named twice counts once.
/// </summary>
/// <remarks>
/// Builder (Gang of Four), open from outside as .NET's own setup calls are: the eval studio's and the accounts' areas
/// are extension methods in their own libraries, since the framework's web can't name them (dependencies point one
/// way). Of three (a record of flags each part looks up; each site keeping its wiring by hand, with a test listing it),
/// the one where a new site is a few lines that can't forget a part. A name twice counts once, as <c>TryAdd</c> does,
/// so nothing is thrown (Claude's pick, of three, after Louis's "let's replan this").
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/builder">Builder (Refactoring.Guru)</seealso>
public sealed class SiteAreasBuilder
{
    private readonly List<SiteArea> _areas = [];
    private Type? _adminLayout;

    /// <summary>The chat portal: the address a screen hands a draft to, its chat page's.</summary>
    public SiteAreasBuilder Chat(string handoff = "chat") =>
        Add(new SiteArea("chat", services => services.TryAddSingleton(new ChatHandoff(handoff)), []));

    /// <summary>
    /// The admin portal: the packs' screens and uploads, drawn inside <paramref name="layout"/>, each pack's own view
    /// set by <paramref name="views"/>.
    /// </summary>
    public SiteAreasBuilder Admin(Type layout, Action<PackViews>? views = null)
    {
        if (Named("admin")) return this;
        _adminLayout = layout;
        var packViews = new PackViews();
        views?.Invoke(packViews);
        return Add(new SiteArea("admin", services => services.TryAddSingleton(packViews), FrameworkPages.Assemblies));
    }

    /// <summary>
    /// The showroom (ADR 0115): the site's demos, and the guide's way in, the demos' windows and the guide kept across
    /// the site's layouts.
    /// </summary>
    public SiteAreasBuilder Showroom(ShowroomDemos demos) =>
        Add(new SiteArea("showroom", services =>
        {
            services.TryAddSingleton(demos);
            services.TryAddSingleton<GuideDoor>();
            services.TryAddSingleton<AgentWindows>();
            services.TryAddSingleton<GuideKeeper>();
        }, []));

    /// <summary>A library's area, by its name; one named before stays.</summary>
    public SiteAreasBuilder Add(SiteArea area)
    {
        if (!Named(area.Name)) _areas.Add(area);
        return this;
    }

    /// <summary>
    /// Registers each area's services, the site's frames when it has admin or account pages, and the areas once.
    /// </summary>
    internal void Register(IServiceCollection services)
    {
        foreach (var area in _areas) area.Services(services);

        // Each frame falls back to the other: a site with only one of the two still draws both kinds of page.
        var accountLayout = _areas.Select(area => area.AccountLayout).OfType<Type>().FirstOrDefault();
        if ((_adminLayout ?? accountLayout) is { } admin)
            services.TryAddSingleton(new SiteFrames(admin, accountLayout ?? admin));
        services.TryAddSingleton(new SiteAreas(_areas));
    }

    /// <summary>Whether an area by this name is named already.</summary>
    private bool Named(string name) =>
        _areas.Any(area => string.Equals(area.Name, name, StringComparison.OrdinalIgnoreCase));
}
