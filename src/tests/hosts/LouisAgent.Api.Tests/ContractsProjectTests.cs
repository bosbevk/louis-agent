using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The records both sides read live in their own project, LouisAgent.Api.Contracts, which references nothing, so the
/// API and any client (Puls, the code-review app, the orchestrator, someone else's app) can take it without the rest.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class ContractsProjectTests
{
    private const string Assembly = "LouisAgent.Api.Contracts";

    /// <summary>The project file, read from the repo.</summary>
    private static string Csproj(params string[] parts) =>
        File.ReadAllText(Path.Combine([ApiTestHost.RepoRoot, .. parts]));

    /// <summary>A project everyone can take can't bring anything along: no project of ours and no package.</summary>
    [Test]
    public void TheContracts_ReferenceNothing()
    {
        string path = Path.Combine(ApiTestHost.RepoRoot, "src", "hosts", Assembly, $"{Assembly}.csproj");
        Assert.That(File.Exists(path), Is.True, path);

        var references = Regex
            .Matches(File.ReadAllText(path), "<(ProjectReference|PackageReference) Include=\"([^\"]+)\"")
            .Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}")
            .ToList();

        Assert.That(references, Is.Empty);
    }

    /// <summary>The API builds its replies from the contract, so it references it.</summary>
    [Test]
    public void TheApi_ReferencesTheContracts()
    {
        Assert.That(Csproj("src", "hosts", "LouisAgent.Api", "LouisAgent.Api.csproj"),
            Does.Contain($@"{Assembly}\{Assembly}.csproj"));
    }

    /// <summary>The admin replies and their options object are the contract's, not the API's.</summary>
    [TestCase("LouisAgent.Api.Contracts.Admin.Overview.OverviewReply")]
    [TestCase("LouisAgent.Api.Contracts.Admin.Imports.ImportsReply")]
    [TestCase("LouisAgent.Api.Contracts.Admin.Screens.ScreenActionReply")]
    [TestCase("LouisAgent.Api.Contracts.Admin.AdminJson")]
    public void AnAdminReply_LivesInTheContracts(string type)
    {
        Assert.That(Type.GetType($"{type}, {Assembly}"), Is.Not.Null, type);
    }
}
