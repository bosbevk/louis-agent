using System.Text.Json;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Skills.Headers.Agent.Drafting;
using LouisAgent.Core.Skills.Headers.Agent.Running;
using LouisAgent.Core.Skills.Languages;

namespace LouisAgent.Core.Tests.Skills.Headers.Agent.Drafting;

/// <summary>
/// F97.1 step 8: a skill the agent makes is kept as a draft only once it is proved: its files read back as a skill, its
/// example fits its parameters, and the example runs. Anything short of that leaves nothing on disk and says what to
/// fix, as a skill library keeps a skill only after a check confirms it works.
/// </summary>
public class SkillDraftingTests
{
    private const string SkillFile =
        "---\nname: word-count\ndescription: Counts words.\nmetadata:\n  script: scripts/count.py\n---\n# Count\n";

    private const string Parameters =
        """{ "type": "object", "properties": { "text": { "type": "string" } }, "required": ["text"] }""";

    private static readonly JsonElement Example = JsonDocument.Parse("""{ "text": "a b" }""").RootElement;

    private string _skills = "";

    /// <summary>A skills folder of its own for each test.</summary>
    [SetUp]
    public void MakeTheSkillsFolder() => _skills = Directory.CreateTempSubdirectory("skill-drafting-").FullName;

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>A proved skill is a draft, its example and answer kept beside it.</summary>
    [Test]
    public void AProvedSkill_IsADraft_WithItsExample()
    {
        var answer = Drafting(Answering("2")).Draft("word-count", SkillFile, Parameters, "print(2)", Example);

        string draft = Path.Combine(_skills, "drafts", "word-count");
        Assert.Multiple(() =>
        {
            Assert.That(answer.Succeeded, Is.True, answer.Text);
            Assert.That(File.ReadAllText(Path.Combine(draft, "scripts", "count.py")), Is.EqualTo("print(2)"));
            Assert.That(File.ReadAllText(Path.Combine(draft, "examples", "example.json")),
                Does.Contain("\"a b\"").And.Contain("\"2\""));
        });
    }

    /// <summary>A skill short of proved is refused with what to fix, and nothing of it is kept.</summary>
    [TestCase("bad name", "Word Count", "word-count")]
    [TestCase("bad file", "word-count", "toolset")]
    [TestCase("example misfits", "word-count", "'text'")]
    [TestCase("example fails", "word-count", "[Exit Code 1]")]
    public void ASkillShortOfProved_IsRefused_AndNothingIsKept(string fault, string name, string named)
    {
        string skillFile = fault == "bad file" ? SkillFile.Replace("metadata:", "toolset: x\nmetadata:") : SkillFile;
        var example = fault == "example misfits" ? JsonDocument.Parse("{}").RootElement : Example;
        var run = fault == "example fails" ? Failing() : Answering("2");

        var answer = Drafting(run).Draft(name, skillFile, Parameters, "print(2)", example);

        Assert.Multiple(() =>
        {
            Assert.That((answer.Succeeded, answer.Text.Contains(named)), Is.EqualTo((false, true)), answer.Text);
            Assert.That(Directory.Exists(Path.Combine(_skills, "drafts", name)), Is.False);
        });
    }

    /// <summary>A name an approved skill has already is refused; that skill is changed another way.</summary>
    [Test]
    public void AnApprovedSkillsName_IsRefused()
    {
        Directory.CreateDirectory(Path.Combine(_skills, "word-count"));

        var answer = Drafting(Answering("2")).Draft("word-count", SkillFile, Parameters, "print(2)", Example);

        Assert.That((answer.Succeeded, answer.Text.Contains("approved")), Is.EqualTo((false, true)), answer.Text);
    }

    /// <summary>Drafting into this test's skills folder, its scripts run by this.</summary>
    private SkillDrafting Drafting(ScriptRun run) => new(_skills, new SkillRunner(run));

    /// <summary>A script run that prints this and ends cleanly.</summary>
    private static ScriptRun Answering(string output) =>
        (ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = null;
            return new ProcessRunner.Result(0, output, false);
        };

    /// <summary>A script run that fails.</summary>
    private static ScriptRun Failing() =>
        (ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = null;
            return new ProcessRunner.Result(1, "boom", false);
        };
}
