using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

// F5 step 4b: the built-in skill files ship inside louis-agent.core, so a host needs no Skills/ folder. CL8 step 4: they
// live in core's Prompts/ and are read only from the assembly, by their real paths; a copy on disk is never read.
public class SkillFileSourceTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("skill-files-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private static string Repo(string path) => File.ReadAllText(Path.Combine(TestPaths.Prompts, path));

    [Test]
    public void EveryBuiltInFile_IsEmbedded_UnderItsOwnPath()
    {
        var builtIn = ToolsetCatalog.All.Select(t => t.GuideFile).OfType<string>()
            .Concat(["shared/principles.md", "agent/louis-agent/voice.md", "agent/louis-agent/voice-orchestrator.md"]);

        Assert.That(SkillFileSource.EmbeddedFiles, Is.SupersetOf(builtIn));
        Assert.That(SkillFileSource.EmbeddedFiles, Has.None.Contains("HelloWorld"), "a test fixture, not a built-in");
    }

    [Test]
    public void AFolder_ListsItsFiles()
    {
        // CL15: the roles here are the tests' own (Prompts/), registered as an identity's are; Puls's live in its identity.
        Assert.That(SkillFileSource.Files("specialists"), Is.EqualTo(new[] { "testprod-specialist.md" }));
        Assert.That(SkillFileSource.Files("minions"), Is.EqualTo(new[] { "testprod-minion-lookup.md", "testprod-minion.md" }), "F30 step 4: a role and the base");
    }

    [Test]
    public void TheEmbeddedText_IsTheFilesText()
    {
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/git.md")!.ReplaceLineEndings("\n"),
            Is.EqualTo(Repo("toolsets/git.md").ReplaceLineEndings("\n")));
    }

    [Test]
    public void ACopyOnDisk_OfABuiltIn_IsIgnored()
    {
        // CL8 step 4: a wording change is a rebuild; a stale copy in a mounted folder can't change what the model reads.
        Directory.CreateDirectory(Path.Combine(_dir, "toolsets"));
        File.WriteAllText(Path.Combine(_dir, "toolsets", "git.md"), "# Git Skills\n\nOur own version.");

        var file = AgentHost.ReadSkillFile(new AgentOptions { SkillsDirectory = _dir }, "toolsets/git.md");

        Assert.That(file!.Value.FromDisk, Is.False);
        Assert.That(file.Value.Text, Does.Not.Contain("Our own version."));
    }

    [Test]
    public void AFunctionTheAgentWrote_IsReadFromDisk()
    {
        // What the agent writes (CreateSkillFile) lives in the skills folder, not the assembly.
        Directory.CreateDirectory(Path.Combine(_dir, "agent", "functions"));
        File.WriteAllText(Path.Combine(_dir, "agent", "functions", "greeting.md"), "# Greeting Skills");

        var file = AgentHost.ReadSkillFile(new AgentOptions { SkillsDirectory = _dir }, "agent/functions/greeting.md");

        Assert.That(file!.Value.FromDisk, Is.True);
        Assert.That(file.Value.Text, Does.Contain("# Greeting Skills"));
    }

    [Test]
    public void ARoleOnDisk_IsNotRead()
    {
        // Only the agent's own files come from disk: a role or a voice is built in, or it isn't one.
        Directory.CreateDirectory(Path.Combine(_dir, "specialists"));
        File.WriteAllText(Path.Combine(_dir, "specialists", "puls-baker.md"), "# Baker");

        Assert.That(AgentHost.ReadSkillFile(new AgentOptions { SkillsDirectory = _dir }, "specialists/puls-baker.md"), Is.Null);
    }

    [Test]
    public void ANameThatIsntEmbedded_IsNull()
    {
        Assert.That(SkillFileSource.ReadEmbedded("no-such.md"), Is.Null);
    }
}