using System.Security.Cryptography;
using System.Text;

namespace LouisAgent.Packs.Recipes.Library;

/// <summary>
/// A page's one spelling in the library, because a site and a search spell the same link differently: no <c>http</c> or
/// <c>https</c>, no <c>www.</c>, no last slash, no <c>#</c>. Without it we'd keep a page twice and miss it when we
/// look.
/// </summary>
public static class RecipeAddress
{
    private const string Www = "www.";

    /// <summary>Long enough that two pages won't share one at this size, short enough to be cheap to read.</summary>
    private const int IdLength = 16;

    /// <summary>The key for a page. A path's case and a query stay, because either can name another recipe.</summary>
    public static string Of(Uri page)
    {
        string host = page.Host.ToLowerInvariant();
        if (host.StartsWith(Www, StringComparison.Ordinal)) host = host[Www.Length..];

        string path = page.Query.Length > 0 ? page.AbsolutePath : page.AbsolutePath.TrimEnd('/');
        return host + path + page.Query;
    }

    /// <summary>
    /// The page's short id, the same however its link is spelled: a site's path has slashes and any length, which makes
    /// a poor last part for our own address.
    /// </summary>
    public static string IdOf(Uri page) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Of(page))))[..IdLength];

    /// <summary>Whether this could be an id: what a model hands us as one gets no further if it isn't.</summary>
    public static bool IsId(string text) =>
        text.Length == IdLength && text.All(letter => char.IsAsciiHexDigitLower(letter));
}
