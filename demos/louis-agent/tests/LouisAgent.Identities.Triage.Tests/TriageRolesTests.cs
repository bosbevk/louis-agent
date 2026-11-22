using LouisAgent.Choices;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Skills;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// The roles the manager asks (F106.3.2): each is a subagent of the triage agent that names its toolsets and ends
/// its work with its typed tool, and no other role's; every role file has a member and every member a file.
/// </summary>
public class TriageRolesTests
{
    /// <summary>A role's file, as the engine reads it: inside the triage agent, by its address (ADR 0133).</summary>
    private static string Role(TriageRole role)
    {
        var identity = new TriageIdentity();
        var file = identity.Prompts.GetFileInfo(RoleCatalog.FileName(AddressOf(role), RoleLevel.Subagent));
        Assert.That(file.Exists, Is.True, $"{role.Wire()} is a file of the identity");
        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Each role names its toolsets on its first line of text and ends with its own tool, never another's.
    /// </summary>
    [TestCase(TriageRole.Sorter, "toolsets: triage", "SortCase", "ReportInvestigation")]
    [TestCase(TriageRole.Investigator, "toolsets: triage, files, git, dotnet", "ReportInvestigation", "ReportReview")]
    [TestCase(TriageRole.Reviewer, "toolsets: triage, files, git", "ReportReview", "ReportInvestigation")]
    public void ARole_NamesItsToolsets_AndEndsWithItsTool(TriageRole role, string toolsets, string tool,
        string notItsTool)
    {
        string text = Role(role);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("level: subagent"));
            Assert.That(text, Does.Contain($"({toolsets})"));
            Assert.That(text, Does.Contain($"`{tool}`"));
            Assert.That(text, Does.Not.Contain(notItsTool));
            Assert.That(text, Does.Contain("Never invent"));
        });
    }

    /// <summary>A subagent's folder with no member is a role the code cannot name.</summary>
    [Test]
    public void EachSubagentOfTheIdentity_IsAMember()
    {
        var identity = new TriageIdentity();
        var members = Enum.GetValues<TriageRole>().Select(AddressOf).ToHashSet();
        var subagents = identity.Prompts.GetDirectoryContents($"agents/triage/{RoleCatalog.SubagentsFolder}")
            .Where(folder => folder.IsDirectory).Select(folder => $"triage.{folder.Name}").ToList();

        Assert.That(subagents, Is.Not.Empty, "the subagents folder was found");
        Assert.That(subagents.Where(name => !members.Contains(name)), Is.Empty,
            "a subagent no TriageRole member names");
    }

    /// <summary>
    /// A member's address: its wire name with its dash a dot, until the team names its subagents by their addresses
    /// alone (CL68.2 step 6).
    /// </summary>
    private static string AddressOf(TriageRole role) => role.Wire().Replace('-', '.');
}
