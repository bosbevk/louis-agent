using System.Net.Mime;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer.Resources;

/// <summary>
/// Turns the packs' feeds into MCP resources: one per feed, read by <c>feed://{name}</c>, described as the feed
/// describes itself, its JSON handed over as text. The API serves the same feeds at <c>/v1/feeds/{name}</c>.
/// </summary>
/// <remarks>
/// Adapter (GoF): a feed already is a name, a description and its text (ADR 0070), so each becomes the SDK's own
/// <see cref="McpServerResource"/> and the SDK answers <c>resources/list</c> and <c>resources/read</c>, matching the
/// URI and refusing one nobody publishes. Picked over one <c>feed://{name}</c> template (a client would have to know
/// the names) and hand-written handlers (redoing what the SDK does): F56 finding A1. Feeds don't change while the
/// server runs, so the list is built once, unlike the tools (<c>EngineToolSync</c>). The feed names no pack, so neither
/// does this (ADR 0027), and a pack stays convertible to MCP (ADR 0028).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
/// <seealso href="https://modelcontextprotocol.io/specification/2025-06-18/server/resources">MCP resources</seealso>
public static class FeedResources
{
    /// <summary>One resource per feed, in the feeds' order; none for none.</summary>
    public static IReadOnlyList<McpServerResource> From(IEnumerable<IDataFeed> feeds) =>
        feeds.Select(Resource).ToList();

    /// <summary>The resource one feed becomes; reading it reads the feed now, as the API's route does.</summary>
    private static McpServerResource Resource(IDataFeed feed)
    {
        var uri = $"{ResourceScheme.Feed.Wire()}://{feed.Name}";
        return McpServerResource.Create(
            async (CancellationToken cancellationToken) => new TextResourceContents
            {
                Uri = uri,
                MimeType = MediaTypeNames.Application.Json,
                Text = await feed.ReadJsonAsync(cancellationToken),
            },
            new McpServerResourceCreateOptions
            {
                UriTemplate = uri,
                Name = feed.Name,
                Description = feed.Description,
                MimeType = MediaTypeNames.Application.Json,
            });
    }
}
