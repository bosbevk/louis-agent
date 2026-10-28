using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The API is every product's: its code, its project file and its contract never name a product (an identity such as
/// Puls). A product gets its own front door (its web app, its compose file), and the API stays the one agent API.
/// </summary>
/// <remarks>
/// A ratchet, as FileSizeTests and RazorHasNoCodeTests are. The names the rename hasn't reached yet are listed in
/// NotYetRenamed: a new one fails the test, and a listed one that's gone fails too, so the list can only shrink until
/// it is empty. Two exceptions stay on purpose, in Allowed: the project file picks Puls's web app until Phase 9 moves
/// the web apps out, and the stored key <c>puls_calls</c> is still read as an alias so old runs keep their counts.
/// The product names come from the identities folder, so a new identity is checked with no change here.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0037-one-agent-api-products-get-a-front-door.md">ADR 0037: one agent API; products get a front door</seealso>
public class ApiNamesNoProductTests
{
    private static readonly string[] Projects = ["src/louis-agent.api", "src/louis-agent.api.contracts"];

    /// <summary>
    /// What may name a product for good (until its TODO line or Phase 9): a whole file, or a word in any file.
    /// </summary>
    private static readonly (string? File, string? Word)[] Allowed =
    [
        // The API serves Puls's web app when built with -p:WebApp=puls, until Phase 9 gives each product its own host.
        ("src/louis-agent.api/louis-agent.api.csproj", null),
        // Runs stored and recorded before the rename say puls_calls; it is read as an alias until a TODO line.
        (null, "puls_calls"),
    ];

    /// <summary>
    /// The names step 7b (iii) hasn't renamed yet, as "file: word", repo-relative with forward slashes. Remove a line
    /// in the commit that renames it.
    /// </summary>
    private static readonly HashSet<string> NotYetRenamed =
    [
        "src/louis-agent.api.contracts/Admin/Runs/TurnRows.cs: PulsCalls",
        "src/louis-agent.api.contracts/Info/InfoReplies.cs: Puls's",
        "src/louis-agent.api.contracts/louis-agent.api.contracts.csproj: Puls",
        "src/louis-agent.api/Admin/Replies/StoredRunReplies.cs: PulsCalls",
        "src/louis-agent.api/Evals/Export/ProtocolLines.cs: PulsCalls",
        "src/louis-agent.api/Evals/Runs/EvalProcess.cs: PULS_API",
        "src/louis-agent.api/Evals/Runs/EvalProcess.cs: pulsApi",
        "src/louis-agent.api/Evals/Runs/Protocol/EvalProtocol.cs: PulsCalls",
        "src/louis-agent.api/Evals/Runs/Start/RunnerStartInfo.cs: PULS_API",
        "src/louis-agent.api/Evals/Runs/Start/RunnerStartInfo.cs: PulsApi",
        "src/louis-agent.api/Evals/Store/EvalDocuments.cs: PulsCalls",
        "src/louis-agent.api/Evals/Store/Mongo/Documents/TurnDocuments.cs: PulsCalls",
        "src/louis-agent.api/Evals/Store/Recording/RunRecording.cs: PulsCalls",
        "src/louis-agent.api/Config/AdminOptions.cs: puls_benchmarks",
        "src/louis-agent.api/Config/AdminOptions.cs: puls_benchmarks_test",
    ];

    /// <summary>
    /// Nothing in the API names a product except what is allowed or still waiting for its rename, and a name that's
    /// been renamed is taken off the waiting list.
    /// </summary>
    [Test]
    public void TheApi_NamesNoProduct_ExceptItsAllowedAndWaitingNames()
    {
        var found = ProductWords().Where(hit => !IsAllowed(hit)).Select(hit => $"{hit.File}: {hit.Word}").ToHashSet();

        var newOffenders = found.Except(NotYetRenamed).Order().ToList();
        var renamed = NotYetRenamed.Except(found).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                "The API names no product (ADR 0037): say what it is for every agent instead:\n  "
                + string.Join("\n  ", newOffenders));
            Assert.That(renamed, Is.Empty,
                "Renamed now: remove from NotYetRenamed so the list only shrinks:\n  " + string.Join("\n  ", renamed));
        });
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
        // One pattern per product: the whole word around the name, so PulsCalls and puls_calls are told apart.
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
