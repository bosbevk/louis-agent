using louis_agent.core.tools;

namespace louis_agent.core.tests;

public class PythonToolsTests
{
    private string _root = null!;
    private string _venv = null!;
    private PythonTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("python-tools-").FullName;
        _venv = Path.Combine(_root, ".venv-test");
        _tools = new PythonTools(_root, _venv);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    [TestCase("requests")]
    [TestCase("pandas==2.2.2")]
    [TestCase("numpy>=2,<3")]
    [TestCase("uvicorn[standard]")]
    [TestCase("ruamel.yaml~=0.18")]
    public void RequirementSpec_AcceptsPackageSpecifiers(string spec) =>
        Assert.That(PythonTools.RequirementSpec().IsMatch(spec), Is.True);

    [TestCase("--index-url")]
    [TestCase("-r")]
    [TestCase("requests;rm")]
    [TestCase("../evil")]
    [TestCase("https://example.com/pkg.whl")]
    public void RequirementSpec_RejectsOptionsAndUrls(string spec) =>
        Assert.That(PythonTools.RequirementSpec().IsMatch(spec), Is.False);

    [Test]
    public void PythonInstallPackages_RejectsPipOptions()
    {
        Assert.That(_tools.PythonInstallPackages("requests --index-url http://evil"), Does.Contain("not a valid package specifier"));
    }

    [TestCase("", "")]
    [TestCase("print(1)", "script.py")]
    public void PythonRun_RequiresExactlyOneOfCodeOrScript(string code, string script)
    {
        Assert.That(_tools.PythonRun(code, script), Does.Contain("exactly one"));
    }

    [Test]
    public void TryResolveWorkspacePath_RejectsPathsOutsideWorkspace()
    {
        Assert.That(_tools.TryResolveWorkspacePath("../outside.py", out _, out var error), Is.False);
        Assert.That(error, Does.Contain("outside the workspace"));
    }

    [Test]
    [Category("Integration")]
    public void PythonRun_InlineCode_ReturnsOutputArgsAndStdin()
    {
        RequirePython();

        var result = _tools.PythonRun(
            code: "import sys\nprint('hello', sys.argv[1], sys.stdin.read().strip())",
            arguments: "\"big world\"",
            standardInput: "from stdin");

        Assert.That(result, Does.Contain("Exit code: 0"));
        Assert.That(result, Does.Contain("hello big world from stdin"));
    }

    [Test]
    [Category("Integration")]
    public void PythonRun_ReportsTracebackWithSnippetName()
    {
        RequirePython();

        var result = _tools.PythonRun(code: "def f():\n    raise ValueError('boom')\nf()");

        Assert.That(result, Does.Contain("Exit code: 1"));
        Assert.That(result, Does.Contain("ValueError: boom"));
        Assert.That(result, Does.Contain("<snippet>"));
        Assert.That(result, Does.Not.Contain("snippet_"), "temp file path should be hidden");
    }

    [Test]
    [Category("Integration")]
    public void PythonRun_ScriptPath_RunsFromWorkspaceRoot()
    {
        RequirePython();
        Directory.CreateDirectory(Path.Combine(_root, "scripts"));
        File.WriteAllText(Path.Combine(_root, "data.txt"), "42");
        File.WriteAllText(Path.Combine(_root, "scripts", "read.py"), "print(open('data.txt').read())");

        Assert.That(_tools.PythonRun(scriptPath: "scripts/read.py"), Does.Contain("42"));
    }

    [Test]
    [Category("Integration")]
    public void PythonRun_KillsProgramAfterTimeout()
    {
        RequirePython();

        var result = _tools.PythonRun(code: "import time\nwhile True: time.sleep(1)", timeoutSeconds: 2);

        Assert.That(result, Does.Contain("was killed"));
    }

    [Test]
    [Explicit("Creates a virtual environment and downloads a package from PyPI")]
    public void PythonInstallPackages_InstallsIntoVenvAndRunUsesIt()
    {
        RequirePython();

        Assert.That(_tools.PythonInstallPackages("six"), Does.Not.Contain("FAILED"));
        Assert.That(_tools.PythonRun(code: "import six, sys\nprint(sys.prefix)"), Does.Contain(".venv-test"));
    }

    private void RequirePython() =>
        Assume.That(_tools.ResolveInterpreter(), Is.Not.Null, "Python 3 is not installed on this machine");
}
