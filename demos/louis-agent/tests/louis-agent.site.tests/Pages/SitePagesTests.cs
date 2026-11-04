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
    [TestCase("/", "louis-agent · an AI agent engine in C#")]
    [TestCase("/how-it-works", "How the engine works")]
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
