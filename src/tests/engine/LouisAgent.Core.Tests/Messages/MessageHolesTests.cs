using LouisAgent.Composition.Messages;
using LouisAgent.Testing.Messages;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Core.Tests.Messages;

/// <summary>
/// ADR 0119, Louis: "WHY IS THIS AN ANONOMOUS TYPE?". A message's holes are a record the member names with
/// <c>[Holes]</c>, and the check holds its properties equal to the template's holes, so a misspelt hole fails here
/// rather than when the message is first sent.
/// </summary>
public class MessageHolesTests
{
    /// <summary>The holes of <see cref="SampleMessage.Matching"/>.</summary>
    private sealed record MatchingHoles(string Tool, int Count);

    /// <summary>A record that misspells one hole and misses another.</summary>
    private sealed record MisspeltHoles(string Tol);

    /// <summary>A message enum with a member of each kind.</summary>
    private enum SampleMessage
    {
        [Description("Called {Tool} {Count} times.")]
        [Holes(typeof(MatchingHoles))]
        Matching,

        [Description("Called {Tool} {Count} times.")]
        [Holes(typeof(MisspeltHoles))]
        Misspelt,

        [Description("Nothing to fill.")] Plain,
    }

    /// <summary>A record whose properties are the template's holes passes; a member with no holes needs none.</summary>
    [Test]
    public void AMatchingRecord_HasNoProblem() =>
        Assert.That(MessageHoles.Problems([typeof(SampleMessage)]), Has.None.Contain(nameof(SampleMessage.Matching))
            .And.None.Contain(nameof(SampleMessage.Plain)));

    /// <summary>A record that misspells a hole is named, with what the template wants and what the record has.</summary>
    [Test]
    public void AMisspeltRecord_IsNamed() =>
        Assert.That(MessageHoles.Problems([typeof(SampleMessage)]),
            Has.One.Contain("SampleMessage.Misspelt").And.Contain("Count, Tool").And.Contain("Tol"));

    /// <summary>A record's values fill the template by name, as the anonymous objects did.</summary>
    [Test]
    public void ARecord_FillsTheHoles()
    {
        var called = new MatchingHoles("Offer", 2);

        Assert.That(MessageText.Format(SampleMessage.Matching, called), Is.EqualTo("Called Offer 2 times."));
    }
}
