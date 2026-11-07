using System.Text.RegularExpressions;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The framework's scripts are modules beside their components (CL31): every path a framework class imports is a file
/// on disk, every function it invokes on a module is exported there, no framework script but first-paint.js writes
/// to the window, and a site's page lists nothing of ours but first-paint.js. The rule it replaces only looked for
/// two globals, and missed the download that one site defined for everyone.
/// </summary>
public partial class FrameworkModulesTests
{
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web");

    /// <summary>The libraries whose classes import the framework's modules.</summary>
    private static readonly string[] Libraries =
        ["louis-agent.web", "louis-agent.web.evals", "louis-agent.web.accounts"];

    /// <summary>A module's path as a class names it: <c>./_content/louis-agent.web/js/dialogs.js</c>.</summary>
    [GeneratedRegex("\"\\./_content/(louis-agent\\.web(?:\\.\\w+)?)/([A-Za-z0-9_./-]+\\.js)\"",
        RegexOptions.NonBacktracking)]
    private static partial Regex ModulePath();

    /// <summary>A function being invoked on a module: matches <c>InvokeVoidAsync("focus"</c> and gives <c>focus</c>.</summary>
    [GeneratedRegex("Invoke(?:Void)?Async(?:<[^>]+>)?\\(\"([A-Za-z]+)\"", RegexOptions.NonBacktracking)]
    private static partial Regex Invoked();

    /// <summary>A module's export: matches <c>export function focus(</c> and <c>export const focus =</c>.</summary>
    [GeneratedRegex("export (?:async )?(?:function|const|let) ([A-Za-z]+)", RegexOptions.NonBacktracking)]
    private static partial Regex Exported();

    /// <summary>A script writing a global: matches <c>window.chatUi =</c>.</summary>
    [GeneratedRegex("window\\.[A-Za-z]+\\s*=", RegexOptions.NonBacktracking)]
    private static partial Regex WritesWindow();

    /// <summary>A path a class names, as a file on disk.</summary>
    private static string OnDisk(string library, string path) => Path.Combine(Web, library,
        path.StartsWith("js/", StringComparison.Ordinal) ? "wwwroot/" + path : path);

    private static IEnumerable<string> Sources(string library) =>
        Directory.EnumerateFiles(Path.Combine(Web, library), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    /// <summary>Every module a class names is a file on disk.</summary>
    [Test]
    public void EveryModuleAClassNames_IsOnDisk()
    {
        var missing = Libraries.SelectMany(Sources)
            .SelectMany(file =>
                ModulePath().Matches(File.ReadAllText(file)).Select(m => (m.Groups[1].Value, m.Groups[2].Value)))
            .Distinct()
            .Where(path => !File.Exists(OnDisk(path.Item1, path.Item2)))
            .ToList();

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// Every function a class invokes on a module is exported by a module that class names. A class names its modules
    /// through <see cref="FrameworkScripts"/> (or its library's own paths), so the paths are read from that class
    /// and the names from the calls.
    /// </summary>
    [Test]
    public void EveryFunctionAClassInvokes_IsExportedByAModuleItNames()
    {
        var paths = PathsByName();
        var problems = new List<string>();
        foreach (string file in Libraries.SelectMany(Sources))
        {
            string source = File.ReadAllText(file);
            var modules = ModulesNamed(source, paths).ToList();
            if (modules.Count == 0) continue;
            var exported = modules
                .SelectMany(module => Exported().Matches(File.ReadAllText(module)).Select(m => m.Groups[1].Value))
                .ToHashSet();
            problems.AddRange(Invoked().Matches(source).Select(m => m.Groups[1].Value).Distinct()
                .Where(name => !exported.Contains(name))
                .Select(name =>
                    $"{Path.GetRelativePath(Web, file)}: {name} is not exported by {string.Join(", ", modules.Select(m => Path.GetFileName(m)))}"));
        }

        Assert.That(problems, Is.Empty);
    }

    /// <summary>Only first-paint.js, which runs before anything draws, writes to the window.</summary>
    [Test]
    public void NoFrameworkModule_WritesToTheWindow_ButFirstPaint()
    {
        var writers = Libraries
            .SelectMany(library =>
                Directory.EnumerateFiles(Path.Combine(Web, library), "*.js", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(file => Path.GetFileName(file) != "first-paint.js" && WritesWindow().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(Web, file))
            .ToList();

        Assert.That(writers, Is.Empty);
    }

    /// <summary>A site's page lists nothing of ours but first-paint.js; the modules load themselves.</summary>
    [Test]
    public void ASitesPage_ListsOnlyFirstPaint_OfOurs()
    {
        var listed = FrameworkWebSites.Folders()
            .Select(folder => Path.Combine(folder, "wwwroot", "index.html"))
            .Where(File.Exists)
            .SelectMany(page => File.ReadLines(page).Where(line =>
                    line.Contains("_content/louis-agent.", StringComparison.Ordinal) &&
                    line.Contains("<script", StringComparison.Ordinal))
                .Select(line =>
                    $"{Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(page)))}: {line.Trim()}"))
            .Where(line => !line.Contains("js/first-paint.js", StringComparison.Ordinal))
            .ToList();

        Assert.That(listed, Is.Empty);
    }

    /// <summary>What the browser keeps goes by a neutral name, the same for the first paint and for the settings.</summary>
    [TestCase("wwwroot/js/preferences.js")]
    [TestCase("wwwroot/js/first-paint.js")]
    [TestCase("Components/Chat/Shell/ChatShell.razor.js")]
    public void WhatTheBrowserKeeps_GoesByANeutralName(string file) =>
        Assert.That(File.ReadAllText(Path.Combine(Web, "louis-agent.web", file)),
            Does.Contain("\"chat.").And.Not.Contain("\"puls."));

    /// <summary>The script holds no words for the person: it says what went wrong, and the site's words say it.</summary>
    [Test]
    public void ThePreferences_NameAProblem_RatherThanSayingIt()
    {
        string script = File.ReadAllText(Path.Combine(Web, "louis-agent.web", "wwwroot/js/preferences.js"));

        Assert.That(script, Does.Contain("\"denied\"").And.Contain("error: \"no-browser\""));
        Assert.That(script, Does.Not.Contain("accept-language=af"), "the place's language is the site's");
    }

    /// <summary>The constants of <see cref="FrameworkScripts"/> and the account library's, by name, as the classes use them.</summary>
    private static Dictionary<string, string> PathsByName()
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Libraries.SelectMany(Sources)
                     .Where(file => Path.GetFileName(file).EndsWith("Scripts.cs", StringComparison.Ordinal)))
        {
            // A constant built on the library's Content prefix names no library: it is the file's own.
            string own = Path.GetRelativePath(Web, file).Split(Path.DirectorySeparatorChar)[0];
            foreach (Match m in ConstantPath().Matches(File.ReadAllText(file)))
                paths[$"{Path.GetFileNameWithoutExtension(file)}.{m.Groups[1].Value}"] =
                    OnDisk(m.Groups[2].Success ? m.Groups[2].Value : own, m.Groups[3].Value);
        }

        return paths;
    }

    /// <summary>A path constant: matches <c>public const string Dialogs = Content + "js/dialogs.js";</c>.</summary>
    [GeneratedRegex(
        "public const string (\\w+) = (?:Content \\+ )?\"(?:\\./_content/(louis-agent\\.web(?:\\.\\w+)?)/)?([A-Za-z0-9_./-]+\\.js)\";",
        RegexOptions.NonBacktracking)]
    private static partial Regex ConstantPath();

    /// <summary>The modules a source names, by constant (<c>FrameworkScripts.Dialogs</c>) or by path.</summary>
    private static IEnumerable<string> ModulesNamed(string source, Dictionary<string, string> paths) =>
        paths.Where(p => source.Contains(p.Key, StringComparison.Ordinal)).Select(p => p.Value)
            .Concat(ModulePath().Matches(source).Select(m => OnDisk(m.Groups[1].Value, m.Groups[2].Value)))
            .Distinct();
}
