using System.Diagnostics;

namespace LouisAgent.Starter.IntegrationTests;

/// <summary>
/// The one test that proves the template: <c>dotnet new louis-site</c> really makes a site from the starter, with its
/// own name throughout, and it builds. The template's list goes in a throwaway hive, so this machine's isn't touched,
/// and the site goes in a throwaway demo folder, deleted after.
/// </summary>
[Category("Integration")]
public class SpawnASiteTests
{
    private string _demo = null!;
    private string _hive = null!;

    /// <summary>Names a throwaway demo folder at the depth a site's paths expect, and a throwaway hive.</summary>
    [SetUp]
    public void Folders()
    {
        string id = Guid.NewGuid().ToString("N")[..8];
        _demo = Path.Combine(TestPaths.RepoRoot, "demos", $".spawn-{id}");
        _hive = Path.Combine(Path.GetTempPath(), $"louis-spawn-hive-{id}");
    }

    /// <summary>Deletes both, whatever happened.</summary>
    [TearDown]
    public void Tidy()
    {
        if (Directory.Exists(_demo)) Directory.Delete(_demo, recursive: true);
        if (Directory.Exists(_hive)) Directory.Delete(_hive, recursive: true);
    }

    /// <summary>A new site has its own name throughout, keeps nothing the build makes, and builds.</summary>
    [Test]
    public void ANewSite_HasItsOwnName_AndBuilds()
    {
        string starter = Path.Combine(TestPaths.RepoRoot, "demos/starter/web/louis-agent.starter");
        string site = Path.Combine(_demo, "sites", "acme");

        Run($"new install \"{starter}\" --debug:custom-hive \"{_hive}\"");
        Run($"new louis-site -n Acme -o \"{site}\" --debug:custom-hive \"{_hive}\"");

        string project = Path.Combine(site, "acme.csproj");
        Assert.That(File.Exists(project), Is.True, "the project takes the site's name");
        Assert.That(Directory.Exists(Path.Combine(site, ".template.config")), Is.False);
        var leftOver = Directory.EnumerateFiles(site, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor" or ".csproj" or ".html")
            .Where(file => File.ReadAllText(file).Contains("Starter", StringComparison.OrdinalIgnoreCase));
        Assert.That(leftOver, Is.Empty);

        Run($"build \"{project}\" -nodeReuse:false -p:UseSharedCompilation=false");
        Assert.That(File.Exists(Path.Combine(site, "wwwroot/css/app.css")), Is.True, "the build makes its styles");
    }

    /// <summary>Runs the dotnet tool and fails the test with what it said if it didn't work.</summary>
    private static void Run(string arguments)
    {
        var start = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = TestPaths.RepoRoot
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.Zero, $"dotnet {arguments}\n{output.Result}\n{errors.Result}");
    }
}
