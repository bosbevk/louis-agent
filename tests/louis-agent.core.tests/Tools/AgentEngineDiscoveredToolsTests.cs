using louis_agent.core.tools;
using louis_agent.core.config;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

public class AgentEngineDiscoveredToolsTests
{
    [Test]
    public void RegisterDiscoveredTool_WithValidTool_ReturnsTrue()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);

        var result = engine.RegisterDiscoveredTool("rider_build", "Build the project");
        Assert.That(result, Is.True);
    }

    [Test]
    public void RegisterDiscoveredTool_LogsDiscoveredTools()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);

        // Discovered tools are logged for informational purposes
        var result1 = engine.RegisterDiscoveredTool("rider_build", "Build the project");
        var result2 = engine.RegisterDiscoveredTool("rider_test", "Run tests");

        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public void RegisterDiscoveredTool_WhenToolsDisabled_ReturnsFalse()
    {
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var skillProvider = new DefaultSkillsProvider(null);
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: false);

        var result = engine.RegisterDiscoveredTool("rider_build", "Build");
        Assert.That(result, Is.False);
    }

    [Test]
    public void RegisterDiscoveredTool_MultipleToolsCanBeRegistered()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);

        var result1 = engine.RegisterDiscoveredTool("rider_build", "Build");
        var result2 = engine.RegisterDiscoveredTool("rider_test", "Test");
        var result3 = engine.RegisterDiscoveredTool("rider_debug", "Debug");

        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
        Assert.That(result3, Is.True);
    }

    [Test]
    public void SupportsTools_ReflectsConstructorValue()
    {
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var skillProvider = new DefaultSkillsProvider(null);

        var engineWith = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);
        var engineWithout = new AgentEngine(skillProvider, chatClient, options, supportsTools: false);

        Assert.That(engineWith.SupportsTools, Is.True);
        Assert.That(engineWithout.SupportsTools, Is.False);
    }

    private class StubChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => throw new NotImplementedException();
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(ChatResponse)!);
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
