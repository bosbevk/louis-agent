using LouisAgent.Composition.Words;
using LouisAgent.Site.Content.Trust;

namespace LouisAgent.Site.Tests.Content.Trust;

/// <summary>
/// The trust page's words come from a dictionary factory (ADR 0132): each of its five lists is keyed by an enum of its
/// facts, in a <c>Parts</c> folder beside the content, so every fact has its words and none is a bare literal list.
/// </summary>
public class TrustFactsTests
{
    /// <summary>Each list, by its member's name.</summary>
    [TestCase(nameof(TrustFacts.WhereItRuns))]
    [TestCase(nameof(TrustFacts.YourData))]
    [TestCase(nameof(TrustFacts.AiSafety))]
    [TestCase(nameof(TrustFacts.IfSomethingBreaks))]
    [TestCase(nameof(TrustFacts.BeforePayingCustomers))]
    public void EachList_IsAPartWordsOverItsOwnParts(string list)
    {
        var type = typeof(TrustFacts).GetField(list)!.FieldType;

        Assert.Multiple(() =>
        {
            Assert.That(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PartWords<,>), Is.True,
                $"{list} is {type.Name}");
            Assert.That(type.GetGenericArguments().FirstOrDefault()?.Namespace,
                Is.EqualTo("LouisAgent.Site.Content.Trust.Parts"), list);
        });
    }
}
