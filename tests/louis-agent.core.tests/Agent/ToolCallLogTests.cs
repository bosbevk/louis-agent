using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Skills;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

// CL3 step 6: the [TOOL] lines people watch in Rider and the logs read as they did before ILogger.
[NonParallelizable]
public class ToolCallLogTests
{
    private sealed class PingTools
    {
        [System.ComponentModel.Description("Replies pong")]
        public string Ping(string to) => $"pong to {to}";
    }

    [Test]
    public async Task AToolCall_IsLoggedOnStderr_AsTool_ArrowIn_ArrowOut()
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            AgentLogging.Configure(null);
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("call_1", "Ping", new Dictionary<string, object?> { ["to"] = "Louis" })])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            var skills = new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md"));
            var engine = new AgentEngine(skills, client, TestPaths.Agent(), toolsets: [new PingTools()]);

            await engine.ProcessPromptAsync(engine.NewHistory(), "ping Louis");
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(), Does.Contain("[TOOL] -> Ping({\"to\":\"Louis\"})"));
        Assert.That(stderr.ToString(), Does.Contain("[TOOL] <- Ping: pong to Louis"));
    }
}