using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Skills.Providers;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer.Tests;

/// <summary>
/// What both transports publish (F56 step 2): the catalogue's feeds as resources beside the engine's tools, set up in
/// one place so neither transport can miss one.
/// </summary>
[TestFixture]
public class McpPrimitivesTests
{
    /// <summary>An engine with core's tools and no model, as the server builds it.</summary>
    private static AgentEngine Engine() =>
        new(new CompositeSkillProvider(), new FakeChatClient(), TestPaths.Agent());

    [Test]
    public async Task The_catalogues_feeds_are_published_as_resources()
    {
        var catalog = new AgentCatalog([], [new FakePack("rentals", toolsets: [], feeds: [new FakeFeed("rent")])]);
        var options = new McpServerOptions();

        McpPrimitives.Add(options, Engine(), catalog);
        await using var mcp = await InMemoryMcp.StartAsync(options);

        var listed = await mcp.Client.ListResourcesAsync();
        Assert.That(listed.Select(r => r.Uri), Is.EqualTo(new[] { "feed://rent" }));
    }

    [Test]
    public async Task The_engines_tools_are_published_beside_them()
    {
        var engine = Engine();
        var options = new McpServerOptions();

        McpPrimitives.Add(options, engine, AgentCatalog.CoreOnly);
        await using var mcp = await InMemoryMcp.StartAsync(options);

        var listed = await mcp.Client.ListToolsAsync();
        Assert.That(listed.Select(t => t.Name),
            Is.SupersetOf(engine.Tools.OfType<Microsoft.Extensions.AI.AIFunction>().Select(f => f.Name)));
    }
}
