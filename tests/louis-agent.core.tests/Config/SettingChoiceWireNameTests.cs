using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tests.Config;

// F18 step 7.5.0 (Louis, 2026-10-27: "strings that repeat must be made enum", ADR 0043): a name that repeats is an enum, but where it travels it stays the text it always was (`puls-only`, `puls/voice-af`).
// A member carries that text as [WireName]; SettingChoice parses it once where it enters (as well as the member's own name, case ignored), writes it back with Wire, and lists it in Names. A member with
// none travels as its name in lower case, as before. Written first: the attribute was a stub and TryParse did not read it.
public class SettingChoiceWireNameTests
{
    public enum Mode
    {
        [WireName("puls-only")] PulsOnly,
        [WireName("puls/voice-af")] VoiceAf,
        Plain,
    }

    [TestCase("puls-only", Mode.PulsOnly)]
    [TestCase("PULS-ONLY", Mode.PulsOnly)]
    [TestCase(" puls/voice-af ", Mode.VoiceAf)]
    [TestCase("PulsOnly", Mode.PulsOnly, TestName = "TheMembersOwnNameStillParses")]
    [TestCase("plain", Mode.Plain)]
    public void AWireName_OrTheMembersName_ParsesIgnoringCase(string text, Mode expected)
    {
        Assert.That(SettingChoice.TryParse(text, out Mode? choice), Is.True);
        Assert.That(choice, Is.EqualTo(expected));
    }

    [TestCase("1")]
    [TestCase("puls_only")]
    [TestCase("puls")]
    public void ANumberOrAnythingElse_IsUnknown(string text) => Assert.That(SettingChoice.TryParse(text, out Mode? _), Is.False);

    [Test]
    public void WireWritesTheTextBack_TheMembersLowerCaseNameWhenItHasNone()
    {
        Assert.That(SettingChoice.Wire(Mode.PulsOnly), Is.EqualTo("puls-only"));
        Assert.That(SettingChoice.Wire(Mode.VoiceAf), Is.EqualTo("puls/voice-af"));
        Assert.That(SettingChoice.Wire(Mode.Plain), Is.EqualTo("plain"));
    }

    [Test]
    public void ParsingWhatWasWritten_GivesTheMemberBack()
    {
        foreach (var member in Enum.GetValues<Mode>())
        {
            Assert.That(SettingChoice.TryParse(SettingChoice.Wire(member), out Mode? back), Is.True, member.ToString());
            Assert.That(back, Is.EqualTo(member));
        }
    }

    [Test]
    public void TheNamesAMessageLists_AreTheWireNames_InTheEnumsOrder() =>
        Assert.That(SettingChoice.Names<Mode>(), Is.EqualTo(new[] { "puls-only", "puls/voice-af", "plain" }));
}
