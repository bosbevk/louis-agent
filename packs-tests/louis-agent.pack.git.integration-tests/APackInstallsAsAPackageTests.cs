using LouisAgent.Composition.Tools;

namespace LouisAgent.Packs.Git.IntegrationTests;

/// <summary>What "it's a framework of packages" means: a plain console app takes a pack from a feed, and nothing else.</summary>
[Category("Integration")]
public class APackInstallsAsAPackageTests
{
    private static readonly TimeSpan Build = TimeSpan.FromMinutes(5);

    /// <summary>The app lists the pack's tools and finds its guide, with no engine anywhere in its dependencies.</summary>
    [Test]
    public void AConsoleApp_RunsAPackFromAPackage_WithoutTheEngine()
    {
        Outside.Tool(ProcessRunner.Run("dotnet", ["--version"], Path.GetTempPath(), TimeSpan.FromMinutes(1)).ExitCode == 0,
            "the .NET SDK isn't on this machine");
        string root = Directory.CreateTempSubdirectory("pack-proof-").FullName;
        try
        {
            // The three packages, into a feed of their own.
            string feed = Directory.CreateDirectory(Path.Combine(root, "feed")).FullName;
            foreach (string project in new[]
                     {
                         "src/engine/louis-agent.choices", "src/engine/louis-agent.composition", "packs/louis-agent.pack.git",
                     })
                Assert.That(Run(TestPaths.RepoRoot, "pack", project, "-c", "Release", "-o", feed, "--nologo").ExitCode, Is.Zero);

            // An app that knows only the feed.
            string app = Directory.CreateDirectory(Path.Combine(root, "app")).FullName;
            File.WriteAllText(Path.Combine(app, "app.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="louis-agent.pack.git" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(app, "Program.cs"), """
                using LouisAgent.Packs.Git;

                var pack = new GitPack();
                var toolset = pack.Toolsets.Single();
                Console.WriteLine(string.Join(",", toolset.ToolTypes!.Select(t => t.Name)));
                Console.WriteLine(pack.Prompts.GetFileInfo(toolset.GuideFile!).Exists);
                """);
            File.WriteAllText(Path.Combine(app, "nuget.config"), $"""
                <configuration>
                  <packageSources>
                    <add key="local" value="{feed}" />
                  </packageSources>
                </configuration>
                """);

            var result = Run(app, "run", "--nologo");

            Assert.That(result.ExitCode, Is.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("GitChangeTools").And.Contain("True"));
            Assert.That(File.ReadAllText(Path.Combine(app, "obj", "project.assets.json")),
                Does.Not.Contain("louis-agent.core"), "a pack needs the contracts, never the engine");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Runs the SDK in a folder.</summary>
    private static ProcessRunner.Result Run(string folder, params string[] arguments) =>
        ProcessRunner.Run("dotnet", arguments, folder, Build);
}
