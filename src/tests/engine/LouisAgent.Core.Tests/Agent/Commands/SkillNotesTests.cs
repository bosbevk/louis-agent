using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Commands;

/// <summary>
/// F97.1 step 12: the host answers <c>/approve</c> and <c>/reject</c> itself, so the model would never hear of them;
/// the chat they were typed in tells it on the person's next message, in a <c>system-reminder</c> before their words,
/// as Claude Code tells its model what changed outside its view. Once, and in that chat only; nothing already sent
/// changes.
/// </summary>
public class SkillNotesTests
{
    private string _skills = "";

    /// <summary>A skills folder of its own for each test, with one draft.</summary>
    [SetUp]
    public void MakeTheSkillsFolder()
    {
        _skills = Directory.CreateTempSubdirectory("skill-notes-").FullName;
        string draft = Path.Combine(_skills, "drafts", "line-count");
        Directory.CreateDirectory(Path.Combine(draft, "scripts"));
        File.WriteAllText(Path.Combine(draft, "SKILL.md"),
            "---\nname: line-count\ndescription: Counts lines.\nmetadata:\n  script: scripts/run.py\n---\n# Count\n");
        File.WriteAllText(Path.Combine(draft, "scripts", "run.py"), "print(2)");
        File.WriteAllText(Path.Combine(draft, "parameters.schema.json"), """{ "type": "object", "properties": {} }""");
    }

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>
    /// The chat /approve was typed in tells the model on the next message, before the person's words.
    /// </summary>
    [Test]
    public async Task AnApproval_IsNoted_OnThatChatsNextMessage_Once()
    {
        var model = new FakeChatClient(_ => Say("ok"), _ => Say("ok"));
        var engine = Engine(model);
        var chat = engine.StartConversation();

        engine.HandleUserCommand("/approve line-count", chat: chat);
        await engine.ProcessPromptAsync(chat, "count my lines");
        await engine.ProcessPromptAsync(chat, "thanks");

        string first = LastUserText(model, 0), second = LastUserText(model, 1);
        Assert.Multiple(() =>
        {
            Assert.That(first,
                Does.StartWith("<system-reminder>").And.Contain("line-count").And.EndWith("count my lines"));
            Assert.That(second, Is.EqualTo("thanks"));
        });
    }

    /// <summary>Another chat isn't told.</summary>
    [Test]
    public async Task AnotherChat_IsNotTold()
    {
        var model = new FakeChatClient(_ => Say("ok"));
        var engine = Engine(model);
        var typedIn = engine.StartConversation();
        var other = engine.StartConversation();

        engine.HandleUserCommand("/reject line-count", chat: typedIn);
        await engine.ProcessPromptAsync(other, "hello");

        Assert.That(LastUserText(model, 0), Is.EqualTo("hello"));
    }

    /// <summary>The text of the last user message a request carried.</summary>
    private static string LastUserText(FakeChatClient model, int request) =>
        model.Requests[request].Messages.Last(m => m.Role == ChatRole.User).Text;

    /// <summary>An engine on this test's skills folder.</summary>
    private AgentEngine Engine(IChatClient model)
    {
        var options = new AgentOptions { WorkspaceRoot = _skills, SkillsDirectory = _skills };
        return AgentEngine.CreateBuilder(SkillLoader.Load(options), model, options)
            .WithSkillsDirectory(_skills)
            .Build();
    }

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
