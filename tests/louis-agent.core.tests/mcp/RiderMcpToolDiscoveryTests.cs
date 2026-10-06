using louis_agent.core.mcp;
using louis_agent.core.tools;
using louis_agent.core.config;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

public class RiderMcpToolDiscoveryTests
{
    [Test]
    public void Constructor_WithAgentEngine_Initializes()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);

        var discovery = new RiderMcpToolDiscovery(engine);
        Assert.That(discovery, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithCustomEndpoint_Initializes()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);

        var discovery = new RiderMcpToolDiscovery(engine, "http://custom:1234");
        Assert.That(discovery, Is.Not.Null);
    }

    [Test]
    public async Task DiscoverAndRegisterAsync_WhenRiderUnavailable_DoesNotThrow()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);
        var discovery = new RiderMcpToolDiscovery(engine, "http://localhost:99999");

        Assert.DoesNotThrowAsync(async () =>
        {
            await discovery.DiscoverAndRegisterAsync(throwOnError: false);
        });
    }

    [Test]
    public async Task DiscoverAndRegisterAsync_IsAsync()
    {
        var skillProvider = new DefaultSkillsProvider(null);
        var chatClient = new StubChatClient();
        var options = new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory };
        var engine = new AgentEngine(skillProvider, chatClient, options, supportsTools: true);
        var discovery = new RiderMcpToolDiscovery(engine, "http://localhost:99999");

        var task = discovery.DiscoverAndRegisterAsync();
        var completedInTime = await Task.WhenAny(task, Task.Delay(5000)) == task;
        Assert.That(completedInTime, Is.True);
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
