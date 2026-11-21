using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Frames;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests;

/// <summary>
/// F127.6: the accounts are an area a site names with <c>.Accounts(layout)</c>: their services, their pages for the
/// router, and the layout the account pages draw inside.
/// </summary>
public class AccountAreaTests
{
    /// <summary>The layout a site gives its account pages.</summary>
    private sealed class AccountLayout;

    /// <summary>Named, the accounts register their services, their pages and their frame.</summary>
    [Test]
    public void TheAccounts_AreAnArea_WithTheirServicesPagesAndFrame()
    {
        var services = new ServiceCollection();

        services.AddSiteAreas(areas => areas.Accounts(typeof(AccountLayout)));

        var areas = (SiteAreas)services.Single(d => d.ServiceType == typeof(SiteAreas)).ImplementationInstance!;
        var frames = (SiteFrames)services.Single(d => d.ServiceType == typeof(SiteFrames)).ImplementationInstance!;
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "accounts" }));
            Assert.That(areas.PageAssemblies, Does.Contain(AccountLibrary.Assembly));
            Assert.That(frames.Account, Is.EqualTo(typeof(AccountLayout)));
            Assert.That(services.Any(d => d.ServiceType == typeof(AccountApi)), Is.True);
        });
    }
}
