using LouisAgent.Site.Components.Landing.Offer;

namespace LouisAgent.Site.Tests.Components.Landing.Offer;

/// <summary>
/// FX205: the landing's tape is read by a business owner, so it says what the agents did in plain words, never the
/// name of a tool the model calls.
/// </summary>
public class LandingTapeTests
{
    /// <summary>The tools the showroom's agents call, which a visitor would not know.</summary>
    private static readonly string[] ToolNames = ["ShowPage", "ShowCard", "SendLead", "AskFor", "Offer"];

    /// <summary>No item names a tool, in its words or in its mark.</summary>
    [Test]
    public void TheTape_SpeaksInPlainWords_NotToolNames()
    {
        var words = LandingTape.Items.Select(item => $"{item.Text} {item.Mark}").ToList();

        Assert.That(words.Where(line => ToolNames.Any(tool => line.Contains(tool, StringComparison.Ordinal))),
            Is.Empty);
    }
}
