using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

/// <summary>
/// A role's level is where its file is (F127.3.1 step 6), on the tests' own prompts: a mode under <c>agents/</c>, a
/// subagent under its agent's <c>subagents/</c>, a lookup agent under its <c>lookups/</c>. Since the folder names the
/// level, a role name may sit in one folder only.
/// </summary>
public class RoleLevelOfTests
{
    /// <summary>Each role is at the level of its folder.</summary>
    [TestCase("testprod/voice-en", RoleLevel.Agent)]
    [TestCase("testprod.researcher", RoleLevel.Subagent)]
    [TestCase("testprod.lookup", RoleLevel.Lookup)]
    public void ARole_IsAtItsFoldersLevel(string role, RoleLevel level) =>
        Assert.That(RoleCatalog.LevelOf(role), Is.EqualTo(level));

    /// <summary>A name no folder holds, the lookup agents' base and nothing at all have no level.</summary>
    [TestCase("nobody")]
    [TestCase("testprod-helper")]
    [TestCase(null)]
    public void ANameNoFolderHolds_HasNoLevel(string? role) =>
        Assert.That(RoleCatalog.LevelOf(role), Is.Null);

    /// <summary>No role name sits in two folders, so its folder names one level.</summary>
    [Test]
    public void NoRoleName_SitsInTwoLevels()
    {
        var names = Enum.GetValues<RoleLevel>().SelectMany(RoleCatalog.Available).ToList();

        Assert.That(names, Is.Unique);
    }
}
