namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// Core ships no browser binary and no tracer (CL33): its project file names neither PuppeteerSharp nor an
/// OpenTelemetry package; those are <c>LouisAgent.Core.Web</c>'s and <c>LouisAgent.Core.Telemetry</c>'s, which a
/// host opts into.
/// </summary>
public class CorePackagesTests
{
    private static string Project(string path) =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, path));

    /// <summary>
    /// How a package is named in a project file. Only that counts: a friend assembly such as
    /// <c>LouisAgent.Core.Anthropic</c> may carry a package's word in its name.
    /// </summary>
    private static string PackageReference(string package) => $"<PackageReference Include=\"{package}";

    [TestCase("PuppeteerSharp")]
    [TestCase("OpenTelemetry")]
    [TestCase("Anthropic")]
    [TestCase("OllamaSharp")]
    [TestCase("Microsoft.Extensions.AI.OpenAI")]
    public void Core_NamesNoPackageOf(string package) =>
        Assert.That(Project("src/engine/LouisAgent.Core/LouisAgent.Core.csproj"),
            Does.Not.Contain(PackageReference(package)));

    [TestCase("src/engine/LouisAgent.Core.Web/LouisAgent.Core.Web.csproj", "PuppeteerSharp")]
    [TestCase("src/engine/LouisAgent.Core.Telemetry/LouisAgent.Core.Telemetry.csproj", "OpenTelemetry")]
    [TestCase("src/engine/LouisAgent.Core.Anthropic/LouisAgent.Core.Anthropic.csproj", "Anthropic")]
    [TestCase("src/engine/LouisAgent.Core.Ollama/LouisAgent.Core.Ollama.csproj", "OllamaSharp")]
    [TestCase("src/engine/LouisAgent.Core.OpenAi/LouisAgent.Core.OpenAi.csproj", "Microsoft.Extensions.AI.OpenAI")]
    public void TheLibraryOnCore_IsWhereThePackageLives(string project, string package) =>
        Assert.That(Project(project), Does.Contain(PackageReference(package)));
}
