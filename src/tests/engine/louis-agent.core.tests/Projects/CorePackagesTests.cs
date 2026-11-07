namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// Core ships no browser binary and no tracer (CL33): its project file names neither PuppeteerSharp nor an
/// OpenTelemetry package; those are <c>louis-agent.core.web</c>'s and <c>louis-agent.core.telemetry</c>'s, which a
/// host opts into.
/// </summary>
public class CorePackagesTests
{
    private static string Project(string path) =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, path));

    [TestCase("PuppeteerSharp")]
    [TestCase("OpenTelemetry")]
    public void Core_NamesNoPackageOf(string package) =>
        Assert.That(Project("src/engine/louis-agent.core/louis-agent.core.csproj"), Does.Not.Contain(package));

    [TestCase("src/engine/louis-agent.core.web/louis-agent.core.web.csproj", "PuppeteerSharp")]
    [TestCase("src/engine/louis-agent.core.telemetry/louis-agent.core.telemetry.csproj", "OpenTelemetry")]
    public void TheLibraryOnCore_IsWhereThePackageLives(string project, string package) =>
        Assert.That(Project(project), Does.Contain(package));
}
