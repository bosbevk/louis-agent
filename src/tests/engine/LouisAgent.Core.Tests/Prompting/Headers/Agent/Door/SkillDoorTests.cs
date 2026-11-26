using System.Text.Json;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Prompting.Headers.Agent.Door;
using LouisAgent.Core.Prompting.Headers.Agent.Library;
using LouisAgent.Core.Prompting.Headers.Agent.Running;
using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.Tests.Prompting.Headers.Agent.Door;

/// <summary>
/// F97.1 step 10a: the one door to every skill. With a name alone it reads the skill, as the Agent Skills spec loads a
/// skill's body only when it is needed; with arguments it checks them, then runs the skill. A call that can't run says
/// what to fix, and a name it can't find lists what this chat may use.
/// </summary>
public class SkillDoorTests
{
    private const string Parameters =
        """{ "type": "object", "properties": { "text": { "type": "string" } }, "required": ["text"] }""";

    private static readonly IReadOnlySet<string> NoDrafts = new HashSet<string>();

    private string _skills = "";

    /// <summary>A skills folder of its own for each test, with one approved skill.</summary>
    [SetUp]
    public void MakeTheSkillsFolder()
    {
        _skills = Directory.CreateTempSubdirectory("skill-door-").FullName;
        Skill("word-count");
    }

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>A name alone reads the skill: what it does, how to use it, its parameters and its example.</summary>
    [Test]
    public void ANameAlone_ReadsTheSkill()
    {
        string read = Door().Use("word-count", null, NoDrafts);

        Assert.That(read, Does.Contain("Counts words.").And.Contain("Give it a text.").And.Contain("\"required\"")
            .And.Contain("\"one two\""));
    }

    /// <summary>A good call runs the skill and answers what it printed.</summary>
    [Test]
    public void AGoodCall_RunsTheSkill() =>
        Assert.That(Door().Use("word-count", Json("""{ "text": "a b" }"""), NoDrafts), Is.EqualTo("2"));

    /// <summary>A call that doesn't fit says each problem and gives the parameters, and nothing runs.</summary>
    [Test]
    public void ACallThatDoesntFit_SaysWhy_AndGivesTheParameters()
    {
        string answer = Door().Use("word-count", Json("{}"), NoDrafts);

        Assert.That(answer, Does.Contain("'text'").And.Contain("\"properties\"").And.Not.EqualTo("2"));
    }

    /// <summary>A name the chat can't use lists the ones it can: the approved skills and its own drafts.</summary>
    [Test]
    public void AnUnknownName_ListsWhatThisChatMayUse()
    {
        Skill(Path.Combine("drafts", "line-count"));

        string answer = Door().Use("letter-count", null, new HashSet<string> { "line-count" });

        Assert.That(answer, Does.Contain("letter-count").And.Contain("word-count").And.Contain("line-count"));
    }

    /// <summary>
    /// F97.1 step 16: a name alone reads core's built-in on-demand skill too, the standard a skill is written in,
    /// which no skills folder holds.
    /// </summary>
    [Test]
    public void TheBuiltInWriteASkill_IsReadByItsName() =>
        Assert.That(Door().Use("write-a-skill", null, NoDrafts),
            Does.Contain("reads its arguments as one JSON object from stdin"));

    /// <summary>The door over this test's skills, every script answering 2.</summary>
    private SkillDoor Door() => new(SkillLibrary.Read(_skills), new SkillRunner(
        (ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = null;
            return new ProcessRunner.Result(0, "2", false);
        }));

    /// <summary>A good skill's folder, with its example.</summary>
    private void Skill(string folder)
    {
        string path = Path.Combine(_skills, folder);
        string name = Path.GetFileName(folder);
        Directory.CreateDirectory(Path.Combine(path, "scripts"));
        Directory.CreateDirectory(Path.Combine(path, "examples"));
        string skillFile = $"---\nname: {name}\ndescription: Counts words.\n" +
                           "metadata:\n  script: scripts/run.py\n---\nGive it a text.\n";
        File.WriteAllText(Path.Combine(path, "SKILL.md"), skillFile);
        File.WriteAllText(Path.Combine(path, "scripts", "run.py"), "print(2)");
        File.WriteAllText(Path.Combine(path, "parameters.schema.json"), Parameters);
        File.WriteAllText(Path.Combine(path, "examples", "example.json"),
            """{ "arguments": { "text": "one two" }, "answer": "2" }""");
    }

    /// <summary>A call's arguments as the model sends them.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;
}
