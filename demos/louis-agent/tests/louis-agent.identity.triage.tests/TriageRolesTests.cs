using LouisAgent.Choices;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// The roles the manager asks (F106.3.2): each is a specialist file of the identity that names its toolsets and ends
/// its work with its typed tool, and no other role's; every role file has a member and every member a file.
/// </summary>
public class TriageRolesTests
{
    /// <summary>A role's file, as the engine reads it.</summary>
    private static string Role(TriageRole role)
    {
        var identity = new TriageIdentity();
        var file = identity.Prompts.GetFileInfo($"{RoleCatalog.SpecialistsFolder}/{role.Wire()}.md");
        Assert.That(file.Exists, Is.True, $"{role.Wire()} is a file of the identity");
        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd();
    }

    /// <summary>Each role names its toolsets on its first line of text and ends with its own tool, never another's.</summary>
    [TestCase(TriageRole.Sorter, "toolsets: triage", "SortCase", "ReportInvestigation")]
    public void ARole_NamesItsToolsets_AndEndsWithItsTool(TriageRole role, string toolsets, string tool,
        string notItsTool)
    {
        string text = Role(role);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("level: specialist"));
            Assert.That(text, Does.Contain($"({toolsets})"));
            Assert.That(text, Does.Contain($"`{tool}`"));
            Assert.That(text, Does.Not.Contain(notItsTool));
            Assert.That(text, Does.Contain("Never invent"));
        });
    }

    /// <summary>A role file with no member is a role the code cannot name.</summary>
    [Test]
    public void EachRoleFileOfTheIdentity_IsAMember()
    {
        var identity = new TriageIdentity();
        var members = Enum.GetValues<TriageRole>().Select(r => r.Wire()).ToHashSet();
        var files = identity.Prompts.GetDirectoryContents(RoleCatalog.SpecialistsFolder).Select(f => f.Name)
            .Where(name => name.EndsWith(".md", StringComparison.Ordinal)).Select(name => name[..^3]).ToList();

        Assert.That(files, Is.Not.Empty, "the specialists folder was found");
        Assert.That(files.Where(name => !members.Contains(name)), Is.Empty, "a role file no TriageRole member names");
    }
}
