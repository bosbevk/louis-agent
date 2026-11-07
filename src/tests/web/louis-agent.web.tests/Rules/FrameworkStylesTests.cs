using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The chat's frame is drawn by the framework, so its rules live there too, and a site lists them rather than copying
/// them. They read every colour from a variable, or each new site would come out in the colours of the first.
/// </summary>
public partial class FrameworkStylesTests
{
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Styles/web");

    /// <summary>A colour written as a value: matches <c>#f58220</c> and <c>rgba(245, 130, 32, .4)</c>, but not
    /// <c>rgba(var(--bs-primary-rgb), .4)</c>.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*\d", RegexOptions.NonBacktracking)]
    private static partial Regex FixedColour();

    /// <summary>The framework's rules, without its colours, which are where the values belong.</summary>
    private static IEnumerable<string> Rules() =>
        Directory.EnumerateFiles(Styles, "_*.scss", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(Styles, file).Replace('\\', '/')
                .StartsWith("settings/", StringComparison.Ordinal));

    /// <summary>The parts the framework draws have their rules in the framework.</summary>
    [TestCase(".sidebar")]
    [TestCase(".conversation")]
    [TestCase(".composer")]
    [TestCase(".delegation")]
    [TestCase(".work-item")]
    [TestCase(".dropdown-item")]
    [TestCase(".admin-table")]
    [TestCase(".run-table")]
    [TestCase(".trend-delta")]
    [TestCase(".account-card")]
    public void TheFramesRules_AreInTheFramework(string selector) =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)), Does.Contain(selector));

    /// <summary>The folded work panel loses its border, or that 1px gives the whole page scrollbars.</summary>
    [Test]
    public void TheFoldedWorkPanel_HasNoBorder() =>
        Assert.That(string.Concat(Rules().Select(File.ReadAllText)),
            Does.Contain(".work-hidden .work { visibility: hidden; border: 0; }"));

    /// <summary>No rule in the framework writes a colour as a value.</summary>
    [Test]
    public void NoRule_HoldsAFixedColour()
    {
        var found = Rules()
            .Select(file => (File: Path.GetFileName(file), Count: FixedColour().Count(File.ReadAllText(file))))
            .Where(file => file.Count > 0)
            .Select(file => $"{file.File}: {file.Count}");

        Assert.That(found, Is.Empty);
    }
}
