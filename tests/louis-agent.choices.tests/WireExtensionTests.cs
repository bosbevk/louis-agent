namespace LouisAgent.Choices.Tests;

// F18 step 7.5.2, the pattern everywhere (Louis, 2026-10-27: "the system should work like this everywhere"): in with SettingChoice.TryParse, out with .Wire(), the text on [WireName].
public class WireExtensionTests
{
    public enum Mode
    {
        [WireName("fast-mode")] FastMode,
        Plain,
    }

    [Test]
    public void AMemberWritesItsWireName_OrItsOwnNameInLowerCase()
    {
        Assert.That(Mode.FastMode.Wire(), Is.EqualTo("fast-mode"));
        Assert.That(Mode.Plain.Wire(), Is.EqualTo("plain"));
    }

    [Test]
    public void ItIsTheSameTextSettingChoiceWrites_ForEveryMember()
    {
        foreach (var member in Enum.GetValues<Mode>())
            Assert.That(member.Wire(), Is.EqualTo(SettingChoice.Wire(member)), member.ToString());
    }

    [Test]
    public void AValueTheEnumDoesNotDefine_IsRefusedNamingTheEnum() =>
        Assert.That(() => ((Mode)99).Wire(), Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contain(nameof(Mode)));
}
