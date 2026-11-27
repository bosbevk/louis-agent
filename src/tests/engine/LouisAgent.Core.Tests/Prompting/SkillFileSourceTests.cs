using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Roles;

namespace LouisAgent.Core.Tests.Prompting;

/// <summary>
/// The built-in prompts ship inside the engine's assembly, so a host needs no skills folder: they are read from the
/// assembly by their real paths, and a copy on disk is never read. Only what the agent writes itself comes from disk.
/// </summary>
public class SkillFileSourceTests
{
    private string _dir = null!;

    /// <summary>A skills folder of this test's own.</summary>
    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("skill-files-").FullName;

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    /// <summary>A prompt's text as the repository holds it.</summary>
    private static string Repo(string path) => File.ReadAllText(Path.Combine(TestPaths.Prompts, path));

    /// <summary>
    /// A project's prompts are read from its root (CL68.1), where the embedded provider also lists its own manifest;
    /// only markdown is a prompt (Louis's pick of three), so the manifest is never a prompt path.
    /// </summary>
    [Test]
    public void AProjectsProvider_ListsOnlyItsPrompts()
    {
        var files = SkillFileSource.FilesOf(EmbeddedPrompts.Of(typeof(SkillFileSource).Assembly)).ToList();

        Assert.That(files, Is.Not.Empty);
        Assert.That(files, Has.All.EndsWith(".md"));
    }

    /// <summary>
    /// An eval file embedded beside an agent's prompts (F126.3.1) reaches no prompt: the list of prompts is what it
    /// was without it.
    /// </summary>
    [Test]
    public void AnEvalFileBesideTheAgentsPrompts_IsNoPrompt()
    {
        var prompts = new Dictionary<string, string>
        {
            ["agents/a-guide/voice.md"] = "# A guide\n",
            ["agents/a-guide/skills/pages/SKILL.md"] = "# Pages\n"
        };
        var withEval = new Dictionary<string, string>(prompts)
        {
            ["agents/a-guide/guide.eval.json"] = """{"agent":"a-guide","conversations":[]}"""
        };

        var listed = SkillFileSource.FilesOf(new FakePrompts(withEval)).ToList();

        Assert.That(listed, Is.EqualTo(SkillFileSource.FilesOf(new FakePrompts(prompts)).ToList()));
    }

    /// <summary>Every toolset's guide and the principles are embedded, and no test fixture is.</summary>
    [Test]
    public void EveryBuiltInFile_IsEmbedded_UnderItsOwnPath()
    {
        var builtIn = ToolsetCatalog.All.Select(t => t.Skill).OfType<string>().Select(PackToolset.SkillFile)
            .Concat(["shared/principles.md"]);

        Assert.That(SkillFileSource.EmbeddedFiles, Is.SupersetOf(builtIn));
        Assert.That(SkillFileSource.EmbeddedFiles, Has.None.Contains("SampleSkills"), "a test fixture, not a built-in");
    }

    /// <summary>A folder lists the files directly in it, a subagent's and a lookup agent's alike.</summary>
    [Test]
    public void AFolder_ListsItsFiles()
    {
        // The roles here are the tests' own (their agents/), registered as an identity's are.
        Assert.That(SkillFileSource.Files("agents/testprod/subagents/researcher"), Is.EqualTo(new[] { "voice.md" }));
        Assert.That(SkillFileSource.Files($"agents/testprod/{PromptFolder.Lookups.Wire()}/lookup"),
            Is.EqualTo(new[] { "voice.md" }), "CL68.3: a lookup agent inside its agent");
    }

    /// <summary>What is embedded is the file's own text.</summary>
    [Test]
    public void TheEmbeddedText_IsTheFilesText()
    {
        string skills = PackToolset.SkillFile(CoreToolset.Skills.Wire());
        Assert.That(SkillFileSource.ReadEmbedded(skills)!.ReplaceLineEndings("\n"),
            Is.EqualTo(Repo(skills).ReplaceLineEndings("\n")));
    }

    /// <summary>A copy of a built-in prompt on disk is never read in its place.</summary>
    [Test]
    public void ACopyOnDisk_OfABuiltIn_IsIgnored()
    {
        // A wording change is a rebuild, so a stale copy in a mounted folder can't change what the model reads.
        string agents = PackToolset.SkillFile(CoreToolset.Agents.Wire());
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_dir, agents))!);
        File.WriteAllText(Path.Combine(_dir, agents), "# Agents Skills\n\nOur own version.");

        string? text = SkillFolder.Read(new AgentOptions { SkillsDirectory = _dir }, agents);

        Assert.That(text, Is.Not.Null.And.Not.Contain("Our own version."));
    }

    /// <summary>
    /// F97.1 step 14d: a file in the old functions folder isn't read either. Nothing of the prompt comes from disk,
    /// since the skills the agent keeps are folders the door reads.
    /// </summary>
    [Test]
    public void AFileInTheOldFunctionsFolder_IsNotRead()
    {
        // The old place, written out: no type names it any more.
        string folder = Path.Combine(_dir, PromptFolder.Agents.Wire(), "functions");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "greeting.md"), "# Greeting Skills");

        Assert.That(SkillFolder.Read(new AgentOptions { SkillsDirectory = _dir }, "agents/functions/greeting.md"),
            Is.Null);
    }

    /// <summary>A subagent's file on disk is never read: a role is built in, or it isn't one.</summary>
    [Test]
    public void ARoleOnDisk_IsNotRead()
    {
        // A subagent or a voice is built in, or it isn't one.
        string baker = RoleCatalog.FileName("acme.baker", RoleLevel.Subagent);
        Directory.CreateDirectory(Path.Combine(_dir, Path.GetDirectoryName(baker)!));
        File.WriteAllText(Path.Combine(_dir, baker), "# Baker");

        Assert.That(SkillFolder.Read(new AgentOptions { SkillsDirectory = _dir }, baker), Is.Null);
    }

    /// <summary>A prompt that isn't embedded is none.</summary>
    [Test]
    public void ANameThatIsntEmbedded_IsNull()
    {
        Assert.That(SkillFileSource.ReadEmbedded("no-such.md"), Is.Null);
    }
}
