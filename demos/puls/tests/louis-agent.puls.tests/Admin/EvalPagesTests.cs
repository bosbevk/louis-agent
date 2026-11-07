using LouisAgent.Puls.Admin;
using LouisAgent.Puls.Admin.Explorer.Sites;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Admin;

/// <summary>The one call a site makes so the eval pages find what they need.</summary>
public class EvalPagesTests
{
    /// <summary>The pages' own states are there, one for each user of the app.</summary>
    [Test]
    public void ThePagesStates_AreRegistered()
    {
        var services = new ServiceCollection();

        EvalPages.AddTo(services);

        Type[] states = [typeof(ReviewerState), typeof(ReviewQueueState), typeof(RunNotesState)];
        Assert.That(services.Where(s => s.Lifetime == ServiceLifetime.Scoped).Select(s => s.ServiceType),
            Is.SupersetOf(states));
    }

    /// <summary>A site that names no kinds of website gets none.</summary>
    [Test]
    public void WithNoKindsNamed_ThereAreNone()
    {
        var services = new ServiceCollection();

        EvalPages.AddTo(services);

        Assert.That(services.BuildServiceProvider().GetRequiredService<SiteKinds>(), Is.SameAs(SiteKinds.None));
    }

    /// <summary>A site that names them gets its own.</summary>
    [Test]
    public void TheKindsASiteNames_AreTheOnesUsed()
    {
        var services = new ServiceCollection();
        var kinds = new SiteKinds([new SiteKind("shops", "shop.example")]);

        EvalPages.AddTo(services, kinds);

        Assert.That(services.BuildServiceProvider().GetRequiredService<SiteKinds>(), Is.SameAs(kinds));
    }
}
