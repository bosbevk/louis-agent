namespace LouisAgent.Packs.Recipes.Library;

/// <summary>
/// A page's one spelling in the library, because a site and a search spell the same link differently: no <c>http</c> or
/// <c>https</c>, no <c>www.</c>, no last slash, no <c>#</c>. Without it we'd keep a page twice and miss it when we look.
/// </summary>
public static class RecipeAddress
{
    private const string Www = "www.";

    /// <summary>The key for a page. A path's case and a query stay, because either can name another recipe.</summary>
    public static string Of(Uri page)
    {
        string host = page.Host.ToLowerInvariant();
        if (host.StartsWith(Www, StringComparison.Ordinal)) host = host[Www.Length..];

        string path = page.Query.Length > 0 ? page.AbsolutePath : page.AbsolutePath.TrimEnd('/');
        return host + path + page.Query;
    }
}
