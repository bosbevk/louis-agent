using LouisAgent.Composition.Sites;

namespace LouisAgent.Core.Agent.Toolsets;

/// <summary>
/// Where a host puts its site's places once it maps the site (F127.7), so a pack's tool can find them. Until then it
/// has no site and finds nothing.
/// </summary>
/// <remarks>
/// Proxy (GoF): the engine is built before the host maps its site, so the tools get this stand-in at once and the
/// host fills it later, with no line in a site's own program (ADR 0120).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public sealed class SitePlacesSlot : ISitePlaces
{
    private volatile ISitePlaces? _site;

    /// <summary>The host's site, from now on.</summary>
    public void Serve(ISitePlaces site) => _site = site;

    /// <inheritdoc/>
    public bool HasSite => _site?.HasSite ?? false;

    /// <inheritdoc/>
    public Task<IReadOnlyList<SitePlace>> AllAsync(CancellationToken cancellationToken = default) =>
        _site?.AllAsync(cancellationToken) ?? Task.FromResult<IReadOnlyList<SitePlace>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<SitePlace>> FindAsync(string words, int most,
        CancellationToken cancellationToken = default) =>
        _site?.FindAsync(words, most, cancellationToken) ?? Task.FromResult<IReadOnlyList<SitePlace>>([]);
}
