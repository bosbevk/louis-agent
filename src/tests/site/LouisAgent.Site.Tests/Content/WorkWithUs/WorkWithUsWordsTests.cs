using LouisAgent.Composition.Words;
using LouisAgent.Site.Content.WorkWithUs;

namespace LouisAgent.Site.Tests.Content.WorkWithUs;

/// <summary>
/// The Work with us page's words come from a dictionary factory (ADR 0132), as the trust page's do: each of its six
/// lists is keyed by an enum of its parts, in a <c>Parts</c> folder beside the content, so every part has its words.
/// </summary>
public class WorkWithUsWordsTests
{
    /// <summary>The longest sentence the site's writing rules allow, in words.</summary>
    private const int LongestSentence = 25;

    /// <summary>Each list, by its member's name.</summary>
    [TestCase(nameof(WorkWithUsWords.WhatWeBuild))]
    [TestCase(nameof(WorkWithUsWords.SeeItWorking))]
    [TestCase(nameof(WorkWithUsWords.HowWeWork))]
    [TestCase(nameof(WorkWithUsWords.HowYouPay))]
    [TestCase(nameof(WorkWithUsWords.WhatYouGet))]
    [TestCase(nameof(WorkWithUsWords.RunningCosts))]
    public void EachList_IsAPartWordsOverItsOwnParts(string list)
    {
        var type = typeof(WorkWithUsWords).GetField(list)!.FieldType;

        Assert.Multiple(() =>
        {
            Assert.That(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PartWords<,>), Is.True,
                $"{list} is {type.Name}");
            Assert.That(type.GetGenericArguments().FirstOrDefault()?.Namespace,
                Is.EqualTo("LouisAgent.Site.Content.WorkWithUs.Parts"), list);
        });
    }

    /// <summary>The work runs in seven steps, from the first call to the handover, and is paid in two halves.</summary>
    [Test]
    public void TheWork_IsSevenSteps_PaidInTwoHalves()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WorkWithUsWords.HowWeWork.InOrder, Has.Count.EqualTo(7));
            Assert.That(WorkWithUsWords.HowWeWork.InOrder[0].Title, Is.EqualTo("Discovery call."));
            Assert.That(WorkWithUsWords.HowWeWork.InOrder[^1].Title, Is.EqualTo("Handover."));
            Assert.That(WorkWithUsWords.HowYouPay.InOrder.Select(half => half.Share),
                Is.EqualTo(new[] { "50%", "50%" }));
        });
    }

    /// <summary>Every sentence a visitor reads in a tile or a step is short, as the site's writing rules ask.</summary>
    [Test]
    public void EverySentence_IsShort()
    {
        var texts = WorkWithUsWords.WhatWeBuild.InOrder.Select(tile => tile.Text)
            .Concat(WorkWithUsWords.SeeItWorking.InOrder.Select(tile => tile.Text))
            .Concat(WorkWithUsWords.WhatYouGet.InOrder.Select(tile => tile.Text))
            .Concat(WorkWithUsWords.RunningCosts.InOrder.Select(tile => tile.Text))
            .Concat(WorkWithUsWords.HowWeWork.InOrder.Select(step => step.Text))
            .Concat(WorkWithUsWords.HowYouPay.InOrder.Select(half => half.When));

        var sentences = texts.SelectMany(text => text.Split('.', StringSplitOptions.RemoveEmptyEntries));

        Assert.That(sentences.Select(sentence => sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length),
            Is.All.LessThanOrEqualTo(LongestSentence));
    }
}
