using System.Text.Json;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.EngineTools.Skills;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Prompting.Headers.Agent.Drafting;
using LouisAgent.Core.Prompting.Headers.Agent.Running;
using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.Tests.Agent.EngineTools.Skills;

/// <summary>
/// F97.1 step 14a: <c>CreateSkill</c>, the model's tool that makes a skill: it drafts it, proved by its example, and
/// the chat whose turn is running notes the draft as its own, so it can use it at once and no other chat can.
/// </summary>
public class CreateSkillTests
{
    private const string SkillFile =
        "---\nname: word-count\ndescription: Counts words.\nmetadata:\n  script: scripts/count.py\n---\n# Count\n";

    private const string Parameters =
        """{ "type": "object", "properties": { "text": { "type": "string" } }, "required": ["text"] }""";

    private static readonly JsonElement Example = JsonDocument.Parse("""{ "text": "a b" }""").RootElement;

    private string _skills = "";

    /// <summary>A skills folder of its own for each test.</summary>
    [SetUp]
    public void MakeTheSkillsFolder() => _skills = Directory.CreateTempSubdirectory("create-skill-").FullName;

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>A proved skill is drafted, and the chat whose turn is running notes it as its own.</summary>
    [Test]
    public void AProvedSkill_IsDrafted_AsThisChats()
    {
        var chat = new Conversation([]);
        using var turn = TurnTools.Begin([], chat);

        string reply = Tools().CreateSkill("word-count", SkillFile, Parameters, "print(2)", Example);

        Assert.Multiple(() =>
        {
            Assert.That(reply, Does.Contain("word-count is drafted"));
            Assert.That(chat.Drafts, Does.Contain("word-count"));
        });
    }

    /// <summary>A skill that isn't proved is noted nowhere.</summary>
    [Test]
    public void ASkillNotProved_IsNotNoted()
    {
        var chat = new Conversation([]);
        using var turn = TurnTools.Begin([], chat);

        Tools().CreateSkill("Word Count", SkillFile, Parameters, "print(2)", Example);

        Assert.That(chat.Drafts, Is.Empty);
    }

    /// <summary>The tool over this test's skills folder, every script answering 2.</summary>
    private SkillMakingTools Tools() => new(() => new SkillDrafting(_skills, new SkillRunner(
        (ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = null;
            return new ProcessRunner.Result(0, "2", false);
        })));
}
