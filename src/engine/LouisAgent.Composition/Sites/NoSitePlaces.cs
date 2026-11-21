namespace LouisAgent.Composition.Sites;

/// <summary>A host that serves no site: it has no places, and finds none (F127.7).</summary>
/// <remarks>
/// Null Object: a tool asks every host the same way and reads "no site" from <see cref="HasSite"/>, never a null.
/// </remarks>
public sealed class NoSitePlaces : ISitePlaces
{
    /// <summary>The one there is.</summary>
    public static readonly NoSitePlaces Instance = new();

    private NoSitePlaces()
    {
    }

    /// <inheritdoc/>
    public bool HasSite => false;

    /// <inheritdoc/>
    public Task<IReadOnlyList<SitePlace>> AllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitePlace>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<SitePlace>> FindAsync(string words, int most,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SitePlace>>([]);
}
