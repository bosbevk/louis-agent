using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Api.Site;

/// <summary>
/// Where a page sits in its site, as schema.org says it (F123): the site, then the page, at their addresses. The
/// framework writes it into every served page under the landing, so no page has to.
/// </summary>
/// <remarks>
/// The same shape as the web library's <c>SchemaBreadcrumbList</c>, kept here as its own record because the API serves
/// any site and references no site's web library (a shared shape is a shared project or a tested duplicate, ADR 0027);
/// <c>PageShellTests</c> holds its JSON.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record PageBreadcrumb(IReadOnlyList<PageBreadcrumbStep> ItemListElement)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    /// <summary>The vocabulary.</summary>
    [JsonPropertyName("@context")]
    public string Context => "https://schema.org";

    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")]
    public string Type => "BreadcrumbList";

    /// <summary>The site, then the page.</summary>
    public static PageBreadcrumb Of(string siteName, string siteUrl, string pageName, string pageUrl) =>
        new([new PageBreadcrumbStep(1, siteName, siteUrl), new PageBreadcrumbStep(2, pageName, pageUrl)]);

    /// <summary>The breadcrumb as a whole JSON-LD script for the page's head.</summary>
    public string Script() =>
        $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(this, Options)}</script>";
}

/// <summary>One step of a breadcrumb: its place, its name and its address.</summary>
/// <param name="Position">Its place, from one.</param>
/// <param name="Name">Its name.</param>
/// <param name="Item">Its address.</param>
internal sealed record PageBreadcrumbStep(int Position, string Name, string Item)
{
    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")]
    public string Type => "ListItem";
}
