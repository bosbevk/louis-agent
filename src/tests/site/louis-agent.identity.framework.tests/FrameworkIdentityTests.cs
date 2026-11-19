namespace LouisAgent.Identities.Framework.Tests;

/// <summary>
/// The framework Orbi is an identity of its own (F164.2, Louis: "the agents name is orbi but the framework orbi"):
/// called for what it speaks for, its voice under that name, on the docs and site packs and not the leads pack, since
/// its site doesn't sell, and ending each answer with choices, as the public Orbi does.
/// </summary>
public class FrameworkIdentityTests
{
    /// <summary>Its name, where its voice is, and that the voice is there.</summary>
    [Test]
    public void TheFrameworkOrbi_IsCalledForTheFramework_AndFindsItsVoice()
    {
        var orbi = new FrameworkIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(orbi.Name, Is.EqualTo("louis-agent-framework"));
            Assert.That(orbi.DefaultVoice, Is.EqualTo("louis-agent-framework/voice"));
            Assert.That(orbi.Prompts.GetFileInfo("agent/louis-agent-framework/voice.md").Exists, Is.True);
        });
    }

    /// <summary>It answers from the docs and acts on the page, and never sends a lead.</summary>
    [Test]
    public void TheFrameworkOrbi_NamesTheDocsAndSitePacks_AndNotTheLeads()
    {
        var orbi = new FrameworkIdentity();

        Assert.That(orbi.Packs, Is.EqualTo(new[] { "docs", "site" }));
    }

    /// <summary>Every answer ends with choices, but after Stop here, as the public Orbi's do.</summary>
    [Test]
    public void EveryAnswer_EndsWithChoices_ButAfterStopHere()
    {
        var orbi = new FrameworkIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(orbi.EndsEachAnswerWith, Is.EqualTo("Offer"));
            Assert.That(orbi.StopsOn, Is.EqualTo(new[] { "Stop here" }));
        });
    }

    /// <summary>
    /// It says it is an AI and never a person (ADR 0113), and it is the framework's Orbi, not Louis's: its voice says
    /// so, and tells no story of Louis's.
    /// </summary>
    [Test]
    public void ItsVoice_SaysItIsTheFrameworksOrbi_AndAnAI()
    {
        var orbi = new FrameworkIdentity();
        using var stream = orbi.Prompts.GetFileInfo("agent/louis-agent-framework/voice.md").CreateReadStream();
        string voice = new StreamReader(stream).ReadToEnd();

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("Orbi · AI"));
            Assert.That(voice, Does.Contain("the framework's Orbi"));
            Assert.That(voice, Does.Not.Contain("Malmesbury"), "Louis's story is the public Orbi's to tell");
        });
    }
}
