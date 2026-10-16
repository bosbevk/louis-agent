using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

// F5 step 4b: the built-in skill files ship inside louis-agent.core, so a host needs no Skills/ folder; a file of the
// same name on disk overrides one, editable without a rebuild.
public class SkillFileSourceTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("skill-files-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private static string Repo(string fileName) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "Skills", fileName));

    /// <summary>A file in a subfolder (toolsets/git.md) is embedded as toolsets.git.md.</summary>
    private static string Embedded(string fileName) => fileName.Replace('/', '.');

    [Test]
    public void EveryBuiltInFile_IsEmbedded()
    {
        var builtIn = ToolsetCatalog.All.Select(t => t.GuideFile).OfType<string>()
            .Concat(["agent/personality.md", "agent/default.md", "code-review-skills.md", "time-logging-skills.md"]);

        Assert.That(SkillFileSource.EmbeddedNames, Is.SupersetOf(builtIn.Select(Embedded)));
        Assert.That(SkillFileSource.EmbeddedNames, Does.Not.Contain("HelloWorld.md"), "a test fixture, not a built-in");
    }

    [Test]
    public void WithNothingOnDisk_TheEmbeddedCopyIsUsed()
    {
        var file = SkillFileSource.Read("toolsets/git.md", [Path.Combine(_dir, "git.md")]);

        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Value.FromDisk, Is.False);
        Assert.That(file.Value.Text.ReplaceLineEndings("\n"), Is.EqualTo(Repo("toolsets/git.md").ReplaceLineEndings("\n")));
    }

    [Test]
    public void AFileOnDisk_Overrides()
    {
        string path = Path.Combine(_dir, "git.md");
        File.WriteAllText(path, "# Git Skills\n\nOur own version.");

        var file = SkillFileSource.Read("toolsets/git.md", [path]);

        Assert.That(file!.Value.FromDisk, Is.True);
        Assert.That(file.Value.Text, Does.Contain("Our own version."));
    }

    [Test]
    public void ANameNeitherOnDiskNorEmbedded_IsNull()
    {
        Assert.That(SkillFileSource.Read("no-such-skills.md", [Path.Combine(_dir, "no-such-skills.md")]), Is.Null);
    }

    [Test]
    public void AnEmbeddedFunctionFile_KeepsItsProcedures()
    {
        // Parsing from text gives the same skills as parsing the file.
        var fromText = MarkdownSkillProvider.FromText(SkillFileSource.ReadEmbedded("code-review-skills.md")!);

        Assert.That(fromText.Skills.Select(s => s.Name), Does.Contain("AnalyzeChanges"));
        Assert.That(fromText.Skills.Select(s => s.Name),
            Is.EqualTo(MarkdownSkillLoader.Load(Path.Combine(TestPaths.RepoRoot, "Skills", "code-review-skills.md")).Select(s => s.Name)));
    }
}