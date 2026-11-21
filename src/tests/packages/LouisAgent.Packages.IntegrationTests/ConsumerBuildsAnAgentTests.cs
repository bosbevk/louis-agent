using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace LouisAgent.Packages.IntegrationTests;

/// <summary>
/// F166.2 step 3, the proof the plan rests on: the framework packed into a folder is all a project outside the
/// repository needs to build an agent. Each run packs a version of its own, so NuGet's cache on this machine never
/// hands back an older copy.
/// </summary>
[Category("Integration")]
public class ConsumerBuildsAnAgentTests
{
    /// <summary>How many packages the framework ships (PackagesTests holds which).</summary>
    private const int PackageCount = 31;

    /// <summary>What the outside project's agent answers, through the fake chat client.</summary>
    private const string Answer = "hello from the packages";

    private string _root = null!;
    private string _version = null!;

    /// <summary>A folder of its own for the packages and the outside project, and a version no other run has.</summary>
    [SetUp]
    public void Folders()
    {
        _root = Directory.CreateTempSubdirectory("louis-packages-").FullName;
        _version = $"0.0.0-local.{Guid.NewGuid():N}"[..28];
    }

    /// <summary>Deletes the folder, whatever happened.</summary>
    [TearDown]
    public void Tidy() => Directory.Delete(_root, recursive: true);

    /// <summary>
    /// The 31 pack; a package's framework dependency names the version of the project it referenced, as "this one or
    /// higher"; and a project outside restores the packages from the folder and runs an agent that answers.
    /// </summary>
    [Test]
    public void AProjectOutside_BuildsAnAgent_FromThePackagesAlone()
    {
        string feed = Path.Combine(_root, "feed");
        Dotnet(TestPaths.RepoRoot, "pack", "louis-agent-solution.sln", "-o", feed,
            $"-p:MinVerVersionOverride={_version}");

        var packages = Directory.EnumerateFiles(feed, "*.nupkg").ToList();
        Assert.That(packages, Has.Count.EqualTo(PackageCount));
        Assert.That(DependencyVersion(feed, "LouisAgent.Web", "LouisAgent.Composition"), Is.EqualTo(_version));

        string consumer = WriteConsumer(feed);
        string output = Dotnet(consumer, "run");

        Assert.That(output, Does.Contain(Answer));
    }

    /// <summary>The version a package's nuspec names for one of its dependencies.</summary>
    private string DependencyVersion(string feed, string package, string dependency)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(feed, $"{package}.{_version}.nupkg"));
        using var nuspec = archive.GetEntry($"{package}.nuspec")!.Open();
        return XDocument.Load(nuspec).Descendants().First(element =>
                element.Name.LocalName == "dependency" && (string?)element.Attribute("id") == dependency)
            .Attribute("version")!.Value;
    }

    /// <summary>
    /// A console project outside the repository that knows only the folder of packages and nuget.org: it builds an
    /// agent on the fake chat client and prints its answer.
    /// </summary>
    private string WriteConsumer(string feed)
    {
        string project = Path.Combine(_root, "consumer");
        Directory.CreateDirectory(project);

        var sources = new XElement("packageSources", new XElement("clear"),
            new XElement("add", new XAttribute("key", "framework"), new XAttribute("value", feed)),
            new XElement("add", new XAttribute("key", "nuget.org"),
                new XAttribute("value", "https://api.nuget.org/v3/index.json")));
        new XElement("configuration", sources).Save(Path.Combine(project, "nuget.config"));

        var properties = new XElement("PropertyGroup", new XElement("OutputType", "Exe"),
            new XElement("TargetFramework", "net10.0"), new XElement("ImplicitUsings", "enable"),
            new XElement("Nullable", "enable"));
        var references = new XElement("ItemGroup",
            new XElement("PackageReference", new XAttribute("Include", "LouisAgent.Core"),
                new XAttribute("Version", _version)),
            new XElement("PackageReference", new XAttribute("Include", "LouisAgent.Testing"),
                new XAttribute("Version", _version)));
        new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"), properties, references)
            .Save(Path.Combine(project, "Consumer.csproj"));

        string program = $$"""
            using LouisAgent.Core.Agent;
            using LouisAgent.Core.Config;
            using LouisAgent.Core.Skills.Providers;
            using LouisAgent.Testing;
            using Microsoft.Extensions.AI;

            var answer = new ChatMessage(ChatRole.Assistant, "{{Answer}}");
            var model = new FakeChatClient(_ => new ChatResponse(answer));
            var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
            var agent = new AgentEngine(new CompositeSkillProvider(), model, options, supportsTools: false);
            Console.WriteLine(await agent.ProcessPromptAsync([], "hello"));
            """;
        File.WriteAllText(Path.Combine(project, "Program.cs"), program);
        return project;
    }

    /// <summary>
    /// Runs dotnet with its arguments as a list and hands back what it printed, failing with it if it failed.
    /// </summary>
    private static string Dotnet(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        Assert.That(process.ExitCode, Is.Zero,
            $"dotnet {string.Join(' ', arguments)}\n{output.Result}\n{errors.Result}");
        return output.Result;
    }
}
