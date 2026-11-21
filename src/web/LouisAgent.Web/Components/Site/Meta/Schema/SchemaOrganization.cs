using System.Text.Json.Serialization;

namespace LouisAgent.Web.Components.Site.Meta.Schema;

/// <summary>
/// Who runs the site, as schema.org says it (F123): its name, address, logo and its profiles elsewhere.
/// </summary>
/// <param name="Name">The organisation's name.</param>
/// <param name="Url">Its site's address.</param>
/// <param name="Logo">The address of its logo image.</param>
/// <param name="SameAs">Its profiles on other sites, such as LinkedIn.</param>
public sealed record SchemaOrganization(string Name, string Url, string Logo, IReadOnlyList<string> SameAs)
{
    /// <summary>The vocabulary.</summary>
    [JsonPropertyName("@context")] public string Context => "https://schema.org";

    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")] public string Type => "Organization";
}
