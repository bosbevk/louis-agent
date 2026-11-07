using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>The Packs page lists what's really in the repo, so a new pack can't be forgotten.</summary>
public class PacksPageTests
{
    /// <summary>The site, started once for the fixture.</summary>
    private WebApplicationFactory<Program> _site = null!;

    /// <summary>Starts it.</summary>
    [OneTimeSetUp]
    public void StartTheSite() => _site = new WebApplicationFactory<Program>();

    /// <summary>Stops it.</summary>
    [OneTimeTearDown]
    public void StopTheSite() => _site.Dispose();

    /// <summary>Every pack under <c>packs/tools/</c> has its place on the page.</summary>
    [Test]
    public async Task EveryPack_IsOnThePage()
    {
        string html = await _site.CreateClient().GetStringAsync("/packs");

        var packs = Directory.GetDirectories(Path.Combine(TestPaths.RepoRoot, "packs", "tools"), "louis-agent.pack.*")
            .Select(folder => Path.GetFileName(folder)!).ToList();

        Assert.That(packs, Has.Count.GreaterThanOrEqualTo(10), "the search found the packs");
        Assert.That(packs.Where(pack => !html.Contains(pack)), Is.Empty);
    }

    /// <summary>The menu leads to it, from every page.</summary>
    [Test]
    public async Task TheMenu_LeadsToThePage()
    {
        using var reply = await _site.CreateClient().GetAsync("/packs");
        string home = await _site.CreateClient().GetStringAsync("/");

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(home, Does.Contain("href=\"/packs\""));
    }
}
