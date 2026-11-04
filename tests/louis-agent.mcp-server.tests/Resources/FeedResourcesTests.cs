using System.Net.Mime;
using LouisAgent.McpServer.Resources;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer.Tests.Resources;

/// <summary>
/// The packs' feeds as MCP resources (F56 step 1, ADR 0070): a real client lists them, reads one, and is told no for a
/// feed nobody publishes.
/// </summary>
[TestFixture]
public class FeedResourcesTests
{
    /// <summary>Starts a server whose resources are these feeds, and connects a client.</summary>
    private static Task<InMemoryMcp> ServeAsync(params FakeFeed[] feeds)
    {
        var options = new McpServerOptions { ResourceCollection = [.. FeedResources.From(feeds)] };
        return InMemoryMcp.StartAsync(options);
    }

    [Test]
    public async Task Each_feed_is_listed_by_its_uri_with_its_description_as_json()
    {
        await using var mcp = await ServeAsync(new FakeFeed("rent"), new FakeFeed("fuel"));

        var listed = await mcp.Client.ListResourcesAsync();

        Assert.That(listed.Select(r => (r.Uri, r.Name, r.Description, r.MimeType)), Is.EquivalentTo(new[]
        {
            ("feed://rent", "rent", "The rent feed", MediaTypeNames.Application.Json),
            ("feed://fuel", "fuel", "The fuel feed", MediaTypeNames.Application.Json),
        }));
    }

    [Test]
    public async Task Reading_a_feed_gives_its_json_as_text()
    {
        await using var mcp = await ServeAsync(new FakeFeed("rent", """{"notice":"averages"}"""));

        var read = await mcp.Client.ReadResourceAsync("feed://rent");

        var text = read.Contents.OfType<TextResourceContents>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(text.Text, Is.EqualTo("""{"notice":"averages"}"""));
            Assert.That(text.Uri, Is.EqualTo("feed://rent"));
            Assert.That(text.MimeType, Is.EqualTo(MediaTypeNames.Application.Json));
        });
    }

    [Test]
    public async Task A_feed_nobody_publishes_is_the_protocols_error()
    {
        await using var mcp = await ServeAsync(new FakeFeed("rent"));

        Assert.That(async () => await mcp.Client.ReadResourceAsync("feed://nope"), Throws.InstanceOf<McpException>());
    }

    [Test]
    public void No_feeds_is_no_resources()
    {
        Assert.That(FeedResources.From([]), Is.Empty);
    }
}
