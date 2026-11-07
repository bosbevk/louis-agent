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
    [TestCase("Anthropic")]
    [TestCase("OllamaSharp")]
    [TestCase("Microsoft.Extensions.AI.OpenAI")]
    public void Core_NamesNoPackageOf(string package) =>
        Assert.That(Project("src/engine/louis-agent.core/louis-agent.core.csproj"), Does.Not.Contain(package));

    [TestCase("src/engine/louis-agent.core.web/louis-agent.core.web.csproj", "PuppeteerSharp")]
    [TestCase("src/engine/louis-agent.core.telemetry/louis-agent.core.telemetry.csproj", "OpenTelemetry")]
    [TestCase("src/engine/louis-agent.core.anthropic/louis-agent.core.anthropic.csproj", "Anthropic")]
    [TestCase("src/engine/louis-agent.core.ollama/louis-agent.core.ollama.csproj", "OllamaSharp")]
    [TestCase("src/engine/louis-agent.core.openai/louis-agent.core.openai.csproj", "Microsoft.Extensions.AI.OpenAI")]
    public void TheLibraryOnCore_IsWhereThePackageLives(string project, string package) =>
        Assert.That(Project(project), Does.Contain(package));
}
