using LouisAgent.Core.Skills.Roles;
using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Tests;

// F18 step 7.5.4c (ADR 0043): the roles Puls offers a session are an enum of the identity's own; a request, the stream and a stored run carry their text, and each is a role file of the identity.
public class PulsRoleTests
{
    [Test]
    public void TheRolesPulsOffers_TravelUnderTheirFileNames() =>
        Assert.That(Enum.GetValues<PulsRole>().Select(r => r.Wire()), Is.EqualTo(new[] { "puls-cook", "puls-researcher", "puls-analyst", "puls-historian", "puls-reader", "puls-minion-web-price-lookup" }));

    [Test]
    public void EachRole_IsAFileOfTheIdentity()
    {
        var identity = new PulsIdentity();
        foreach (var role in Enum.GetValues<PulsRole>())
        {
            string folder = role == PulsRole.MinionWebPriceLookup ? RoleCatalog.MinionsFolder : RoleCatalog.SpecialistsFolder;
            Assert.That(identity.Prompts.GetFileInfo($"{folder}/{role.Wire()}.md").Exists, Is.True, role.ToString());
        }
    }

    // The other way round: a role file with no member is a role the code cannot name, so a typo in a test or a request goes unnoticed. The minions' base (puls-minion) frames every minion and is no role.
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
