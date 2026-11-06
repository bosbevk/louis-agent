using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// A language lives in its own file. The routing is read in every chat, so an Afrikaans line there was copied into
/// answers to English questions; each voice now holds its own labels and its own lines, and the routing holds neither.
/// </summary>
public class PulsLanguageFilesTests
{
    /// <summary>A prompt's file as the model reads it, through the catalogue.</summary>
    private static string Prompt(string path) => PromptCatalog.Get(path)!.Body;

    /// <summary>The routing has no Afrikaans in it, and no example of either language's note.</summary>
    [TestCase("Wil hê")]
    [TestCase("Vir hoeveel")]
    [TestCase("Reeds gesê")]
    [TestCase("Ek gee dit")]
    [TestCase("Ek soek vir eers")]
    [TestCase("haastig")]
    [TestCase("enige")]
    [TestCase("**For:** cook")]
    [TestCase("I'm handing this")]
    public void TheRouting_HoldsNoLanguage(string words) =>
        Assert.That(Prompt("agent/puls/voice.md"), Does.Not.Contain(words));

    /// <summary>Each voice has the note's labels and the lines it says, in its own language.</summary>
    [TestCase("agent/puls/voice-af.md", "**Vir:** kok")]
    [TestCase("agent/puls/voice-af.md", "**Wil hê:**")]
    [TestCase("agent/puls/voice-af.md", "**Vir hoeveel:**")]
    [TestCase("agent/puls/voice-af.md", "Vir hoeveel mense kook jy?")]
    [TestCase("agent/puls/voice-af.md", "Ek soek vir eers nie winkels se pryse op nie")]
    [TestCase("agent/puls/voice-en.md", "**For:** cook")]
    [TestCase("agent/puls/voice-en.md", "**Wants:**")]
    [TestCase("agent/puls/voice-en.md", "**Serves:**")]
    [TestCase("agent/puls/voice-en.md", "How many people are you cooking for?")]
    [TestCase("agent/puls/voice-en.md", "I don't look up shops' prices for now")]
    public void EachVoice_HoldsItsOwnLines(string file, string words) =>
        Assert.That(Prompt(file), Does.Contain(words));

    /// <summary>Puls briefs a specialist with a short note: what the user wants, and how they come across.</summary>
    [Test]
    public void Puls_BriefsTheSpecialist_WithWhatTheUserWants_AndHowTheySound()
    {
        // The task is a note in a fixed shape; the mood is a read of the typing.
        string router = Prompt("agent/puls/voice.md");
        string afrikaans = Prompt("agent/puls/voice-af.md");
        string english = Prompt("agent/puls/voice-en.md");

        foreach (string label in new[]
                 {
                     "**Vir:**", "**Wil hê:**", "**Vir hoeveel:**", "**Klink:**", "**Waar:**", "**Reeds gesê:**"
                 })
            Assert.That(afrikaans, Does.Contain(label), "the note's labels, in Afrikaans");
        foreach (string label in new[]
                 {
                     "**For:**", "**Wants:**", "**Serves:**", "**Sounds:**", "**Where:**", "**Already said:**"
                 })
            Assert.That(english, Does.Contain(label), "the note's labels, in English");
        Assert.That(router, Does.Contain("a guess from the typing").And.Contain("never a diagnosis"),
            "the guard rails");
        Assert.That(afrikaans, Does.Contain("neutraal"), "when the typing says nothing");
        Assert.That(english, Does.Contain("neutral"), "when the typing says nothing");
        Assert.That(router, Does.Contain("[stemming: af]").And.Contain("no `Sounds`"),
            "the mood can be switched off for a turn");
    }

    /// <summary>No file holds a hand-off line to copy, in either language: Puls words it itself each time.</summary>
    [TestCase("agent/puls/voice.md")]
    [TestCase("agent/puls/voice-af.md")]
    [TestCase("agent/puls/voice-en.md")]
    public void NoFile_HoldsAFixedHandOffLine(string file)
    {
        foreach (string line in new[]
                 {
                     "Ek gee dit gou", "hy's nou-nou terug", "My kok is terug", "I'm handing this", "he'll be back",
                     "My cook is back"
                 })
            Assert.That(Prompt(file), Does.Not.Contain(line), file);
    }

    /// <summary>Each voice says what the hand-off holds, and that the words are Puls's own.</summary>
    [TestCase("agent/puls/voice-af.md", "in jou eie woorde, nooit 'n vaste sin nie")]
    [TestCase("agent/puls/voice-en.md", "in your own words, never a set phrase")]
    public void EachVoice_LeavesTheHandOffToPuls(string file, string words) =>
        Assert.That(string.Join(' ', Prompt(file).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            Does.Contain(words));

    /// <summary>The English voice shows no Afrikaans line to copy, which is what went wrong.</summary>
    [TestCase("Ek gee dit gou")]
    [TestCase("**Wil hê:**")]
    public void TheEnglishVoice_HasNoAfrikaansToCopy(string words) =>
        Assert.That(Prompt("agent/puls/voice-en.md"), Does.Not.Contain(words));
}
