namespace LouisAgent.Web.Evals.Admin.Explorer.Sites;

/// <summary>
/// One kind of website a site wants its sources chart to count: its name, and the addresses it takes. A plain address
/// takes itself and what's under it (<c>shop.example</c> takes <c>www.shop.example</c>); one that starts with a dot
/// takes every address that ends that way (<c>.example</c>).
/// </summary>
/// <param name="Name">What the chart calls this kind.</param>
/// <param name="Addresses">The addresses it takes.</param>
/// <remarks>
/// Specification (Evans and Fowler): a small named rule a website either fits or doesn't. Which websites matter is the
/// site's own knowledge, so the rule travels as data from the site to the chart; the chart only counts. Without it
/// the framework's chart would have to name one site's shops.
/// </remarks>
/// <seealso href="https://www.martinfowler.com/apsupp/spec.pdf">Specifications (Evans and Fowler)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public sealed record SiteKind(string Name, params IReadOnlyList<string> Addresses)
{
    /// <summary>Whether this website is of this kind.</summary>
    public bool Fits(string host) => Addresses.Any(address => address.StartsWith('.')
        ? host.EndsWith(address, StringComparison.Ordinal)
        : host == address || host.EndsWith("." + address, StringComparison.Ordinal));
}
