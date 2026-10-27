using LouisAgent.Testing;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.IntegrationTests.Tools.Shell;

[Category("Integration")]
public class BashToolsIntegrationTests
{
    private string _root = null!;

    private BashTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("bash-tools-").FullName;
        _tools = new BashTools(_root);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Test]
    public void FindBash_NeverReturnsTheWslStub()
    {
        var bash = BashTools.FindBash();
        Outside.Tool(bash is not null, "bash is not installed");

        Assert.That(bash, Does.Not.Contain("System32").IgnoreCase);
    }

    [Test]
    public void BashRun_PassesArgumentsAsPositionalParametersNotCode()
    {
        RequireBash();

        var result = _tools.BashRun(
            script: "printf '%s|' \"$@\"; echo; echo \"count=$#\"",
            arguments: "one \"two words\" '$(touch hacked)'");

        Assert.That(result, Does.Contain("Exit code: 0"));
        Assert.That(result, Does.Contain("one|two words|'$(touch|hacked)'|"));
        Assert.That(File.Exists(Path.Combine(_root, "hacked")), Is.False, "arguments must never be executed");
    }

    [Test]
    public void BashRun_DoesNotExpandWildcardArguments()
    {
        RequireBash();
        File.WriteAllText(Path.Combine(_root, "a.txt"), "");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "");

        Assert.That(_tools.BashRun(script: "echo \"[$1] $#\"", arguments: "*.txt"), Does.Contain("[*.txt] 1"));
    }

    [Test]
    public void BashRun_ErrorsReportTheScriptLine()
    {
        RequireBash();

        var result = _tools.BashRun(script: "echo first\nno_such_command_xyz\necho never");

        Assert.That(result, Does.Contain("<script>: line 2"));
        Assert.That(result, Does.Contain("Exit code: 127"));
        Assert.That(result, Does.Not.Contain("never"));
    }

    [Test]
    public void BashRun_RunsFromWorkspaceRootWithStdin()
    {
        RequireBash();
        File.WriteAllText(Path.Combine(_root, "data.txt"), "42");

        var result = _tools.BashRun(script: "cat data.txt; echo; tr a-z A-Z", standardInput: "from stdin");

        Assert.That(result, Does.Contain("42"));
        Assert.That(result, Does.Contain("FROM STDIN"));
    }

    [Test]
    public void BashRun_StrictModeStopsAtFirstFailure()
    {
        RequireBash();
        const string script = "false\necho should-not-print";

        var strict = _tools.BashRun(script: script);
        var lenient = _tools.BashRun(script: script, strict: false);

        Assert.That(strict, Does.Contain("Exit code: 1"));
        Assert.That(strict, Does.Not.Contain("should-not-print"));
        Assert.That(lenient, Does.Contain("should-not-print"));
    }

    [Test]
    public void BashRun_StrictModeRejectsUnsetVariables()
    {
        RequireBash();
        Assert.That(_tools.BashRun(script: "echo \"$UNDEFINED_THING\""), Does.Contain("unbound variable"));
    }

    [Test]
    public void BashRun_ScriptPathWithCrlfLineEndings_ExplainsTheProblem()
    {
        RequireBash();
        Assume.That(OperatingSystem.IsWindows(), Is.False, "Git Bash tolerates CRLF; only Linux bash fails on it");
        File.WriteAllText(Path.Combine(_root, "win.sh"), "x=1\r\nif [ \"$x\" = 1 ]; then\r\n  echo ok\r\nfi\r\n");

        var result = _tools.BashRun(scriptPath: "win.sh", strict: false);

        Assert.That(result, Does.Contain("CRLF"));
    }

    [Test]
    public void BashRun_InlineCrlfScriptIsNormalised()
    {
        RequireBash();
        Assert.That(_tools.BashRun(script: "echo one\r\necho two\r\n"),
            Does.Contain("one\ntwo").Or.Contain("one\r\ntwo"));
    }

    [Test]
    public void BashRun_KillsScriptAfterTimeout()
    {
        RequireBash();
        Assert.That(_tools.BashRun(script: "while true; do sleep 1; done", timeoutSeconds: 2),
            Does.Contain("was killed"));
    }

    [Test]
    public void BashInfo_ReportsVersionAndCommands()
    {
        RequireBash();

        var info = _tools.BashInfo();

        Assert.That(info, Does.Match(@"Version: \d+\.\d+"));
        Assert.That(info, Does.Match("git: (available|missing)"));
    }

    private static void RequireBash() => Outside.Tool(BashTools.FindBash() is not null, "bash is not installed");
}
