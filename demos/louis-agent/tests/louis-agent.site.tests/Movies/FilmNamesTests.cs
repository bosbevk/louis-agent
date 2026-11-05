using System.Text.RegularExpressions;
using LouisAgent.Site.Components.Movies;

namespace LouisAgent.Site.Tests.Movies;

/// <summary>What a film says must stay true of the code, so the site can't drift from it again.</summary>
public partial class FilmNamesTests
{
    /// <summary>Everything in the repo a film could be naming, in one lump to search.</summary>
    private static readonly Lazy<string> Source = new(() => string.Concat(
        new[] { "src/engine", "src/hosts", "packs", "demos/louis-agent/hosts" }
            .SelectMany(root =>
                Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, root), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText)));

    /// <summary>A type or a member on a card: matches <c>ToolCallGuards</c>, and not the plain word <c>Claude</c>.</summary>
    [GeneratedRegex(@"\b[A-Z][A-Za-z]*[a-z][A-Z][A-Za-z]*\b")]
    private static partial Regex CodeName();

    /// <summary>What a film's cards show as code.</summary>
    private static List<string> CodeOn<TFilm>() where TFilm : Microsoft.AspNetCore.Components.IComponent
    {
        using var ctx = new BunitContext();
        return ctx.Render<TFilm>().FindAll(".card code").Select(code => code.TextContent).ToList();
    }

    /// <summary>Every type a card names is in the repo: a rename can't leave a film naming what's gone.</summary>
    [Test]
    public void EveryNameOnACard_IsInTheCode()
    {
        var names = CodeOn<ShortMovie>().Concat(CodeOn<DetailedMovie>())
            .SelectMany(code => CodeName().Matches(code).Select(match => match.Value)).Distinct().ToList();

        Assert.That(names, Does.Contain("ToolCallGuards"), "the search found the cards' names");
        Assert.That(names.Where(name => !Regex.IsMatch(Source.Value, $@"\b{name}\b")), Is.Empty);
    }

    /// <summary>A demo's program names its packs, so the film must not say settings pick them.</summary>
    [Test]
    public void TheShortFilm_TellsTheStructureAsItIs()
    {
        using var ctx = new BunitContext();

        string film = ctx.Render<ShortMovie>().Markup;

        Assert.That(film, Does.Not.Contain("IDENTITIES").And.Not.Contain("PACKS").And.Not.Contain("Composition"));
        Assert.That(film, Does.Contain("Your program").And.Contain("Catalogue").And.Contain("A pack's tool"));
    }
}
