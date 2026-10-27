using LouisAgent.Testing;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.Tests.Tools.Shell;

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
}
