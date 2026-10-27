using LouisAgent.Testing;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.Tests.Tools.Shell;

public class PowerShellToolsTests
{
    private string _root = null!;

    private PowerShellTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("powershell-tools-").FullName;
        _tools = new PowerShellTools(_root);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    [TestCase("", "")]
    [TestCase("'hi'", "script.ps1")]
    public void PowerShellRun_RequiresExactlyOneOfScriptOrPath(string script, string path) =>
        Assert.That(_tools.PowerShellRun(script, path), Does.Contain("exactly one"));

    [Test]
    public void PowerShellRun_RejectsPathsOutsideWorkspace() =>
        Assert.That(_tools.PowerShellRun(scriptPath: "../outside.ps1"), Does.Contain("outside the workspace"));

    [Test]
    public void PowerShellRun_RejectsNonPs1Files()
    {
        File.WriteAllText(Path.Combine(_root, "notes.txt"), "x");
        Assert.That(_tools.PowerShellRun(scriptPath: "notes.txt"), Does.Contain(".ps1"));
    }
}
