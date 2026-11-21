using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// The guide in the site's corner is called what it is (F124, Louis's pick 3B): <c>louis-agent-guide</c>, its voice
/// under that name, and the three packs it answers with.
/// </summary>
public class GuideIdentityTests
{
    /// <summary>Its name, where its voice is, and that the voice is there.</summary>
    [Test]
    public void TheGuide_IsCalledTheGuide_AndFindsItsVoice()
    {
        var guide = new GuideIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(guide.Name, Is.EqualTo("louis-agent-guide"));
            Assert.That(guide.DefaultVoice, Is.EqualTo("louis-agent-guide/voice"));
            Assert.That(guide.Prompts.GetFileInfo("agent/louis-agent-guide/voice.md").Exists, Is.True);
            Assert.That(guide.Packs, Is.EqualTo(new[] { "docs", "leads", "site" }));
        });
    }
}
