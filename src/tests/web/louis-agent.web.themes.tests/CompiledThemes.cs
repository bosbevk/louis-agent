namespace LouisAgent.Web.Themes.Tests;

/// <summary>
/// What the themes project's build compiles for its tests (F151.2, Louis's pick A of three): the framework's names,
/// then every theme's sheets. Each theme's sheet tests read it from here.
/// </summary>
internal static class CompiledThemes
{
    /// <summary>The compiled CSS, refused with a reason when the build made none.</summary>
    public static string Css()
    {
        string compiled = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.themes/obj/css/app.css");
        Assert.That(File.Exists(compiled), Is.True, "the themes project's build compiled its sheets");
        return File.ReadAllText(compiled);
    }
}
