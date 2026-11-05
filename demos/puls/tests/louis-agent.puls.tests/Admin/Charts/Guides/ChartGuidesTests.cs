using LouisAgent.Choices;
using LouisAgent.Puls.Admin.Charts.Guides;

namespace LouisAgent.Puls.Tests.Admin.Charts.Guides;

/// <summary>
/// Every measure a chart can show has its note (what it shows, good, bad, why, which way is better), and a chart names
/// its measure by the word the pages have always used.
/// </summary>
public class ChartGuidesTests
{
    /// <summary>No measure is left without a note, and no part of a note is empty.</summary>
    [Test]
    public void EveryMeasure_HasAWholeNote()
    {
        foreach (var topic in Enum.GetValues<ChartTopic>())
        {
            var guide = ChartGuides.For(topic);

            Assert.That(new[] { guide.What, guide.Good, guide.Bad, guide.Why, guide.Better },
                Has.All.Not.Empty, topic.ToString());
        }
    }

    /// <summary>The pages' words name the measures, whatever their case.</summary>
    [TestCase("cost", ChartTopic.Cost)]
    [TestCase("passat", ChartTopic.PassAt)]
    [TestCase("passhat", ChartTopic.PassHat)]
    [TestCase("Value", ChartTopic.Value)]
    public void AMeasure_IsNamedByItsWord(string word, ChartTopic topic)
    {
        Assert.That(SettingChoice.TryParse(word, out ChartTopic? parsed), Is.True, word);
        Assert.That(parsed, Is.EqualTo(topic));
    }
}
