using System.Globalization;
using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// A site with no colours of its own comes out in the framework's: black and white with metallic greys, the static
/// site's Console theme. The only colours in it are a muted red for errors and an amber for warnings.
/// </summary>
public partial class BaseColoursTests
{
    private static readonly string[] AllowedToHaveColour =
        ["bs-danger", "bs-warning", "web-error-text", "web-failed"];

    /// <summary>A colour written as six hex digits, such as <c>#d9675f</c>, and not <c>#d9675</c>.</summary>
    [GeneratedRegex(@"#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})\b", RegexOptions.NonBacktracking)]
    private static partial Regex Hex();

    /// <summary>One colour's line: matches <c>colours.both(bs-primary, #ffffff</c> and <c>--web-bright: #ffffff</c>.
    /// </summary>
    [GeneratedRegex(@"(?:colours\.(?:both|numbers)\(|--)([a-z-]+)(?:,\s*|:\s*)(#[0-9a-f]+)")]
    private static partial Regex Colour();

    private static string Base => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Styles/web/settings/_colours.scss"));

    /// <summary>Every colour is a grey, bar the red and the amber.</summary>
    [Test]
    public void EveryColour_IsAGrey_BarTheRedAndTheAmber()
    {
        var colours = Colour().Matches(Base).ToList();
        Assert.That(colours, Has.Count.GreaterThan(20), "the theme is read");

        var tinted = colours.Where(c => !AllowedToHaveColour.Contains(c.Groups[1].Value))
            .Where(c => !IsGrey(c.Groups[2].Value))
            .Select(c => $"{c.Groups[1].Value}: {c.Groups[2].Value}");

        Assert.That(tinted, Is.Empty);
    }

    /// <summary>The red and the amber are there, and they aren't greys.</summary>
    [TestCase("bs-danger")]
    [TestCase("bs-warning")]
    public void TheRedAndTheAmber_AreColours(string name)
    {
        var colour = Colour().Matches(Base).Single(c => c.Groups[1].Value == name);

        Assert.That(IsGrey(colour.Groups[2].Value), Is.False);
    }

    /// <summary>A colour's numbers are close enough to each other that it reads as grey, as the site's slabs do.
    /// </summary>
    private static bool IsGrey(string colour)
    {
        var match = Hex().Match(colour);
        Assert.That(match.Success, Is.True, $"{colour} is written with six digits");
        int[] parts = [.. match.Groups.Values.Skip(1).Select(p => int.Parse(p.Value, NumberStyles.HexNumber))];
        return parts.Max() - parts.Min() <= 14;
    }
}
