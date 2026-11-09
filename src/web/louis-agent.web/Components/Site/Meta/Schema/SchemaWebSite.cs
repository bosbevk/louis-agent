using System.Text.Json.Serialization;

namespace LouisAgent.Web.Components.Site.Meta.Schema;

/// <summary>The site itself, as schema.org says it (F123): its name and its address.</summary>
/// <param name="Name">The site's name.</param>
/// <param name="Url">The site's address.</param>
public sealed record SchemaWebSite(string Name, string Url)
{
    /// <summary>The vocabulary.</summary>
    [JsonPropertyName("@context")] public string Context => "https://schema.org";

    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")] public string Type => "WebSite";
}
