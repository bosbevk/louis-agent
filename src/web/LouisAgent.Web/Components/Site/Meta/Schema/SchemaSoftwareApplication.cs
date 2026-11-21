using System.Text.Json.Serialization;

namespace LouisAgent.Web.Components.Site.Meta.Schema;

/// <summary>
/// A piece of software the site is about, as schema.org says it (F123): what it is called and does, its kind and what
/// it runs on.
/// </summary>
/// <param name="Name">The software's name.</param>
/// <param name="Description">What it is, in a sentence.</param>
/// <param name="ApplicationCategory">Its kind, in schema.org's words, such as DeveloperApplication.</param>
/// <param name="OperatingSystem">What it runs on, such as .NET 10 on Windows, Linux and macOS.</param>
/// <param name="Url">Where it is described.</param>
public sealed record SchemaSoftwareApplication(
    string Name,
    string Description,
    string ApplicationCategory,
    string OperatingSystem,
    string Url)
{
    /// <summary>The vocabulary.</summary>
    [JsonPropertyName("@context")] public string Context => "https://schema.org";

    /// <summary>What this is.</summary>
    [JsonPropertyName("@type")] public string Type => "SoftwareApplication";
}
