using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The Sass compiler's settings file is read at build time only. A project that has one marks it as never published
/// (FX62): left as content, every library's copy lands on the same relative path in a site's publish output and
/// <c>dotnet publish</c> refuses with NETSDK1152, which a build alone never shows. Every project beside a
/// <c>sasscompiler.json</c> is read from disk, so a new library or site comes under the rule by itself.
/// </summary>
public class SassSettingsStayOutOfAPublishTests
{
    private static readonly string[] Areas = ["src/web", "packs/web", "demos"];

    /// <summary>Every project file that sits beside a <c>sasscompiler.json</c>.</summary>
    private static IEnumerable<string> ProjectsWithSassSettings() =>
        Areas.Select(area => Path.Combine(TestPaths.RepoRoot, area))
            .Where(Directory.Exists)
            .SelectMany(area => Directory.EnumerateFiles(area, "sasscompiler.json", SearchOption.AllDirectories))
            .Where(settings => !settings.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(settings => Directory.EnumerateFiles(Path.GetDirectoryName(settings)!, "*.csproj"));

    [Test]
    public void AProjectWithSassSettings_KeepsThemOutOfAPublish()
    {
        var missing = ProjectsWithSassSettings()
            .Where(project => !File.ReadAllText(project).Contains(
                "<Content Update=\"sasscompiler.json\" CopyToOutputDirectory=\"Never\" CopyToPublishDirectory=\"Never\"",
                StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.That(ProjectsWithSassSettings(), Is.Not.Empty,
            "No project with Sass settings was found, so this test would prove nothing.");
        Assert.That(missing, Is.Empty,
            "mark sasscompiler.json CopyToOutputDirectory=Never CopyToPublishDirectory=Never (FX62)");
    }
}
