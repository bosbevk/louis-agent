using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The eval protocol's contract stays a leaf that the API reads instead of the harness. The runner, the API and the
/// portal all read it, so it takes in nothing but the choices, and the API never reaches past it into the harness.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmarks recorded from the protocol</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class EvalProtocolProjectTests
{
    /// <summary>The contract references the choices and nothing else, or one side would drag the others in.</summary>
    [Test]
    public void TheEvalProtocolContract_ReferencesOnlyTheChoices()
    {
        string csproj = Csproj("evals", "louis-agent.evals.protocol", "louis-agent.evals.protocol.csproj");

        var references = References(csproj, "ProjectReference|PackageReference");

        Assert.That(references, Is.EqualTo(new[] { "louis-agent.choices.csproj" }));
    }

    /// <summary>
    /// The API never references the harness: the harness runs as its own process, and the API reads its lines through
    /// the contract.
    /// </summary>
    [Test]
    public void TheApi_NeverReferencesTheHarness()
    {
        string csproj = Csproj("src", "louis-agent.api", "louis-agent.api.csproj");

        var references = References(csproj, "ProjectReference");

        Assert.That(references, Has.None.EqualTo("louis-agent.evals.csproj"));
    }

    /// <summary>A project file's text, from its path under the repo.</summary>
    private static string Csproj(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestPaths.RepoRoot, .. parts]));

    /// <summary>What a project file takes in, by file or package name, for the kinds of reference asked for.</summary>
    private static List<string> References(string csproj, string kinds) =>
        Reference().Matches(csproj)
            .Where(m => Regex.IsMatch(m.Groups[1].Value, $"^({kinds})$"))
            .Select(m => Path.GetFileName(m.Groups[2].Value.Replace('\\', '/')))
            .ToList();

    /// <summary>A project or package reference: its kind, then what it includes.</summary>
    [GeneratedRegex("<(ProjectReference|PackageReference) Include=\"([^\"]+)\"")]
    private static partial Regex Reference();
}
