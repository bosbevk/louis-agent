using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;

/// <summary>
/// The sites a turn read, as the Python's <c>stream.hosts</c> listed them: the page a fetch or a rendered page read,
/// and the shop a shop tool searched (its site), in order, without repeats and without "www.".
/// </summary>
public static class ToolHosts
{
    /// <summary>The tools that read a page by its address.</summary>
    private static readonly string[] PageReaders = ["FetchUrl", "ReadRenderedPage"];

    /// <summary>The sites these calls read, each once, in the order they were first read.</summary>
    public static IReadOnlyList<string> Of(IEnumerable<ToolCall> calls)
    {
        var seen = new List<string>();
        foreach (var call in calls)
        {
            string? host = null;
            if (PageReaders.Contains(call.Name) && Text(call.Input, "url") is { } url)
                host = HostOf(url);
            else if (Shops.ShopTools.Contains(call.Name) && Text(call.Input, "shop") is { } shop)
                host = Shops.Hosts.FirstOrDefault(h => h.Name == shop.Trim().ToLowerInvariant()).Host;
            if (!string.IsNullOrEmpty(host) && !seen.Contains(host)) seen.Add(host);
        }

        return seen;
    }

    /// <summary>An address's site in lower case, without "www."; nothing if it isn't a full address.</summary>
    private static string HostOf(string url)
    {
        string host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : "";
        return host.StartsWith("www.", StringComparison.Ordinal) ? host["www.".Length..] : host;
    }

    /// <summary>The text a call's input holds under a name, if it holds one.</summary>
    private static string? Text(System.Text.Json.JsonElement input, string name) =>
        input.ValueKind == System.Text.Json.JsonValueKind.Object && input.TryGetProperty(name, out var value)
                                                                 && value.ValueKind ==
                                                                 System.Text.Json.JsonValueKind.String
            ? value.GetString()
            : null;
}
