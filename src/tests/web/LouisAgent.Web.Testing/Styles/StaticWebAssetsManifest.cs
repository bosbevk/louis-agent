using System.Text.Json;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// The list of every static file a site's build serves, which the build writes as
/// <c>obj/{Configuration}/{framework}/staticwebassets.build.json</c>: for each one, where it really is and whose it
/// is. A style test reads a library's styles from where this says they are, a package in NuGet's folder or a
/// referenced project's <c>obj</c>, so it reads them in any repository (F166.3's finding 9).
/// </summary>
/// <param name="Assets">The files, the site's own and every library's.</param>
public sealed record StaticWebAssetsManifest(IReadOnlyList<StaticWebAsset> Assets)
{
    /// <summary>The file's name in the build's folder.</summary>
    public const string FileName = "staticwebassets.build.json";

    /// <summary>The build's own manifest, read from a file it wrote.</summary>
    public static StaticWebAssetsManifest From(string file) =>
        JsonSerializer.Deserialize<StaticWebAssetsManifest>(File.ReadAllText(file))!;

    /// <summary>The manifest written out the way the build writes it, for a test that makes one.</summary>
    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>Where a library's bundle of scoped styles is, by the library's name, if the build serves one.</summary>
    public string? BundleOf(string library) =>
        Assets.FirstOrDefault(asset => asset.SourceId == library
                                       && asset.RelativePath.EndsWith(BundleEnding, StringComparison.Ordinal))
            ?.Identity;

    /// <summary>How the name of a bundle of scoped styles ends, a library's or the site's.</summary>
    private const string BundleEnding = ".bundle.scp.css";
}

/// <summary>One static file the build serves.</summary>
/// <param name="Identity">Where it really is on disk.</param>
/// <param name="SourceId">Whose it is: the site's or a library's name.</param>
/// <param name="RelativePath">The name it is served under.</param>
public sealed record StaticWebAsset(string Identity, string SourceId, string RelativePath);
