namespace LouisAgent.Composition.Packs;

/// <summary>
/// Data a pack publishes for anyone to read, apart from its tools: the shop-prices pack's price tables, say. The API
/// serves each one, open, at <c>/v1/feeds/{name}</c>, without knowing which pack it belongs to.
/// </summary>
/// <remarks>
/// Plugin (Fowler, PoEAA): core names the shape and each pack plugs its own in, the way it plugs in its toolsets, so the
/// API never references a pack (ADR 0027). A feed is read-only and reads the pack's own data, so it maps one to one to
/// an MCP resource (a name, a description and the text it reads), and a pack stays convertible (ADR 0028). The pack
/// writes its own JSON from its own records (ADR 0041), so a feed's shape is the pack's, not core's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
/// <seealso href="https://modelcontextprotocol.io/specification/2025-06-18/server/resources">MCP resources</seealso>
public interface IDataFeed
{
    /// <summary>"shop-prices": lowercase letters, digits and dashes, unique in the catalogue; the last part of its URL.</summary>
    string Name { get; }

    /// <summary>What the data is and where it is from, one sentence, for the list of feeds.</summary>
    string Description { get; }

    /// <summary>The data as JSON text, read now. A feed answers with what it has; it doesn't throw for a store that's down.</summary>
    Task<string> ReadJsonAsync(CancellationToken cancellationToken);
}
