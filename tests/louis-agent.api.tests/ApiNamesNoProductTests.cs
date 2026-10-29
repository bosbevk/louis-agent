using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The API is every product's: its code, its project file and its contract never name a product (an identity such as
/// Puls). A product gets its own front door (its web app, its compose file), and the API stays the one agent API.
/// </summary>
/// <remarks>
/// It was a ratchet while the rename ran (a waiting list that could only shrink, as FileSizeTests has); the list is
/// empty now, so it's a plain rule. One exception stays on purpose, in Allowed: the project file picks Puls's web app
/// until Phase 9 moves the web apps out. The stored turn's old <c>puls_calls</c> key went with ADR 0049: nothing reads
/// an older shape before a sale. The product names come from the identities folder, so a new identity is checked with
/// no change here.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0037-one-agent-api-products-get-a-front-door.md">ADR 0037: one agent API; products get a front door</seealso>
public class ApiNamesNoProductTests
{
    /// <summary>The API's own projects: its host, and the contract its clients read.</summary>
    private static readonly string[] Projects = ["src/louis-agent.api", "src/louis-agent.api.contracts"];

    /// <summary>
    /// What may name a product for good (until its TODO line or Phase 9): a whole file, or a word in any file.
    /// </summary>
    private static readonly (string? File, string? Word)[] Allowed =
    [
        // The API serves Puls's web app when built with -p:WebApp=puls, until Phase 9 gives each product its own host.
        ("src/louis-agent.api/louis-agent.api.csproj", null),
    ];

    /// <summary>Nothing in the API names a product, except what is allowed on purpose.</summary>
    [Test]
    public void TheApi_NamesNoProduct_ExceptWhatIsAllowed()
    {
        var found = ProductWords().Where(hit => !IsAllowed(hit)).Select(hit => $"{hit.File}: {hit.Word}").Distinct()
            .Order().ToList();

        Assert.That(found, Is.Empty,
            "The API names no product (ADR 0037): say what it is for every agent instead:\n  "
            + string.Join("\n  ", found));
    }

    /// <summary>Each allowed name is still there, so the list can't go stale after its TODO line is done.</summary>
    [Test]
    public void EveryAllowedName_IsStillThere()
    {
        var hits = ProductWords().ToList();

        Assert.That(Allowed.Where(a => !hits.Any(hit => a.File == hit.File || a.Word == hit.Word)), Is.Empty,
            "an allowed name is gone: take it off Allowed");
    }

    /// <summary>The products are the identities, one folder each, named louis-agent.identity.{product}.</summary>
    [Test]
    public void TheProducts_AreReadFromTheIdentitiesFolder()
    {
        Assert.That(Products(), Does.Contain(new LouisAgent.Identities.Puls.PulsIdentity().Name));
    }

    /// <summary>Every word in the API's code and project files that has a product's name in it.</summary>
    private static IEnumerable<(string File, string Word)> ProductWords()
    {
        // One pattern per product: the whole word around the name, so a hit names the word that holds it.
        var patterns = Products().Select(p => new Regex($"[A-Za-z0-9_']*{Regex.Escape(p)}[A-Za-z0-9_']*",
            RegexOptions.IgnoreCase)).ToList();

        foreach (string project in Projects)
        {
            string dir = Path.Combine(ApiTestHost.RepoRoot, project);
            foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (!(file.EndsWith(".cs") || file.EndsWith(".csproj")) || InBuildOutput(file)) continue;

                string relative = Path.GetRelativePath(ApiTestHost.RepoRoot, file).Replace('\\', '/');
                string text = File.ReadAllText(file);
                foreach (var match in patterns.SelectMany(p => p.Matches(text)))
                    yield return (relative, match.Value);
            }
        }
    }

    /// <summary>A hit the API may keep: one in an allowed file, or an allowed word.</summary>
    private static bool IsAllowed((string File, string Word) hit) =>
        Allowed.Any(a => a.File == hit.File || a.Word == hit.Word);

    /// <summary>Each identity's product name, from its project folder's name.</summary>
    private static IReadOnlyList<string> Products() =>
        Directory.GetDirectories(Path.Combine(ApiTestHost.RepoRoot, "identities"), "louis-agent.identity.*")
            .Select(dir => Path.GetFileName(dir)["louis-agent.identity.".Length..])
            .ToList();

    /// <summary>What the build wrote (bin/, obj/) isn't the API's code.</summary>
    private static bool InBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
}
