using System.Text.Json.Serialization;

namespace LouisAgent.Web.Components.Site.Meta.Schema;

/// <summary>
/// Where a page sits in its site, as schema.org says it (F123): the steps from the site down to the page.
/// </summary>
/// <param name="ItemListElement">The steps, the site first.</param>
public sealed record SchemaBreadcrumbList(IReadOnlyList<SchemaListItem> ItemListElement)
{
    /// <summary>The vocabulary.</summary>
    [JsonPropertyName("@context")] public string Context => "https://schema.org";

    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")] public string Type => "BreadcrumbList";

    /// <summary>The site, then the page: the breadcrumb every page under the landing gets.</summary>
    public static SchemaBreadcrumbList Of(string siteName, string siteUrl, string pageName, string pageUrl) =>
        new([new SchemaListItem(1, siteName, siteUrl), new SchemaListItem(2, pageName, pageUrl)]);
}

/// <summary>One step of a breadcrumb: its place, its name and its address.</summary>
/// <param name="Position">Its place, from one.</param>
/// <param name="Name">Its name.</param>
/// <param name="Item">Its address.</param>
public sealed record SchemaListItem(int Position, string Name, string Item)
{
    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")] public string Type => "ListItem";
}
