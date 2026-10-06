using louis_agent.core.mcp;

namespace louis_agent.core.tests;

public class RiderMcpClientTests
{
    [Test]
    public void Constructor_WithValidEndpoint_Initializes()
    {
        using var client = new RiderMcpClient("http://127.0.0.1:64342");
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithDefaultEndpoint_Initializes()
    {
        using var client = new RiderMcpClient();
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public async Task DiscoverToolsAsync_WhenRiderUnavailable_ReturnsEmptyList()
    {
        using var client = new RiderMcpClient("http://localhost:99999");
        var tools = await client.DiscoverToolsAsync();
        Assert.That(tools, Is.Empty);
    }

    [Test]
    public async Task DiscoverToolsAsync_DoesNotThrowOnFailure()
    {
        using var client = new RiderMcpClient("http://localhost:99999");
        Assert.DoesNotThrowAsync(async () =>
        {
            await client.DiscoverToolsAsync();
        });
    }

    [Test]
    public async Task DiscoverToolsAsync_WithCancellationToken_Completes()
    {
        using var client = new RiderMcpClient("http://localhost:99999");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var tools = await client.DiscoverToolsAsync(cts.Token);
        Assert.That(tools, Is.TypeOf<List<DiscoveredTool>>());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var client = new RiderMcpClient();
        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
            client.Dispose();
        });
    }

    [Test]
    public void DiscoveredTool_Record_HasCorrectProperties()
    {
        var tool = new DiscoveredTool("test", "description", default);
        Assert.That(tool.Name, Is.EqualTo("test"));
        Assert.That(tool.Description, Is.EqualTo("description"));
    }
}
