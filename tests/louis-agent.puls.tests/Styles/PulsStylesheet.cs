using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// Puls's stylesheet as the browser sees it: the files app.css imports, read in their order and joined.
/// </summary>
/// <remarks>
/// Extract Function (Fowler), chosen in CL26: three style tests each read app.css with their own copy of the same
/// line; since app.css became a list of imports, one reader follows it, so a rule can move between area files
/// without breaking a test, and an import that points nowhere fails them all.
/// </remarks>
internal static partial class PulsStylesheet
{
    /// <summary>Where Puls's stylesheets live.</summary>
    internal static readonly string Folder =
        Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.puls", "wwwroot", "css");

    /// <summary>Every rule Puls loads, file after file in app.css's order.</summary>
    internal static string Read() =>
        string.Join("\n", Imports().Select(file => File.ReadAllText(Path.Combine(Folder, file))));

    /// <summary>The files app.css imports, in its order, as paths under the css folder.</summary>
    internal static IReadOnlyList<string> Imports() =>
        [.. Import().Matches(File.ReadAllText(Path.Combine(Folder, "app.css"))).Select(m => m.Groups[1].Value)];

    /// <summary>One import line, <c>@import url("chat/bars.css");</c>, and the path in it.</summary>
    [GeneratedRegex("""@import url\("([^"]+)"\);""")]
    private static partial Regex Import();
}
