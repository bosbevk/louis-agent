using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Roles;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// The subagents the manager asks (F106.3.2): each sits inside the triage agent, names its toolsets and ends its work
/// with its typed tool, and no other's; every subagent is a call and every call a subagent.
/// </summary>
public class TriageRolesTests
{
    /// <summary>
    /// A subagent's voice, as the engine reads it: inside the triage agent, by its address (ADR 0133).
    /// </summary>
    private static string Voice(TriageCall subagent)
    {
        var identity = new TriageIdentity();
        var file = identity.Prompts.GetFileInfo(RoleCatalog.FileName(subagent.Wire(), RoleLevel.Subagent));
        Assert.That(file.Exists, Is.True, $"{subagent.Wire()} is a subagent of the identity");
        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Each subagent names its toolsets on its first line of text and ends with its own tool, never another's.
    /// </summary>
    [TestCase(TriageCall.Sorter, "toolsets: triage", "SortCase", "ReportInvestigation")]
    [TestCase(TriageCall.Investigator, "toolsets: triage, files, git, dotnet", "ReportInvestigation", "ReportReview")]
    [TestCase(TriageCall.Reviewer, "toolsets: triage, files, git", "ReportReview", "ReportInvestigation")]
    public void ASubagent_NamesItsToolsets_AndEndsWithItsTool(TriageCall subagent, string toolsets, string tool,
        string notItsTool)
    {
        string text = Voice(subagent);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("level: subagent"));
            Assert.That(text, Does.Contain($"({toolsets})"));
            Assert.That(text, Does.Contain($"`{tool}`"));
            Assert.That(text, Does.Not.Contain(notItsTool));
            Assert.That(text, Does.Contain("Never invent"));
        });
    }

    /// <summary>A subagent's folder no call names is one the manager can never ask.</summary>
    [Test]
    public void EachSubagentOfTheIdentity_IsACall()
    {
        var identity = new TriageIdentity();
        var calls = Enum.GetValues<TriageCall>().Select(call => call.Wire()).ToHashSet();
        var subagents = identity.Prompts.GetDirectoryContents($"agents/triage/{PromptFolder.Subagents.Wire()}")
            .Where(folder => folder.IsDirectory).Select(folder => $"triage.{folder.Name}").ToList();

        Assert.That(subagents, Is.Not.Empty, "the subagents folder was found");
        Assert.That(subagents.Where(name => !calls.Contains(name)), Is.Empty, "a subagent no TriageCall names");
    }
}
