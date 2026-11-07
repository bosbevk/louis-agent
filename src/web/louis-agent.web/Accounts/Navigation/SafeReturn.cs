namespace LouisAgent.Web.Accounts.Navigation;

/// <summary>
/// Where to go after a sign-in: the page the person came from, if it's one of ours. An address another site put in
/// the query (<c>//evil.test</c>, <c>https://…</c>) is never followed, since a sign-in page that forwards anywhere is
/// a phishing tool.
/// </summary>
public static class SafeReturn
{
    /// <summary>The site's home, the default.</summary>
    public const string Home = "./";

    /// <summary>The address to go to: a relative path of ours, else home.</summary>
    public static string Of(string? returnUrl)
    {
        string url = (returnUrl ?? "").Trim();
        if (url.Length == 0) return Home;
        if (url.StartsWith("//", StringComparison.Ordinal) || url.Contains("://", StringComparison.Ordinal))
            return Home;
        if (url.StartsWith('\\') || url.Contains(':', StringComparison.Ordinal)) return Home;
        return url.TrimStart('/') is { Length: > 0 } relative ? relative : Home;
    }
}
