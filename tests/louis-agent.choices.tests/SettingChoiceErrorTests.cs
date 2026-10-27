using System.Reflection;
using LouisAgent.Choices.Messages;

namespace LouisAgent.Choices.Tests;

// F18 step 7.5.2 (the review of 7.5.0): Wire on a value the enum doesn't define (`(Mode)99`) threw "Sequence contains no matching element", which names nothing. It is an ArgumentOutOfRangeException that
// names the enum and the value, and its text is a member of the project's own message enum (ADR 0009): this project references nothing, so it cannot use core's MessageText.
public class SettingChoiceErrorTests
{
    public enum Mode { Fast, Slow }

    [Test]
    public void WireOnAValueTheEnumDoesNotDefine_NamesTheEnumAndTheValue()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => SettingChoice.Wire((Mode)99))!;

        Assert.That(error.Message, Does.Contain("Mode").And.Contain("99"));
        Assert.That(error.Message, Does.Not.Contain("Sequence contains no matching element"));
    }

    [Test]
    public void EveryMessageMember_HasATemplate_WithWellFormedHoles()
    {
        foreach (var member in Enum.GetValues<ChoiceMessage>())
        {
            var template = typeof(ChoiceMessage).GetField(member.ToString())!.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

            Assert.That(template, Is.Not.Null.And.Not.Empty, member.ToString());
            Assert.That(template!.Count(c => c == '{'), Is.EqualTo(template.Count(c => c == '}')), member + ": braces match");
        }
    }
}
