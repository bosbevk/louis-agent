using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Choices;

namespace LouisAgent.Choices.Tests;

// F18 step 7.5.0, on .NET's own attribute since 7.5.2 (Louis: "Use .NET's JsonStringEnumMemberName"): a name that repeats is an enum, but where it travels it stays the text it always was (`fast-mode`, `puls/voice-af`).
// A member carries that text as [WireName] (our attribute, .NET's JsonStringEnumMemberName underneath), which .NET's JSON writer reads too, so the parser and the writer cannot disagree. SettingChoice parses it once where it enters (as well as the member's own
// name, case ignored), writes it back with Wire, and lists it in Names. A member with none travels as its name in lower case.
public class SettingChoiceWireNameTests
{
    public enum Mode
    {
        [WireName("fast-mode")] FastMode,
        [WireName("puls/voice-af")] VoiceAf,
        Plain,
    }

    /// <summary>The snake_case options the harness writes its enums with (EvalJson.SnakeCaseNoNulls): a two-word member without the attribute would be written `two_words` here, but `twowords` by SettingChoice, so such a member carries the attribute.</summary>
    public enum Written
    {
        Plain,
        [WireName("two_words")] TwoWords,
        [WireName("held_out")] HeldOut,
    }

    [TestCase("fast-mode", Mode.FastMode)]
    [TestCase("FAST-MODE", Mode.FastMode)]
    [TestCase(" puls/voice-af ", Mode.VoiceAf)]
    [TestCase("FastMode", Mode.FastMode, TestName = "TheMembersOwnNameStillParses")]
    [TestCase("plain", Mode.Plain)]
    public void AWireName_OrTheMembersName_ParsesIgnoringCase(string text, Mode expected)
    {
        Assert.That(SettingChoice.TryParse(text, out Mode? choice), Is.True);
        Assert.That(choice, Is.EqualTo(expected));
    }

    public enum DotNetMarked
    {
        [JsonStringEnumMemberName("by-dotnet")] ByDotNet,
    }

    // SettingChoice reads .NET's attribute (WireName is built on it), so a member someone marks with .NET's own name works too.
    [Test]
    public void AMemberMarkedWithDotNetsOwnAttribute_ParsesAndWritesBackToo()
    {
        Assert.That(SettingChoice.TryParse("by-dotnet", out DotNetMarked? parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(DotNetMarked.ByDotNet));
        Assert.That(SettingChoice.Wire(DotNetMarked.ByDotNet), Is.EqualTo("by-dotnet"));
    }

    [TestCase("1")]
    [TestCase("puls_only")]
    [TestCase("pulse")]
    public void ANumberOrAnythingElse_IsUnknown(string text) => Assert.That(SettingChoice.TryParse(text, out Mode? _), Is.False);

    [Test]
    public void WireWritesTheTextBack_TheMembersLowerCaseNameWhenItHasNone()
    {
        Assert.That(SettingChoice.Wire(Mode.FastMode), Is.EqualTo("fast-mode"));
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
        Assert.That(SettingChoice.Names<Mode>(), Is.EqualTo(new[] { "fast-mode", "puls/voice-af", "plain" }));

    // The agreement test: what SettingChoice writes is what the JSON writer writes, for every member, with the options the harness uses. If they ever differ, a stored run and a setting would name one member two ways.
    [Test]
    public void WhatSettingChoiceWrites_IsWhatTheJsonWriterWrites_ForEveryMember()
    {
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) } };

        foreach (var member in Enum.GetValues<Written>())
            Assert.That(SettingChoice.Wire(member), Is.EqualTo(JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, options))), member.ToString());
        foreach (var member in Enum.GetValues<Mode>())
            Assert.That(SettingChoice.Wire(member), Is.EqualTo(JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, options))), member.ToString());
    }
}
