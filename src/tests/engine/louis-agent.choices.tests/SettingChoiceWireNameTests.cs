using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Choices.Tests;

/// <summary>
/// A name that repeats is an enum, but where it travels it stays the text it always was (<c>fast-mode</c>,
/// <c>puls/voice-af</c>) (F18 step 7.5.0, on .NET's own attribute since 7.5.2; Louis: "Use .NET's
/// JsonStringEnumMemberName"). A member carries that text as <c>[WireName]</c> (our attribute, .NET's
/// <c>JsonStringEnumMemberName</c> underneath), which .NET's JSON writer reads too, so the parser and the writer can't
/// disagree. <c>SettingChoice</c> parses it once where it enters (as well as the member's own name, case ignored),
/// writes it back with <c>Wire</c>, and lists it in <c>Names</c>. A member with none travels as its name in lower case.
/// </summary>
public class SettingChoiceWireNameTests
{
    /// <summary>Members with wire names that aren't their own names, and one that has none.</summary>
    public enum Mode
    {
        [WireName("fast-mode")] FastMode,
        [WireName("puls/voice-af")] VoiceAf,
        Plain,
    }

    /// <summary>
    /// An enum written by a snake_case JSON converter: a two-word member without the attribute would be written
    /// `two_words` there, but `twowords` by SettingChoice, so such a member carries the attribute.
    /// </summary>
    public enum Written
    {
        Plain,
        [WireName("two_words")] TwoWords,
        [WireName("held_out")] HeldOut,
    }

    /// <summary>The wire name parses however it is written, and so does the member's own name.</summary>
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

    /// <summary>A member marked with .NET's own attribute, not ours.</summary>
    public enum DotNetMarked
    {
        [JsonStringEnumMemberName("by-dotnet")]
        ByDotNet,
    }

    /// <summary>
    /// SettingChoice reads .NET's attribute (WireName is built on it), so a member someone marks with .NET's own name
    /// works too.
    /// </summary>
    [Test]
    public void AMemberMarkedWithDotNetsOwnAttribute_ParsesAndWritesBackToo()
    {
        Assert.That(SettingChoice.TryParse("by-dotnet", out DotNetMarked? parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(DotNetMarked.ByDotNet));
        Assert.That(SettingChoice.Wire(DotNetMarked.ByDotNet), Is.EqualTo("by-dotnet"));
    }

    /// <summary>A number, a near miss or anything else names no member.</summary>
    [TestCase("1")]
    [TestCase("puls_only")]
    [TestCase("pulse")]
    public void ANumberOrAnythingElse_IsUnknown(string text) =>
        Assert.That(SettingChoice.TryParse(text, out Mode? _), Is.False);

    /// <summary>Wire gives the wire name back, and a member without one its name in lower case.</summary>
    [Test]
    public void WireWritesTheTextBack_TheMembersLowerCaseNameWhenItHasNone()
    {
        Assert.That(SettingChoice.Wire(Mode.FastMode), Is.EqualTo("fast-mode"));
        Assert.That(SettingChoice.Wire(Mode.VoiceAf), Is.EqualTo("puls/voice-af"));
        Assert.That(SettingChoice.Wire(Mode.Plain), Is.EqualTo("plain"));
    }

    /// <summary>What Wire writes parses back to the same member, for every member.</summary>
    [Test]
    public void ParsingWhatWasWritten_GivesTheMemberBack()
    {
        foreach (var member in Enum.GetValues<Mode>())
        {
            Assert.That(SettingChoice.TryParse(SettingChoice.Wire(member), out Mode? back), Is.True, member.ToString());
            Assert.That(back, Is.EqualTo(member));
        }
    }

    /// <summary>A message lists the wire names, in the enum's order.</summary>
    [Test]
    public void TheNamesAMessageLists_AreTheWireNames_InTheEnumsOrder() =>
        Assert.That(SettingChoice.Names<Mode>(), Is.EqualTo(new[] { "fast-mode", "puls/voice-af", "plain" }));

    /// <summary>
    /// The agreement test: what SettingChoice writes is what a snake_case JSON writer writes, for every member. If they
    /// ever differ, a stored run and a setting would name one member two ways.
    /// </summary>
    [Test]
    public void WhatSettingChoiceWrites_IsWhatTheJsonWriterWrites_ForEveryMember()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        foreach (var member in Enum.GetValues<Written>())
            Assert.That(SettingChoice.Wire(member),
                Is.EqualTo(JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, options))),
                member.ToString());
        foreach (var member in Enum.GetValues<Mode>())
            Assert.That(SettingChoice.Wire(member),
                Is.EqualTo(JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, options))),
                member.ToString());
    }
}
