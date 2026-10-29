using LouisAgent.Evals.Puls.Shared.Scoring;

namespace LouisAgent.Evals.Puls.Shared.Turns;

/// <summary>
/// The hosts a turn read (the Python's <c>stream.hosts</c>): the page a fetch or a rendered page read, and the shop a shop
/// tool searched (its site), in order, without repeats and without "www.".
/// </summary>
public static class ToolHosts
{
    private static readonly string[] PageReaders = ["FetchUrl", "ReadRenderedPage"];

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

    private static string HostOf(string url)
    {
        string host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : "";
        return host.StartsWith("www.", StringComparison.Ordinal) ? host["www.".Length..] : host;
    }

    private static string? Text(System.Text.Json.JsonElement input, string name) =>
        input.ValueKind == System.Text.Json.JsonValueKind.Object && input.TryGetProperty(name, out var value)
                                                                 && value.ValueKind ==
                                                                 System.Text.Json.JsonValueKind.String
            ? value.GetString()
            : null;
}
