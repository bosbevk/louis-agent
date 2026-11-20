using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Build;
using LouisAgent.Web.Framework.Pages.Build;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// Run it, at <c>/run-it</c>: the stacks that run on your own machine, the dashboard that watches them, and the two
/// sites in Azure. Every stack it names is a compose file of the repo with that service in it, and every command it
/// shows names a file that is there, so the page can't tell a reader to start something that isn't.
/// </summary>
public class RunItPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        // The site's words: the code listing's copy button reads them.
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>It answers at its address with its heading and its sections, in the menu.</summary>
    [Test]
    public void ThePage_HasItsHeading_AndItsSections()
    {
        using var ctx = Context();

        var cut = ctx.Render<RunItPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Run it where you want."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.EqualTo(new[] { "top", "on-your-machine", "watch-it", "in-azure" }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/run-it"),
                "in the menu");
        });
    }

    /// <summary>Each stack is a compose file of the repo, and each service it names is one of that file's.</summary>
    [Test]
    public void EveryStack_IsAComposeFile_WithItsServices()
    {
        Assert.Multiple(() =>
        {
            foreach (var stack in RunIt.Stacks)
            {
                string path = Path.Combine(TestPaths.RepoRoot, stack.File);
                Assert.That(File.Exists(path), stack.File);
                string compose = File.ReadAllText(path);
                foreach (string service in stack.Services)
                    Assert.That(compose, Does.Contain($"\n  {service}:"), $"{stack.File} runs {service}");
            }
        });
    }

    /// <summary>Each command on the page names a compose file that is in the repo.</summary>
    [Test]
    public void EveryCommand_NamesAFileInTheRepo()
    {
        Assert.Multiple(() =>
        {
            foreach (var stack in RunIt.Stacks)
                Assert.That(stack.Command, Does.Contain(stack.File), $"{stack.File}'s command");
        });
    }

    /// <summary>The Azure section names the workflow that releases both sites, and the script it runs.</summary>
    [Test]
    public void InAzure_NamesTheWorkflowAndTheScript()
    {
        Assert.Multiple(() =>
        {
            foreach (string file in RunIt.ReleaseFiles)
                Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, file)), file);
        });
    }
}
