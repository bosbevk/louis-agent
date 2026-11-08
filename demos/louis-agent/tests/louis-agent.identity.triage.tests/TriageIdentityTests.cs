using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// The triage identity (F106.3.2): one identity, the manager's voice as its mode, the packs it names, and a voice that
/// reads as a mode with its routing naming the toolsets the manager has.
/// </summary>
public class TriageIdentityTests
{
    /// <summary>
    /// The mode file as the engine reads it from the identity's own prompts, its front matter off, so the first line
    /// is the voice's heading and the split finds the second.
    /// </summary>
    private static string Mode()
    {
        var identity = new TriageIdentity();
        using var reader = new StreamReader(identity.Prompts
            .GetFileInfo(RoleCatalog.FileName(identity.DefaultVoice, RoleLevel.Agent)).CreateReadStream());
        string text = reader.ReadToEnd().ReplaceLineEndings("\n");
        int close = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        return text[(close + "\n---\n".Length)..];
    }

    /// <summary>Its name, its default voice and the four packs it names, by their wire names.</summary>
    [Test]
    public void TheIdentity_IsTriage_OnFourPacks()
    {
        var identity = new TriageIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(identity.Name, Is.EqualTo(TriageIdentityName.Triage.Wire()));
            Assert.That(identity.DefaultVoice, Is.EqualTo($"{TriageIdentityName.Triage.Wire()}/voice"));
            Assert.That(identity.Packs, Is.EqualTo(Enum.GetValues<TriageIdentityPack>().Select(p => p.Wire())));
            Assert.That(identity.Packs,
                Does.Contain("triage").And.Contain("files").And.Contain("git").And.Contain("dotnet"));
        });
    }

    /// <summary>The voice is a mode: the manager's part first, then a routing that names its two toolsets.</summary>
    [Test]
    public void TheVoice_IsAMode_WhoseRoutingNamesItsToolsets()
    {
        var (voice, routing) = RoleCatalog.ModeParts(Mode());

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("# The triage manager").And.Contain("hub"));
            Assert.That(voice, Does.Contain("DecideCase").And.Contain("RaiseToPerson"));
            Assert.That(routing, Does.StartWith("# Routing").And.Contain("`triage`").And.Contain("`specialists`"));
            Assert.That(voice, Does.Not.Contain("Puls"), "the team names no other product");
        });
    }

    /// <summary>The three roles are named by the files they will be, under the specialists' folder.</summary>
    [Test]
    public void TheRoles_AreNamedByTheirFiles()
    {
        Assert.That(Enum.GetValues<TriageRole>().Select(role => role.Wire()),
            Is.EqualTo(new[] { "triage-sorter", "triage-investigator", "triage-reviewer" }));
    }
}
