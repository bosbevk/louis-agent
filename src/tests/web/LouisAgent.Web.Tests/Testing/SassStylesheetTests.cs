using LouisAgent.Testing;
using LouisAgent.Web.Testing.Styles;

namespace LouisAgent.Web.Tests.Testing;

/// <summary>
/// F166.3: a site's stylesheet is read from what its own build made, so a site in a repository of its own, built on the
/// framework's packages, reads the framework's partials and its libraries' styles as one in louis-agent does (Louis's
/// pick A of finding 9).
/// </summary>
public class SassStylesheetTests
{
    private string _site = null!;

    /// <summary>A site's project folder of its own for each test, with the CSS its build made.</summary>
    [SetUp]
    public void Site()
    {
        _site = Directory.CreateTempSubdirectory("louis-sass-stylesheet-").FullName;
        Directory.CreateDirectory(Path.Combine(_site, "wwwroot", "css"));
        File.WriteAllText(Path.Combine(_site, "wwwroot", "css", "app.css"), ".site-rule { color: red; }\n");
    }

    /// <summary>Deletes it, whatever happened.</summary>
    [TearDown]
    public void Tidy() => Directory.Delete(_site, recursive: true);

    /// <summary>A site that unpacked the framework's Sass reads a partial from there.</summary>
    [Test]
    public void AFrameworkPartial_IsReadFromTheSitesUnpackedSass()
    {
        string unpacked = Path.Combine(_site, "obj/framework-sass/web/settings");
        Directory.CreateDirectory(unpacked);

        string partial = new SassStylesheet(_site).FrameworkPartial("web/settings/colours");

        Assert.That(Path.GetFullPath(partial), Is.EqualTo(Path.GetFullPath(Path.Combine(unpacked, "_colours.scss"))));
    }

    /// <summary>A site in louis-agent, which unpacks nothing, still reads the framework's own source.</summary>
    [Test]
    public void AFrameworkPartial_IsTheFrameworksSource_WhenNothingWasUnpacked()
    {
        string partial = new SassStylesheet(_site).FrameworkPartial("web/settings/colours");

        string source = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web/settings/_colours.scss");

        Assert.That(Path.GetFullPath(partial), Is.EqualTo(Path.GetFullPath(source)));
    }

    /// <summary>
    /// A library's styles are read from where the site's build says the library's bundle is, wherever that is: a
    /// package in NuGet's folder, here a folder outside the site.
    /// </summary>
    [Test]
    public void ALibrarysStyles_AreReadFromWhereTheSitesBuildSaysTheyAre()
    {
        string library = Directory.CreateTempSubdirectory("louis-library-").FullName;
        string bundle = Path.Combine(library, "Shop.Parts.k3x9z2m1qa.bundle.scp.css");
        File.WriteAllText(bundle, ".library-rule[b-k3x9z2m1qa] { color: blue; }\n");

        string obj = Directory.CreateDirectory(Path.Combine(_site, "obj", "Debug", "net10.0")).FullName;
        File.WriteAllText(Path.Combine(obj, "Shop.Web.styles.css"),
            "@import '_content/Shop.Parts/Shop.Parts.k3x9z2m1qa.bundle.scp.css';\n");
        var asset = new StaticWebAsset(bundle, "Shop.Parts", "Shop.Parts.k3x9z2m1qa.bundle.scp.css");
        var manifest = new StaticWebAssetsManifest([asset]);
        File.WriteAllText(Path.Combine(obj, StaticWebAssetsManifest.FileName), manifest.ToJson());

        string css = new SassStylesheet(_site).Read();
        Directory.Delete(library, recursive: true);

        Assert.That(css, Does.Contain(".library-rule { color: blue; }"));
    }
}
