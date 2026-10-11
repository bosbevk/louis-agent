using LouisAgent.Core.Guardrails;

namespace LouisAgent.Core.Tests.Guardrails;

// The workspace boundary every file tool and the API's workspace and git endpoints resolve paths through.
public class WorkspaceBoundaryTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("boundary-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void AnEmptyRoot_IsRejected()
    {
        Assert.That(() => new WorkspaceBoundary(" "), Throws.ArgumentException.With.Message.Contain("cannot be empty"));
    }

    [Test]
    public void ARelativePath_ResolvesInsideTheRoot()
    {
        Assert.That(new WorkspaceBoundary(_root).Resolve("src/app.cs"), Is.EqualTo(Path.Combine(_root, "src", "app.cs")));
    }

    [Test]
    public void AnAbsolutePath_IsRejected()
    {
        Assert.That(() => new WorkspaceBoundary(_root).Resolve(Path.Combine(_root, "x.txt")),
            Throws.ArgumentException.With.Message.Contain("must be relative"));
    }

    [TestCase("..")]
    [TestCase("../outside.txt")]
    [TestCase("src/../../outside.txt")]
    public void APathAboveTheRoot_IsRejected(string path)
    {
        Assert.That(() => new WorkspaceBoundary(_root).Resolve(path), Throws.ArgumentException.With.Message.Contain("inside the workspace"));
    }
}
