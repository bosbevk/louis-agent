using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// After 10 tool rounds FunctionInvokingChatClient sends one last request without tools. Claude rejects a changed tool
/// list once its thinking blocks are in the history, so the engine puts the tools back with tool mode None.
/// </summary>
public class AgentEngineRoundLimitTests
{
    /// <summary>An engine on the model given, with or without tools.</summary>
    private static AgentEngine Engine(IChatClient client, bool supportsTools = true) =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), client, TestPaths.Agent(),
            supportsTools);

    /// <summary>A model that asks for a tool on every request, so the loop always runs into its limit.</summary>
    private static FakeChatClient NeverStopsCallingTools() => new(Enumerable.Range(1, 15)
        .Select<int, Func<IList<ChatMessage>, ChatResponse>>(i => _ => new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            [
                new FunctionCallContent($"call_{i}", "ExecuteSkill",
                    new Dictionary<string, object?>
                    {
                        ["skillName"] = "SayHello",
                        ["jsonArgs"] = "{\"name\":\"x\"}"
                    }),
            ])))
        .ToArray());

    /// <summary>Streamed past the limit, the last request still sends the tools, with tool mode None.</summary>
    [Test]
    public async Task StreamPrompt_PastTheRoundLimit_FinalRequestKeepsTheToolsWithModeNone()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.StartConversation(), "go"))
        {
        }

        Assert.That(client.Requests, Has.Count.EqualTo(11), "10 rounds, then the final request");
        ChatOptions final = client.Requests[^1].Options!;
        Assert.That(final.Tools!.Select(t => t.Name), Is.EqualTo(engine.Tools.Select(t => t.Name)),
            "the same tool list");
        Assert.That(final.ToolMode, Is.EqualTo(ChatToolMode.None), "but no tool can be called");
    }

    /// <summary>Answered whole, the same.</summary>
    [Test]
    public async Task ProcessPrompt_PastTheRoundLimit_FinalRequestKeepsTheToolsWithModeNone()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await engine.ProcessPromptAsync(engine.StartConversation(), "go");

        Assert.That(client.Requests[^1].Options!.Tools, Has.Count.EqualTo(engine.Tools.Count));
        Assert.That(client.Requests[^1].Options!.ToolMode, Is.EqualTo(ChatToolMode.None));
    }

    /// <summary>The rounds before the limit go out as they always did.</summary>
    [Test]
    public async Task StreamPrompt_RoundsBeforeTheLimit_AreUnchanged()
    {
        var client = NeverStopsCallingTools();
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.StartConversation(), "go"))
        {
        }

        Assert.That(client.Requests.Take(10).Select(r => r.Options!.ToolMode), Is.All.EqualTo(ChatToolMode.Auto));
    }

    /// <summary>For a model without tools there is nothing to put back.</summary>
    [Test]
    public void KeepToolsOnFinalRequest_ModelWithoutToolSupport_LeavesTheRequestAlone()
    {
        var engine = Engine(new FakeChatClient(), supportsTools: false);
        var options = new ChatOptions { MaxOutputTokens = 100 };

        Assert.That(engine.Guards.KeepToolsOnFinalRequest(options), Is.SameAs(options));
    }

    /// <summary>The tools go back on a copy of the options, never on the caller's own.</summary>
    [Test]
    public void KeepToolsOnFinalRequest_DoesNotChangeTheCallersOptions()
    {
        var engine = Engine(new FakeChatClient());
        var options = new ChatOptions { MaxOutputTokens = 100 };

        ChatOptions? final = engine.Guards.KeepToolsOnFinalRequest(options);

        Assert.That(final, Is.Not.SameAs(options));
        Assert.That(options.Tools, Is.Null);
        Assert.That(final!.MaxOutputTokens, Is.EqualTo(100), "everything else is kept");
    }
}
