using LouisAgent.Testing;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.IntegrationTests.Tools.Shell;

[Category("Integration")]
public class PowerShellToolsIntegrationTests
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

    [Test]
    public void PowerShellRun_InlineScript_ReturnsUtf8OutputAndStdin()
    {
        RequirePowerShell();

        var result = _tools.PowerShellRun(script: "\"héllo ✓ $([Console]::In.ReadToEnd().Trim())\"", standardInput: "from stdin");

        Assert.That(result, Does.Contain("Exit code: 0"));
        Assert.That(result, Does.Contain("héllo ✓ from stdin"));
    }

    [Test]
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
    public void PowerShellRun_PropagatesErrorsAndExitCodes(string script, string exitCode, string expectedText)
    {
        RequirePowerShell();

        var result = _tools.PowerShellRun(script: script);

        Assert.That(result, Does.Contain(exitCode));
        Assert.That(result, Does.Contain(expectedText));
    }

    [Test]
    public void PowerShellRun_KillsScriptAfterTimeout()
    {
        RequirePowerShell();
        Assert.That(_tools.PowerShellRun(script: "while ($true) { Start-Sleep -Seconds 1 }", timeoutSeconds: 3), Does.Contain("was killed"));
    }

    [Test]
    public void PowerShellInfo_ReportsEditionAndVersion()
    {
        RequirePowerShell();

        var info = _tools.PowerShellInfo();

        Assert.That(info, Does.Match("Edition: (Desktop|Core)"));
        Assert.That(info, Does.Match(@"Version: \d+\.\d+"));
    }

    private void RequirePowerShell() =>
        Outside.Tool(_tools.ResolveInterpreter() is not null, "PowerShell is not installed on this machine");
}
