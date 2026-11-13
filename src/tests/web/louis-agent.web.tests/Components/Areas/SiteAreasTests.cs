using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Pages;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas;

/// <summary>
/// F127.6: a site names the areas it uses in one call, and each area registers what it needs: its services, its
/// frame and its pages. The chat plumbing is every site's, since a guide in a corner runs on it with no chat portal.
/// </summary>
public class SiteAreasTests
{
    /// <summary>One layout a site gives its admin pages, and another its account pages.</summary>
    private sealed class AdminLayout;

    /// <summary>The account pages' layout.</summary>
    private sealed class AccountLayout;

    /// <summary>A library's admin link, as the evals area gives one.</summary>
    private sealed class SomeLinks;

    /// <summary>A service only a library's area registers.</summary>
    private sealed class LibraryService;

    /// <summary>The services a site gets for the areas it names.</summary>
    private static ServiceCollection Named(Action<SiteAreasBuilder> build)
    {
        var services = new ServiceCollection();
        services.AddSiteAreas(build);
        return services;
    }

    /// <summary>Whether a service is registered.</summary>
    private static bool Has<T>(IServiceCollection services) => services.Any(d => d.ServiceType == typeof(T));

    /// <summary>The one instance registered for a service.</summary>
    private static T One<T>(IServiceCollection services) =>
        (T)services.Single(d => d.ServiceType == typeof(T)).ImplementationInstance!;

    /// <summary>
    /// With no area named, the chat plumbing is there, as the guide in a corner needs it, and a site's own words
    /// stay.
    /// </summary>
    [Test]
    public void WithNoArea_TheChatPlumbingIsThere_AndASitesOwnWordsStay()
    {
        var words = new ChatText { Suggested = "Voorgestel" };
        var services = new ServiceCollection();
        services.AddSingleton(words);

        services.AddSiteAreas(_ => { });

        Assert.Multiple(() =>
        {
            Assert.That(Has<ChatSettings>(services) && Has<ChatStore>(services) && Has<AgentApi>(services) &&
                        Has<CurrencyState>(services), Is.True);
            Assert.That(One<ChatText>(services), Is.SameAs(words));
            Assert.That(One<SiteAreas>(services).Names, Is.Empty);
            Assert.That(Has<SiteFrames>(services) || Has<ChatHandoff>(services), Is.False);
        });
    }

    /// <summary>Each area the framework's web names registers its services, its frame and its pages.</summary>
    [Test]
    public void EachArea_RegistersItsServicesItsFrameAndItsPages()
    {
        var services = Named(areas => areas.Chat().Admin(typeof(AdminLayout)).Showroom(new ShowroomDemos([])));

        var areas = One<SiteAreas>(services);
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "chat", "admin", "showroom" }));
            Assert.That(areas.PageAssemblies, Is.EqualTo(FrameworkPages.Assemblies));
            Assert.That(Has<ChatHandoff>(services) && Has<ShowroomDemos>(services), Is.True);
            Assert.That(Has<GuideDoor>(services) && Has<AgentWindows>(services) && Has<GuideKeeper>(services), Is.True);
            Assert.That(One<SiteFrames>(services),
                Is.EqualTo(new SiteFrames(typeof(AdminLayout), typeof(AdminLayout))));
        });
    }

    /// <summary>A library's area adds its own services, pages, admin links and account layout.</summary>
    [Test]
    public void ALibrarysArea_AddsItsServicesPagesLinksAndAccountLayout()
    {
        var services = Named(areas => areas.Admin(typeof(AdminLayout))
            .Add(new SiteArea("evals", s => s.AddSingleton<LibraryService>(), [typeof(SiteAreasTests).Assembly],
                AdminLinks: typeof(SomeLinks)))
            .Add(new SiteArea("accounts", _ => { }, [], AccountLayout: typeof(AccountLayout))));

        var areas = One<SiteAreas>(services);
        Assert.Multiple(() =>
        {
            Assert.That(areas.PageAssemblies, Does.Contain(typeof(SiteAreasTests).Assembly));
            Assert.That(areas.AdminLinks, Is.EqualTo(new[] { typeof(SomeLinks) }));
            Assert.That(Has<LibraryService>(services), Is.True);
            Assert.That(One<SiteFrames>(services),
                Is.EqualTo(new SiteFrames(typeof(AdminLayout), typeof(AccountLayout))));
        });
    }

    /// <summary>An area named twice counts once: the first one named stays, as .NET's TryAdd does.</summary>
    [Test]
    public void AnAreaNamedTwice_CountsOnce()
    {
        var services = Named(areas => areas.Admin(typeof(AdminLayout)).Admin(typeof(AccountLayout)).Chat().Chat());

        Assert.Multiple(() =>
        {
            Assert.That(One<SiteAreas>(services).Names, Is.EqualTo(new[] { "admin", "chat" }));
            Assert.That(One<SiteFrames>(services).Admin, Is.EqualTo(typeof(AdminLayout)));
            Assert.That(services.Count(d => d.ServiceType == typeof(ChatHandoff)), Is.EqualTo(1));
        });
    }
}
