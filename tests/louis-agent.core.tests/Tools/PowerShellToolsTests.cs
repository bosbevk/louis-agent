using louis_agent.core.tools;

namespace louis_agent.core.tests;

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

    [Test]
    [Category("Integration")]
    public void PowerShellRun_InlineScript_ReturnsUtf8OutputAndStdin()
    {
        RequirePowerShell();

        var result = _tools.PowerShellRun(script: "\"héllo ✓ $([Console]::In.ReadToEnd().Trim())\"", standardInput: "from stdin");

        Assert.That(result, Does.Contain("Exit code: 0"));
        Assert.That(result, Does.Contain("héllo ✓ from stdin"));
    }

    [Test]
    [Category("Integration")]
    public void PowerShellRun_ScriptPath_PassesNamedSwitchAndPositionalArguments()
    {
        RequirePowerShell();
        File.WriteAllText(Path.Combine(_root, "greet.ps1"),
            "param([string]$Name, [int]$Count = 1, [switch]$Loud)\n\"Name=$Name Count=$Count Loud=$Loud\"");

        Assert.That(_tools.PowerShellRun(scriptPath: "greet.ps1", arguments: "-Name \"Louis M\" -Count 3 -Loud"),
            Does.Contain("Name=Louis M Count=3 Loud=True"));
        Assert.That(_tools.PowerShellRun(scriptPath: "greet.ps1", arguments: "Solo"), Does.Contain("Name=Solo Count=1"));
    }

    [TestCase("throw 'boom'", "Exit code: 1", "boom")]
    [TestCase("'partial'; exit 7", "Exit code: 7", "partial")]
    [TestCase("$ErrorActionPreference = 'Stop'; Get-Item 'Z:/definitely/missing'", "Exit code: 1", "missing")]
    [Category("Integration")]
    public void PowerShellRun_PropagatesErrorsAndExitCodes(string script, string exitCode, string expectedText)
    {
        RequirePowerShell();

        var result = _tools.PowerShellRun(script: script);

        Assert.That(result, Does.Contain(exitCode));
        Assert.That(result, Does.Contain(expectedText));
    }

    [Test]
    [Category("Integration")]
    public void PowerShellRun_KillsScriptAfterTimeout()
    {
        RequirePowerShell();
        Assert.That(_tools.PowerShellRun(script: "while ($true) { Start-Sleep -Seconds 1 }", timeoutSeconds: 3), Does.Contain("was killed"));
    }

    [Test]
    [Category("Integration")]
    public void PowerShellInfo_ReportsEditionAndVersion()
    {
        RequirePowerShell();

        var info = _tools.PowerShellInfo();

        Assert.That(info, Does.Match("Edition: (Desktop|Core)"));
        Assert.That(info, Does.Match(@"Version: \d+\.\d+"));
    }

    private void RequirePowerShell() =>
        Assume.That(_tools.ResolveInterpreter(), Is.Not.Null, "PowerShell is not installed on this machine");
}
