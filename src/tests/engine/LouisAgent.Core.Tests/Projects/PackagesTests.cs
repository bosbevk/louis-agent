namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// F166.2: the framework ships as packages, the 31 projects Puls builds on and no other (Louis's pick C), each
/// versioned by tags of its own name (MinVer, his pick A), all under the framework's licence (ADR 0127).
/// </summary>
public class PackagesTests
{
    /// <summary>The projects published as packages: what Puls's projects reach, read from their references.</summary>
    private static readonly string[] Packages =
    [
        "LouisAgent.Choices", "LouisAgent.Composition", "LouisAgent.Core", "LouisAgent.Core.Anthropic",
        "LouisAgent.Core.Ollama", "LouisAgent.Core.OpenAi", "LouisAgent.Core.Telemetry", "LouisAgent.Core.Web",
        "LouisAgent.SessionStream", "LouisAgent.Evals", "LouisAgent.Evals.Checks", "LouisAgent.Evals.Protocol",
        "LouisAgent.Evals.Runner", "LouisAgent.Accounts", "LouisAgent.Accounts.Contracts", "LouisAgent.Api",
        "LouisAgent.Api.Contracts", "LouisAgent.Api.Evals", "LouisAgent.Web", "LouisAgent.Web.Accounts",
        "LouisAgent.Web.Evals", "LouisAgent.Diagrams", "LouisAgent.Diagrams.Engine", "LouisAgent.Packs.Calculator",
        "LouisAgent.Packs.DotNet", "LouisAgent.Packs.Files", "LouisAgent.Packs.Git", "LouisAgent.Packs.Shell",
        "LouisAgent.Packs.Web", "LouisAgent.Testing", "LouisAgent.Web.Testing"
    ];

    /// <summary>
    /// Every project file under the repository's three roots, by its name and its text; a sample the coding agent
    /// works on may stand twice, so a name is not a key.
    /// </summary>
    private static List<KeyValuePair<string, string>> Projects() =>
    [
        .. new[] { "src", "packs", "demos" }
            .SelectMany(root => Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, root), "*.csproj",
                SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(file => KeyValuePair.Create(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file)))
    ];

    /// <summary>The 31 say they are packages, and no other project does.</summary>
    [Test]
    public void The31_AndNoOther_ArePackable()
    {
        var packable = Projects().Where(project => project.Value.Contains("<IsPackable>true</IsPackable>"))
            .Select(project => project.Key);

        Assert.That(packable, Is.EquivalentTo(Packages));
    }

    /// <summary>
    /// Each package is released by tags of its own name and a dash, as <c>LouisAgent.Core-0.1.0</c>: the prefix is
    /// the project's name, which ADR 0129 makes the package's, so one line sets it for all of them and no project sets
    /// one of its own.
    /// </summary>
    [Test]
    public void EachPackage_IsTaggedByItsOwnName()
    {
        string targets = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "Directory.Build.targets"));
        var ownPrefix = Projects().Where(project => project.Value.Contains("<MinVerTagPrefix>"))
            .Select(project => project.Key);

        Assert.Multiple(() =>
        {
            Assert.That(targets, Does.Contain("<MinVerTagPrefix>$(MSBuildProjectName)-</MinVerTagPrefix>"));
            Assert.That(ownPrefix, Is.Empty);
        });
    }

    /// <summary>
    /// The shared settings make a project no package unless it says so, name the licence by its SPDX name, version
    /// each package by its own tags alone, and give MinVer only to what is packed.
    /// </summary>
    [Test]
    public void TheSharedSettings_NameTheLicence_AndKeepEachVersionItsOwn()
    {
        string props = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "Directory.Build.props"));
        string targets = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "Directory.Build.targets"));

        Assert.Multiple(() =>
        {
            Assert.That(props, Does.Contain("<IsPackable>false</IsPackable>"));
            Assert.That(props,
                Does.Contain("<PackageLicenseExpression>PolyForm-Noncommercial-1.0.0</PackageLicenseExpression>"));
            Assert.That(props, Does.Contain("<MinVerIgnoreHeight>true</MinVerIgnoreHeight>"));
            Assert.That(targets, Does.Contain("Condition=\"'$(IsPackable)' == 'true'\""));
            Assert.That(targets, Does.Contain("<PackageReference Include=\"MinVer\""));
        });
    }

    /// <summary>
    /// A package that holds Sass carries it, under <c>sass/</c>, with the shared build file a site's build runs, so a site
    /// outside this repository compiles the framework's Sass as one inside does (Louis's pick A of F166.3's finding 6).
    /// </summary>
    [Test]
    public void APackageWithSass_CarriesIt_AndTheFileThatUnpacksIt()
    {
        string targets = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "Directory.Build.targets"));

        Assert.Multiple(() =>
        {
            Assert.That(targets, Does.Contain("Include=\"Styles/**/*.scss\" Pack=\"true\" PackagePath=\"sass/"));
            Assert.That(targets, Does.Contain("PackagePath=\"buildTransitive/$(PackageId).targets\""));
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, "build", "FrameworkSass.targets")), Is.True);
        });
    }

    /// <summary>
    /// The shared build file gathers every framework package's Sass in one list and copies it into the site's
    /// <c>obj/framework-sass/</c> before the Sass compiler runs, so two packages never fight over one target.
    /// </summary>
    [Test]
    public void TheUnpacking_GathersEveryPackagesSass_BeforeTheCompilerRuns()
    {
        string path = Path.Combine(TestPaths.RepoRoot, "build", "FrameworkSass.targets");
        string unpack = File.Exists(path) ? File.ReadAllText(path) : "";

        Assert.Multiple(() =>
        {
            Assert.That(unpack, Does.Contain("<FrameworkSass Include=\"$(MSBuildThisFileDirectory)../sass/**/*.scss\""));
            Assert.That(unpack, Does.Contain("BeforeTargets=\"Compile Sass\""));
            Assert.That(unpack, Does.Contain("$(BaseIntermediateOutputPath)framework-sass/%(RecursiveDir)"));
        });
    }
}
