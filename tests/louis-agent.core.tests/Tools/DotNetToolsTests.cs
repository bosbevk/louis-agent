using louis_agent.core.tools;

namespace louis_agent.core.tests;

public class DotNetToolsTests
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
    public void SplitArguments_HonoursQuotes()
    {
        Assert.That(DotNetTools.SplitArguments("a \"b c\"  d"), Is.EqualTo(new[] { "a", "b c", "d" }));
        Assert.That(DotNetTools.SplitArguments("  "), Is.Empty);
    }

    [Test]
    public void TryResolveTarget_RejectsPathsOutsideWorkspace()
    {
        Assert.That(_tools.TryResolveTarget("../outside.csproj", out _, out var error), Is.False);
        Assert.That(error, Does.Contain("outside the workspace"));
    }

    [Test]
    public void TryResolveTarget_RejectsMissingPath()
    {
        Assert.That(_tools.TryResolveTarget("missing/App.csproj", out _, out var error), Is.False);
        Assert.That(error, Does.Contain("not found"));
    }

    [Test]
    public void SummarizeBuild_ExtractsDistinctErrorsAndWarnings()
    {
        const string output = """
            C:\src\Foo.cs(12,5): error CS1002: ; expected [C:\src\Foo.csproj]
            C:\src\Foo.cs(12,5): error CS1002: ; expected [C:\src\Foo.csproj]
            C:\src\Bar.cs(3,1): warning CS0168: The variable 'x' is declared but never used [C:\src\Foo.csproj]
            """;
        var summary = DotNetTools.SummarizeBuild(new ProcessRunner.Result(1, output, false), includeWarnings: true);

        Assert.That(summary, Does.StartWith("FAILED"));
        Assert.That(summary, Does.Contain("1 error(s), 1 warning(s)"));
        Assert.That(summary, Does.Contain(@"C:\src\Foo.cs(12,5): error CS1002: ; expected"));
        Assert.That(summary, Does.Not.Contain("[C:\\src\\Foo.csproj]"));
    }

    [Test]
    public void SummarizeTests_ReturnsSummaryAndFailureDetails()
    {
        const string output = """
              Passed MyTests.Adds [1 ms]
              Failed MyTests.Divides [3 ms]
              Error Message:
                 Expected: 2  But was: 3
              Stack Trace:
                 at MyTests.Divides() in C:\src\MyTests.cs:line 20
              Passed MyTests.Subtracts [1 ms]

            Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 10 ms - MyTests.dll (net10.0)
            """;
        var summary = DotNetTools.SummarizeTests(new ProcessRunner.Result(1, output, false));

        Assert.That(summary, Does.Contain("Failed!  - Failed:     1"));
        Assert.That(summary, Does.Contain("Failed MyTests.Divides"));
        Assert.That(summary, Does.Contain("Expected: 2  But was: 3"));
        Assert.That(summary, Does.Contain("MyTests.cs:line 20"));
        Assert.That(summary, Does.Not.Contain("MyTests.Adds"));
    }

    [Test]
    [Category("Integration")]
    public void DotNetBuild_ReportsCompileErrorWithLocation()
    {
        WriteConsoleApp("App", "class Program { static void Main() { int x = } }");

        var result = _tools.DotNetBuild("App");

        Assert.That(result, Does.StartWith("FAILED"));
        Assert.That(result, Does.Match(@"Program\.cs\(1,\d+\): error CS\d+"));
    }

    [Test]
    [Category("Integration")]
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
    [Category("Integration")]
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
