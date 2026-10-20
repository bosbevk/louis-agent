using LouisAgent.AcpServer.Paths;

namespace LouisAgent.AcpServer.Tests.Paths;

// S (CL15): a Windows path from the IDE as the same file inside the container: by ACP_MOUNT_MAPPINGS when one applies,
// else found under a mount root by its last path parts, else the drive letter stripped; never guessed when it isn't there.
public class ContainerPathsTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("acp-paths-").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "proj", "src"));
        File.WriteAllText(Path.Combine(_root, "proj", "src", "Thing.cs"), "class Thing;");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    private static ContainerPaths With(string? mappings, params string[] roots) => new(() => mappings, roots);

    [Test]
    public void AMapping_RewritesThePrefix_IgnoringCase() =>
        Assert.That(With("C:\\Users\\me\\RiderProjects=/repositories").Candidates("c:/users/me/RiderProjects/app/Program.cs"),
            Is.EqualTo(new[] { "/repositories/app/Program.cs" }));

    [Test]
    public void SeveralMappings_EachOneThatApplies() =>
        Assert.That(With("C:/a=/one, C:/b=/two, C:/a/b=/three").Candidates("C:/a/b/x.cs"),
            Is.EqualTo(new[] { "/one/b/x.cs", "/three/x.cs" }));

    [Test]
    public void WithoutAMapping_TheFileIsFoundUnderARoot_ByItsLastParts()
    {
        var candidates = With(null, _root).Candidates("C:/Users/me/proj/src/Thing.cs");

        Assert.That(candidates[0], Is.EqualTo(Path.Combine(_root, "proj", "src", "Thing.cs")));
        Assert.That(candidates[1], Is.EqualTo("/Users/me/proj/src/Thing.cs"), "and the drive letter stripped, as a last try");
    }

    [Test]
    public void AFileWithAnotherFolderAbove_IsNotTheOne() =>
        Assert.That(With(null, _root).Candidates("C:/Users/me/other/lib/Thing.cs"), Is.EqualTo(new[] { "/Users/me/other/lib/Thing.cs" }));

    [Test]
    public void AHostPathThatExistsHere_IsItself()
    {
        string file = Path.Combine(_root, "proj", "src", "Thing.cs");

        Assert.That(With(null).ResolveHostPath(file), Is.EqualTo(file));
        Assert.That(With(null, _root).ResolveHostPath("C:/elsewhere/proj/src/Thing.cs"), Is.EqualTo(file), "else the same file under a root");
        Assert.That(With(null, _root).ResolveHostPath("C:/elsewhere/Nothing.cs"), Is.Null, "never guessed");
    }
}
