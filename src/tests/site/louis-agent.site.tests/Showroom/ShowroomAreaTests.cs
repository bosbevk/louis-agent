using LouisAgent.Site.Components.Showroom;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Widget.Door;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.6: louis-agent.dev names its areas through the framework's builder, its showroom with its own demos; the
/// guide's way in, the demos' windows and the guide kept across layouts come with it, and the guide's chat plumbing
/// with every site.
/// </summary>
public class ShowroomAreaTests
{
    /// <summary>The areas the program names bring the showroom's demos and the guide's pieces.</summary>
    [Test]
    public void TheSitesAreas_BringTheShowroom()
    {
        var services = new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name);

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(s => s.ImplementationInstance == SiteDemos.All), Is.True, "the site's demos");
            Assert.That(services.Any(s => s.ServiceType == typeof(GuideDoor)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(AgentWindows)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(GuideKeeper)), Is.True);
        });
    }

    /// <summary>The program names its areas through the same method, with nothing of theirs wired by hand.</summary>
    [Test]
    public void TheProgram_NamesItsAreas()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSiteAreas(LouisAgentDevAreas.Name)"));
            Assert.That(program, Does.Not.Contain("AddSingleton<GuideKeeper>").And.Not.Contain("AddSingleton<ChatStore>")
                .And.Not.Contain("AddSingleton(SiteDemos.All)"));
        });
    }
}
