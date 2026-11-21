using LouisAgent.Core.Skills.Roles;
using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Tests;

/// <summary>
/// The roles Puls offers a session are an enum of the identity's own (F18 step 7.5.4c, ADR 0043): a request, the stream
/// and a stored run carry their text, and each is a role file of the identity.
/// </summary>
public class PulsRoleTests
{
    /// <summary>Every role travels under its file's name, in the order the enum lists them.</summary>
    [Test]
    public void TheRolesPulsOffers_TravelUnderTheirFileNames() =>
        Assert.That(Enum.GetValues<PulsRole>().Select(r => r.Wire()),
            Is.EqualTo(new[]
            {
                "puls-cook", "puls-researcher", "puls-analyst", "puls-historian", "puls-reader",
                "puls-minion-web-price-lookup", "puls-minion-find-recipe"
            }));

    /// <summary>Every role is a file of the identity: a specialist's in its folder, a minion's in its own.</summary>
    [Test]
    public void EachRole_IsAFileOfTheIdentity()
    {
        var identity = new PulsIdentity();
        foreach (var role in Enum.GetValues<PulsRole>())
        {
            string folder = RoleCatalog.MinionBaseOf(role.Wire()) is null
                ? RoleCatalog.SpecialistsFolder
                : RoleCatalog.MinionsFolder;
            Assert.That(identity.Prompts.GetFileInfo($"{folder}/{role.Wire()}.md").Exists, Is.True, role.ToString());
        }
    }

    /// <summary>
    /// The other way round: a role file with no member is a role the code cannot name, so a typo in a test or a request
    /// goes unnoticed. The minions' base (puls-minion) frames every minion and is no role.
    /// </summary>
    [Test]
    public void EachRoleFileOfTheIdentity_IsAMember()
    {
        var identity = new PulsIdentity();
        var members = Enum.GetValues<PulsRole>().Select(r => r.Wire()).ToHashSet();
        var files = new[] { RoleCatalog.SpecialistsFolder, RoleCatalog.MinionsFolder }
            .SelectMany(folder => identity.Prompts.GetDirectoryContents(folder).Select(f => f.Name))
            .Where(name => name.EndsWith(".md", StringComparison.Ordinal)).Select(name => name[..^3])
            .Where(name => !RoleCatalog.IsMinionBase(name));

        Assert.That(files.Where(name => !members.Contains(name)), Is.Empty, "a role file no PulsRole member names");
    }
}
