using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site started in memory: every page answers with HTML, the home page carries the short film and How it works
/// the detailed one, and a container's health check gets its OK.
/// </summary>
public class SitePagesTests
{
    /// <summary>The site, started once for the fixture.</summary>
    private WebApplicationFactory<Program> _site = null!;

    [OneTimeSetUp]
    public void StartTheSite() => _site = new WebApplicationFactory<Program>();

    [OneTimeTearDown]
    public void StopTheSite() => _site.Dispose();

    /// <summary>Each page of the menu answers.</summary>
    [TestCase("/", "louis-agent · an AI agent framework in C#")]
    [TestCase("/how-it-works", "How the engine works")]
    [TestCase("/packs", "Packs")]
    [TestCase("/proof", "Measured, not assumed")]
    [TestCase("/built-on-it", "Built on it")]
    [TestCase("/contact", "Contact")]
    public async Task APage_Answers_WithItsTitle(string path, string title)
    {
        using var reply = await _site.CreateClient().GetAsync(path);

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(reply.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
        Assert.That(await reply.Content.ReadAsStringAsync(), Does.Contain($"<title>{title}"));
    }

    /// <summary>The words from before the split: a shops pack that went, a list that isn't sorted, tools in the engine.</summary>
    [TestCase("/built-on-it", "A shops pack")]
    [TestCase("/how-it-works", "tools sorted by name")]
    [TestCase("/how-it-works", "The tool loop, streaming, the providers, the usage ledger and the guards.")]
    [TestCase("/", "60+ ADRs")]
    public async Task APage_DropsTheOldStructure(string path, string old)
    {
        string html = await _site.CreateClient().GetStringAsync(path);

        Assert.That(html, Does.Not.Contain(old));
    }

    /// <summary>What's new is on the page: it's a framework, and a demo is built on it.</summary>
    [TestCase("/", "An AI agent framework")]
    [TestCase("/how-it-works", "holds no agent and no tools")]
    [TestCase("/how-it-works", "never the engine")]
    [TestCase("/built-on-it", "Each is a demo")]
    public async Task APage_TellsTheNewStructure(string path, string words)
    {
        string html = await _site.CreateClient().GetStringAsync(path);

        Assert.That(html, Does.Contain(words));
    }

    /// <summary>The home page shows the short film; How it works the detailed one; neither the other's.</summary>
    [TestCase("/", "film-short", "film-detail")]
    [TestCase("/how-it-works", "film-detail", "film-short")]
    public async Task APage_ShowsItsFilm(string path, string film, string notThisOne)
    {
        string html = await _site.CreateClient().GetStringAsync(path);

        Assert.That(html, Does.Contain($"class=\"stage {film}\""));
        Assert.That(html, Does.Not.Contain(notThisOne));
    }

    /// <summary>A page is plain HTML: no Blazor script, since nothing on the site is interactive.</summary>
    [Test]
    public async Task APage_IsPlainHtml_WithNoBlazorScript()
    {
        string html = await _site.CreateClient().GetStringAsync("/");

        Assert.That(html, Does.Not.Contain("blazor.web.js"));
        Assert.That(html, Does.Contain("css/app.css"));
    }

    /// <summary>The container's health check.</summary>
    [Test]
    public async Task Health_IsOk()
    {
        using var reply = await _site.CreateClient().GetAsync("/health");

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
