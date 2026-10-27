using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Choices.Tests;

// F18 step 7.5.2 follow-up (Louis, 2026-10-27, card): our attribute is .NET's underneath, so the parser and the JSON writer read one thing and a name reads as what it means.
public class WireNameAttributeTests
{
    public enum Mode
    {
        [WireName("fast-mode")] FastMode,
        Plain,
    }

    private static readonly JsonSerializerOptions Options = new()
    { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) } };

    [Test]
    public void AMemberMarkedWireName_ParsesAndWritesBackItsName()
    {
        Assert.That(SettingChoice.TryParse("fast-mode", out Mode? parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(Mode.FastMode));
        Assert.That(SettingChoice.Wire(Mode.FastMode), Is.EqualTo("fast-mode"));
    }

    [Test]
    public void TheJsonWriterAndReader_UseTheSameName_UnderTheSnakeCaseConverter()
    {
        Assert.That(JsonSerializer.Serialize(Mode.FastMode, Options), Is.EqualTo("\"fast-mode\""));
        Assert.That(JsonSerializer.Deserialize<Mode>("\"fast-mode\"", Options), Is.EqualTo(Mode.FastMode));
    }

    [Test]
    public void WireName_IsDotNetsOwnAttributeUnderneath() =>
        Assert.That(typeof(WireNameAttribute).IsSubclassOf(typeof(JsonStringEnumMemberNameAttribute)), Is.True);
}
