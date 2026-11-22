using LouisAgent.Composition.Words;

namespace LouisAgent.Core.Tests.Composition.Words;

/// <summary>
/// Something's words, a page's or a pack's, come from a factory keyed by an enum of its parts (CL68.1, Louis: "put
/// this in a dictionary factory and make it a rule"): every part has its words, in the enum's order, or the class that
/// holds them doesn't load.
/// </summary>
public class PartWordsTests
{
    /// <summary>Three parts, for the tests.</summary>
    private enum Part
    {
        First,
        Second,
        Third
    }

    /// <summary>Every part gets its words, and they read in the enum's order.</summary>
    [Test]
    public void EveryPart_HasItsWords_InTheEnumsOrder()
    {
        var words = PartWords<Part, string>.Of(part => part switch
        {
            Part.First => "one",
            Part.Second => "two",
            Part.Third => "three",
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        });

        Assert.Multiple(() =>
        {
            Assert.That(words[Part.Second], Is.EqualTo("two"));
            Assert.That(words.Count, Is.EqualTo(3));
            Assert.That(words.InOrder, Is.EqualTo(new[] { "one", "two", "three" }));
        });
    }

    /// <summary>A part the factory has no words for is refused when the words are built, naming the part.</summary>
    [Test]
    public void APartWithoutWords_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => PartWords<Part, string>.Of(part => part switch
        {
            Part.First => "one",
            Part.Second => "two",
            _ => null!
        }));

        Assert.That(refused!.Message, Does.Contain(nameof(Part.Third)));
    }
}
