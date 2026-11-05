namespace LouisAgent.Packs.Recipes.Library.Excluded;

/// <summary>
/// The pages we opened and found no recipe on, kept so that nobody opens them again: search leaves them out, and a read
/// refuses them. We can't tell from a search hit, so each page costs one look, and only one. It's private, unlike the
/// feed, and clearing it after the reader improves gives those pages another chance.
/// </summary>
/// <remarks>
/// Repository (Fowler, PoEAA) with two strategies (Gang of Four): <see cref="MongoExcludedPages"/> and
/// <see cref="NoExcludedPages"/>, so nothing else knows where the list is kept. Apart from the recipes, because a page
/// with no recipe isn't a recipe, and the feed must never be able to publish one.
/// </remarks>
/// <seealso href="https://www.martinfowler.com/eaaCatalog/repository.html">Repository</seealso>
public interface IExcludedPages
{
    /// <summary>The ids of every page on the list, by <see cref="RecipeAddress.IdOf"/>.</summary>
    Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken);

    /// <summary>Adds a page; one already on the list stays as it was.</summary>
    Task AddAsync(ExcludedPage page, CancellationToken cancellationToken);
}

/// <summary>A page we found no recipe on, and when, so the list can be cleared or read later.</summary>
/// <param name="Page">The page, after any redirect.</param>
/// <param name="At">When we looked.</param>
/// <remarks>A record because it is a plain value (ADR 0040).</remarks>
public sealed record ExcludedPage(Uri Page, DateTimeOffset At)
{
    /// <summary>The page's id, the same however its link is spelled.</summary>
    public string Id => RecipeAddress.IdOf(Page);
}

/// <summary>The list when no database is set: it holds nothing, so every page gets looked at.</summary>
/// <remarks>Null Object, so a read and a search never have to ask whether there's a list.</remarks>
public sealed class NoExcludedPages : IExcludedPages
{
    /// <inheritdoc />
    public Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

    /// <inheritdoc />
    public Task AddAsync(ExcludedPage page, CancellationToken cancellationToken) => Task.CompletedTask;
}
