using louis_agent.core.providers;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// After 10 tool rounds FunctionInvokingChatClient sends one last request without tools. Claude Haiku 5.5 rejects a
// changed tool list once its thinking blocks are in the history, so the engine puts the tools back with tool mode None.
public class AgentEngineRoundLimitTests
{
    private static AgentEngine Engine(IChatClient client, bool supportsTools = true) =>
        new(new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client, TestPaths.Agent(),
            supportsTools);

    /// <summary>A model that asks for a tool on every request, so the loop always runs into its limit.</summary>
    private static FakeChatClient NeverStopsCallingTools() => new(Enumerable.Range(1, 15)
        .Select<int, Func<IList<ChatMessage>, ChatResponse>>(i => _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent($"call_{i}", "ExecuteSkill",
                new Dictionary<string, object?> { ["skillName"] = "SayHello", ["jsonArgs"] = "{\"name\":\"x\"}" }),
        ])))
        .ToArray());

    [Test]
    public async Task StreamPrompt_PastTheRoundLimit_FinalRequestKeepsTheToolsWithModeNone()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), "go")) { }

        Assert.That(client.Requests, Has.Count.EqualTo(11), "10 rounds, then the final request");
        ChatOptions final = client.Requests[^1].Options!;
        Assert.That(final.Tools!.Select(t => t.Name), Is.EqualTo(engine.Tools.Select(t => t.Name)), "the same tool list");
        Assert.That(final.ToolMode, Is.EqualTo(ChatToolMode.None), "but no tool can be called");
    }

    [Test]
    public async Task ProcessPrompt_PastTheRoundLimit_FinalRequestKeepsTheToolsWithModeNone()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await engine.ProcessPromptAsync(engine.NewHistory(), "go");

        Assert.That(client.Requests[^1].Options!.Tools, Has.Count.EqualTo(engine.Tools.Count));
        Assert.That(client.Requests[^1].Options!.ToolMode, Is.EqualTo(ChatToolMode.None));
    }

    [Test]
    public async Task StreamPrompt_RoundsBeforeTheLimit_AreUnchanged()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), "go")) { }

        Assert.That(client.Requests.Take(10).Select(r => r.Options!.ToolMode), Is.All.EqualTo(ChatToolMode.Auto));
    }

    [Test]
    public void KeepToolsOnFinalRequest_ModelWithoutToolSupport_LeavesTheRequestAlone()
    {
        var engine = Engine(new FakeChatClient(), supportsTools: false);
        var options = new ChatOptions { MaxOutputTokens = 100 };

        Assert.That(engine.KeepToolsOnFinalRequest(options), Is.SameAs(options));
    }

    [Test]
    public void KeepToolsOnFinalRequest_DoesNotChangeTheCallersOptions()
    {
        var engine = Engine(new FakeChatClient());
        var options = new ChatOptions { MaxOutputTokens = 100 };

        ChatOptions? final = engine.KeepToolsOnFinalRequest(options);

        Assert.That(final, Is.Not.SameAs(options));
        Assert.That(options.Tools, Is.Null);
        Assert.That(final!.MaxOutputTokens, Is.EqualTo(100), "everything else is kept");
    }
}