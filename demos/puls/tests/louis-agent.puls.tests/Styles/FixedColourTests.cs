using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// The rules every site will share hold no fixed colour, or a new site would come out with Puls's orange glows whatever
/// colours it set. A colour with a see-through level reads an <c>-rgb</c> variable. Each variable holds the value that
/// was written in the rule, which is what keeps Puls looking as it did. The landing stays Puls's own, so it may.
/// </summary>
public partial class FixedColourTests
{
    private static readonly string[] SharedFolders = ["base/", "layout/", "chat/", "admin/"];

    /// <summary>A colour written as a value: matches <c>#f58220</c> and <c>rgba(245, 130, 32, .4)</c>, but not
    /// <c>rgba(var(--bs-primary-rgb), .4)</c>.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*\d", RegexOptions.NonBacktracking)]
    private static partial Regex FixedColour();

    /// <summary>No rule in the shared stylesheets writes a colour as a value.</summary>
    [Test]
    public void TheSharedRules_HoldNoFixedColour()
    {
        var css = SassStylesheet.Of("louis-agent.puls");

        var found = css.AreaFiles()
            .Where(file => SharedFolders.Any(folder => file.StartsWith(folder, StringComparison.Ordinal)))
            .Select(file => (File: file, Count: FixedColour().Count(css.Source(file))))
            .Where(file => file.Count > 0)
            .Select(file => $"{file.File}: {file.Count}");

        Assert.That(found, Is.Empty);
    }

    /// <summary>Each variable holds the value the rules had written.</summary>
    [TestCase("--bs-primary-rgb", "245, 130, 32")]
    [TestCase("--bs-danger-rgb", "242, 107, 79")]
    [TestCase("--bs-body-bg-rgb", "28, 31, 34")]
    [TestCase("--bs-secondary-bg-rgb", "37, 41, 45")]
    [TestCase("--bs-tertiary-bg-rgb", "22, 25, 27")]
    [TestCase("--web-overlay-rgb", "255, 255, 255")]
    [TestCase("--web-shadow-rgb", "36, 40, 44")]
    [TestCase("--web-success-text-rgb", "56, 180, 73")]
    [TestCase("--web-failed-rgb", "229, 72, 77")]
    [TestCase("--web-backdrop-rgb", "8, 9, 10")]
    [TestCase("--web-hover-rgb", "127, 127, 127")]
    [TestCase("--web-menu-bg-rgb", "35, 39, 43")]
    [TestCase("--web-menu-bg", "#23272b")]
    [TestCase("--web-on-primary", "#fff")]
    [TestCase("--web-bright", "#fff")]
    [TestCase("--web-soft-text", "#c9c9c3")]
    [TestCase("--web-settings-text", "#e4e4df")]
    [TestCase("--web-error-text", "#f8a08c")]
    [TestCase("--web-photo-text", "#333")]
    [TestCase("--web-border-hover", "#454b52")]
    [TestCase("--web-scrollbar", "#3b4046")]
    [TestCase("--web-dialog-bg", "#202428")]
    [TestCase("--web-code-number", "#c4a5f5")]
    [TestCase("--web-code-comment", "#8b93ad")]
    [TestCase("--web-live-dot", "#3fb950")]
    public void EachVariable_HoldsTheValueTheRulesHad(string name, string value) =>
        Assert.That(SassStylesheet.Of("louis-agent.puls").Read(), Does.Contain($"{name}: {value};"));
}
