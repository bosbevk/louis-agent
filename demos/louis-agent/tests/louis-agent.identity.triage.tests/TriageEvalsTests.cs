using LouisAgent.Choices;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// The triage team's evals (F106.3.2 step 7), one per role, as folders of files the coding demo's runner lists: each
/// opens its session as the role itself at a specialist's level (Louis's pick), with the role's toolsets, and checks
/// that the role ended with its tool and the choice its scenario expects. Loaded here as the runner loads them, with
/// the harness's general checks only, so a file the runner would refuse fails here first.
/// </summary>
public class TriageEvalsTests
{
    /// <summary>Each role's eval: its folder, the role, its toolsets, its tool and the field it must end with.</summary>
    private static readonly object[] Roles =
    [
        new object[] { "Sorter", TriageRole.Sorter, "triage", "SortCase", "department" },
        new object[]
        {
            "Investigator", TriageRole.Investigator, "triage,files,git,dotnet", "ReportInvestigation", "area"
        },
        new object[] { "Reviewer", TriageRole.Reviewer, "triage,files,git", "ReportReview", "happy" },
    ];

    /// <summary>
    /// The eval proves the triage identity, opens one mode as the role at depth 1 with its toolsets, and grades no
    /// error, the tool and the tool with the expected choice.
    /// </summary>
    [TestCaseSource(nameof(Roles))]
    public async Task ARolesEval_OpensAsTheRole_AndChecksItsEnding(string folder, TriageRole role, string toolsets,
        string tool, string field)
    {
        var eval = Load(folder);
        var modes = await eval.ModesAsync(null!, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(eval.Identity, Is.EqualTo(TriageIdentityName.Triage.Wire()));
            Assert.That(eval.Name, Is.EqualTo(role.Wire()));
            Assert.That(modes.Select(mode => (mode.Role, mode.Depth, string.Join(",", mode.Toolsets))),
                Is.EqualTo(new[] { (role.Wire(), 1, toolsets) }));
            Assert.That(eval.Checks.Select(check => check.Name),
                Is.EqualTo(new[] { $"{folder.ToLowerInvariant()}-no-error", "tool-used", "tool-used-with" }));
            Assert.That(eval.Scenarios, Has.Count.EqualTo(1));
            Assert.That(eval.Manual, Does.EndWith($"/Evals/{folder}/manual.md"));
            Assert.That(File.ReadAllText(Path.Combine(Folder(folder), "eval.json")),
                Does.Contain($"\"tool\": \"{tool}\", \"field\": \"{field}\""));
        });
    }

    /// <summary>The folder the evals project copied beside these tests, as the runner finds it.</summary>
    private static string Folder(string role) => Path.Combine(AppContext.BaseDirectory, "EvalFiles", "demos",
        "louis-agent", "evals", "louis-agent.identity.triage.evals", "Evals", role);

    /// <summary>The eval as the runner loads it, with the harness's general checks and readers.</summary>
    private static FileEval Load(string role) =>
        FileEval.Load(Folder(role), new CheckCatalog(GeneralChecks.All), new ReaderCatalog(GeneralReaders.All),
            $"demos/louis-agent/evals/louis-agent.identity.triage.evals/Evals/{role}");
}
