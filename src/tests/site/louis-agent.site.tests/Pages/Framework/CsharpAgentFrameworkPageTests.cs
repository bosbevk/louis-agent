using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Pages.Framework;

namespace LouisAgent.Site.Tests.Pages.Framework;

/// <summary>
/// The page written for what people type into a search engine (F123): what louis-agent is as an AI agent framework in
/// C#, and how it compares with Microsoft Agent Framework and Semantic Kernel, each claim about theirs linked to their
/// own documentation, and each claim about ours held to the repo.
/// </summary>
public class CsharpAgentFrameworkPageTests
{
    /// <summary>Microsoft's own pages, which every claim the page makes about their frameworks comes from.</summary>
    private static readonly string[] Sources =
    [
        "https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview",
        "https://github.com/microsoft/agent-framework",
        "https://learn.microsoft.com/en-us/semantic-kernel/overview/",
        "https://github.com/microsoft/semantic-kernel"
    ];

    /// <summary>
    /// The page is at the address people search for, names the words they type, links every source it uses and keeps
    /// the site's rule of no semicolons.
    /// </summary>
    [Test]
    public void ThePage_IsAtItsAddress_AndLinksItsSources()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<CsharpAgentFrameworkPage>();

        var route = typeof(CsharpAgentFrameworkPage).GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>().Single().Template;
        var links = cut.FindAll("a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/csharp-ai-agent-framework"));
            Assert.That(cut.Find("h2").TextContent, Does.Contain("AI agent framework in C#"));
            Assert.That(cut.Markup, Does.Contain("Microsoft Agent Framework").And.Contain("Semantic Kernel"));
            Assert.That(Sources.Where(source => !links.Contains(source)), Is.Empty, "every source is linked");
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }

    /// <summary>
    /// What the page says of louis-agent is so: the engine is .NET 10 on Microsoft.Extensions.AI, and it runs on your
    /// own hardware (Louis's selling point): a local model through the Ollama provider, the hosts, the site and Ollama
    /// from one Docker Compose file, no cloud agent service in between.
    /// </summary>
    [Test]
    public void WhatItSaysOfOurs_IsSo()
    {
        using var ctx = SitePageContext.Create();
        string core = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/engine/louis-agent.core/louis-agent.core.csproj"));
        string compose = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/docker/docker-compose.yml"));

        var cut = ctx.Render<CsharpAgentFrameworkPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Microsoft.Extensions.AI").And.Contain(".NET 10"));
            Assert.That(core, Does.Contain("<PackageReference Include=\"Microsoft.Extensions.AI\""));
            Assert.That(core, Does.Contain("<TargetFramework>net10.0</TargetFramework>"));
            Assert.That(cut.Markup, Does.Contain("your own hardware").And.Contain("Ollama").And.Contain("Docker"));
            Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, "src/engine/louis-agent.core.ollama")));
            Assert.That(compose, Does.Contain("\n  api:").And.Contain("\n  ollama:"), "the site's API and Ollama");
        });
    }
}
