using LouisAgent.Testing;

namespace LouisAgent.Puls.Tests.Styles.Colours;

/// <summary>
/// Puls's colours go by Bootstrap's names, and by a <c>--web-</c> name where Bootstrap has none, so the framework's
/// rules can work on any site. Each new name holds exactly the value the old one did, which is what keeps Puls looking
/// as it did. How it looks with Bootstrap's own parts reading these values is checked in the browser.
/// </summary>
public class BootstrapColourNamesTests
{
    private static readonly string[] OldNames =
    [
        "bg", "text", "ink", "muted", "dim", "surface", "sidebar", "border", "orange", "orange-deep", "orange-text",
        "orange-soft", "green", "green-deep", "green-light", "green-soft", "blue", "blue-soft", "navy", "navy-deep",
        "cream", "danger", "code-bg", "cta", "shadow-sm", "shadow-md", "shadow-glow", "radius", "font", "font-code",
        "ease", "spring",
    ];

    /// <summary>No rule sets or reads a colour by its old name, in the stylesheets or in the app's code.</summary>
    [Test]
    public void NoRule_UsesAnOldName()
    {
        string app = Path.Combine(TestPaths.RepoRoot, "demos/puls/web/louis-agent.puls");
        string[] files = Directory.GetFiles(app, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".scss", StringComparison.Ordinal) || f.EndsWith(".css", StringComparison.Ordinal)
                                                                      || f.EndsWith(".razor", StringComparison.Ordinal)
                                                                      || f.EndsWith(".cs", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var found = files.SelectMany(file => OldNames.Where(name => Uses(File.ReadAllText(file), name))
            .Select(name => $"{Path.GetFileName(file)}: --{name}"));

        Assert.That(found, Is.Empty);
    }

    /// <summary>Each new name holds the value the old one held.</summary>
    [TestCase("--bs-body-bg", "#1c1f22")]
    [TestCase("--bs-body-color", "#d4d4cf")]
    [TestCase("--bs-emphasis-color", "#efefeb")]
    [TestCase("--bs-secondary-color", "#9ba1a8")]
    [TestCase("--bs-tertiary-color", "#6c727a")]
    [TestCase("--bs-secondary-bg", "#25292d")]
    [TestCase("--bs-tertiary-bg", "#16191b")]
    [TestCase("--bs-border-color", "#33383d")]
    [TestCase("--bs-primary", "#f58220")]
    [TestCase("--bs-primary-text-emphasis", "#f7a55a")]
    [TestCase("--bs-primary-bg-subtle", "#3a2a1d")]
    [TestCase("--bs-success", "#00723a")]
    [TestCase("--bs-success-text-emphasis", "#38b449")]
    [TestCase("--bs-success-bg-subtle", "#1b3326")]
    [TestCase("--bs-link-color", "#38b449")]
    [TestCase("--bs-danger", "#f26b4f")]
    [TestCase("--web-primary-deep", "#e5731a")]
    [TestCase("--web-success-deep", "#00592d")]
    [TestCase("--web-sidebar-foot", "#121416")]
    [TestCase("--web-sidebar-foot-deep", "#0d0f10")]
    [TestCase("--web-code-bg", "#2a2e33")]
    public void EachNewName_HoldsTheOldValue(string name, string value) =>
        Assert.That(SassStylesheet.Of("louis-agent.puls").Read(), Does.Contain($"{name}: {value};"));

    /// <summary>Whether this text sets or reads the name, and not a longer name that starts with it.</summary>
    private static bool Uses(string text, string name) =>
        text.Contains($"--{name}:", StringComparison.Ordinal) || text.Contains($"var(--{name})",
                                                                  StringComparison.Ordinal)
                                                              || text.Contains($"var(--{name},",
                                                                  StringComparison.Ordinal);
}
