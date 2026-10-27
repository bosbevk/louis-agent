using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.DotNet;

namespace LouisAgent.Core.Tests.Tools.DotNet;

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
}
