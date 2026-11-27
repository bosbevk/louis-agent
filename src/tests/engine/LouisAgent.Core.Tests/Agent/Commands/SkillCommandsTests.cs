using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Testing;

namespace LouisAgent.Core.Tests.Agent.Commands;

/// <summary>
/// F97.1 step 11: what a person types about skills. <c>/skills</c> lists the drafts waiting, each with the call it
/// was proved with, and the approved skills; <c>/approve</c> and <c>/reject</c> reach a draft by its name.
/// </summary>
public class SkillCommandsTests
{
    private string _skills = "";

    /// <summary>A skills folder of its own for each test, with one approved skill and one draft.</summary>
    [SetUp]
    public void MakeTheSkillsFolder()
    {
        _skills = Directory.CreateTempSubdirectory("skill-commands-").FullName;
        Skill("word-count");
        Skill(Path.Combine("drafts", "line-count"));
    }

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>The drafts waiting, each with its example, and the approved skills.</summary>
    [Test]
    public void Skills_ListsTheDrafts_WithTheirExamples_AndTheApproved() =>
        Assert.That(Engine().HandleUserCommand("/skills"),
            Does.Contain("- line-count: Counts.").And.Contain("\"one two\"")
                .And.Contain("Approved skills: word-count"));

    /// <summary>Approving a draft approves it, and its folder moves up.</summary>
    [Test]
    public void Approve_ReachesTheDraft()
    {
        string? reply = Engine().HandleUserCommand("/approve line-count");

        Assert.Multiple(() =>
        {
            Assert.That(reply, Does.Contain("Approved line-count"));
            Assert.That(Directory.Exists(Path.Combine(_skills, "line-count")), Is.True);
        });
    }

    /// <summary>Rejecting a draft deletes it.</summary>
    [Test]
    public void Reject_ReachesTheDraft()
    {
        string? reply = Engine().HandleUserCommand("/reject line-count");

        Assert.Multiple(() =>
        {
            Assert.That(reply, Does.Contain("Rejected line-count"));
            Assert.That(Directory.Exists(Path.Combine(_skills, "drafts", "line-count")), Is.False);
        });
    }

    /// <summary>An engine on this test's skills folder.</summary>
    private AgentEngine Engine()
    {
        var options = new AgentOptions { WorkspaceRoot = _skills, SkillsDirectory = _skills };
        return AgentEngine.CreateBuilder(SkillLoader.Load(options), new FakeChatClient(), options)
            .WithSkillsDirectory(_skills)
            .Build();
    }

    /// <summary>A good skill's folder, with its example.</summary>
    private void Skill(string folder)
    {
        string path = Path.Combine(_skills, folder);
        Directory.CreateDirectory(Path.Combine(path, "scripts"));
        Directory.CreateDirectory(Path.Combine(path, "examples"));
        string skillFile = $"---\nname: {Path.GetFileName(folder)}\ndescription: Counts.\n" +
                           "metadata:\n  script: scripts/run.py\n---\nGive it a text.\n";
        File.WriteAllText(Path.Combine(path, "SKILL.md"), skillFile);
        File.WriteAllText(Path.Combine(path, "scripts", "run.py"), "print(2)");
        File.WriteAllText(Path.Combine(path, "parameters.schema.json"), """{ "type": "object", "properties": {} }""");
        File.WriteAllText(Path.Combine(path, "examples", "example.json"),
            """{ "arguments": { "text": "one two" }, "answer": "2" }""");
    }
}
