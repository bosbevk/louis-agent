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
    [TestCase("agent/puls/voice-af.md", "Ek gee dit gou vir my kok")]
    [TestCase("agent/puls/voice-af.md", "Vir hoeveel mense kook jy?")]
    [TestCase("agent/puls/voice-af.md", "Ek soek vir eers nie winkels se pryse op nie")]
    [TestCase("agent/puls/voice-en.md", "**For:** cook")]
    [TestCase("agent/puls/voice-en.md", "**Wants:**")]
    [TestCase("agent/puls/voice-en.md", "**Serves:**")]
    [TestCase("agent/puls/voice-en.md", "I'm handing this to my cook")]
    [TestCase("agent/puls/voice-en.md", "How many people are you cooking for?")]
    [TestCase("agent/puls/voice-en.md", "I don't look up shops' prices for now")]
    public void EachVoice_HoldsItsOwnLines(string file, string words) =>
        Assert.That(Prompt(file), Does.Contain(words));

    /// <summary>The English voice shows no Afrikaans line to copy, which is what went wrong.</summary>
    [TestCase("Ek gee dit gou")]
    [TestCase("**Wil hê:**")]
    public void TheEnglishVoice_HasNoAfrikaansToCopy(string words) =>
        Assert.That(Prompt("agent/puls/voice-en.md"), Does.Not.Contain(words));
}
