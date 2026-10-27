using LouisAgent.Testing;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.Tests.Tools.Shell;

public class BashToolsTests
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
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    [TestCase("", "")]
    [TestCase("echo hi", "script.sh")]
    public void BashRun_RequiresExactlyOneOfScriptOrPath(string script, string path) =>
        Assert.That(_tools.BashRun(script, path), Does.Contain("exactly one"));

    [Test]
    public void BashRun_RejectsPathsOutsideWorkspace() =>
        Assert.That(_tools.BashRun(scriptPath: "../outside.sh"), Does.Contain("outside the workspace"));
}
