using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.DotNet;

namespace LouisAgent.Core.IntegrationTests.Tools.DotNet;

[Category("Integration")]
public class DotNetToolsIntegrationTests
{
    private string _root = null!;

    private DotNetTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("dotnet-tools-").FullName;
        _tools = new DotNetTools(_root);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* build servers may hold files briefly */ }
    }

    [Test]
    public void DotNetBuild_ReportsCompileErrorWithLocation()
    {
        WriteConsoleApp("App", "class Program { static void Main() { int x = } }");

        var result = _tools.DotNetBuild("App");

        Assert.That(result, Does.StartWith("FAILED"));
        Assert.That(result, Does.Match(@"Program\.cs\(1,\d+\): error CS\d+"));
    }

    [Test]
    public void DotNetRun_ReturnsOutputAndUnhandledExceptionStackTrace()
    {
        WriteConsoleApp("App", """
            System.Console.WriteLine("hello " + args[0] + " " + System.Console.ReadLine());
            throw new System.InvalidOperationException("boom");
            """);

        var result = _tools.DotNetRun("App/App.csproj", "\"big world\"", standardInput: "from stdin");

        Assert.That(result, Does.Contain("hello big world from stdin"));
        Assert.That(result, Does.Contain("InvalidOperationException: boom"));
        Assert.That(result, Does.Not.Contain("Exit code: 0"));
    }

    [Test]
    public void DotNetListProjects_FindsProjectsAndSkipsBuildOutput()
    {
        WriteConsoleApp("App", "System.Console.WriteLine();");
        Directory.CreateDirectory(Path.Combine(_root, "App", "obj"));
        File.WriteAllText(Path.Combine(_root, "App", "obj", "Generated.csproj"), "<Project />");

        var result = _tools.DotNetListProjects();

        Assert.That(result, Does.Contain(Path.Combine("App", "App.csproj")));
        Assert.That(result, Does.Not.Contain("Generated.csproj"));
    }

    private void WriteConsoleApp(string name, string program)
    {
        string dir = Directory.CreateDirectory(Path.Combine(_root, name)).FullName;
        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>disable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(dir, "Program.cs"), program);
    }
}
