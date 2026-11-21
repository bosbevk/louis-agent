using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>What a guide in the corner is doing, kept apart from the widget that draws it (FX135).</summary>
public class KeptGuideTests
{
    /// <summary>
    /// Unfolded, the panel is open, and one folded to its head comes back to its normal size, so an answer is never
    /// hidden; one made large stays large.
    /// </summary>
    [TestCase(ChatWindowSize.Minimised, ChatWindowSize.Normal)]
    [TestCase(ChatWindowSize.Maximised, ChatWindowSize.Maximised)]
    public void Unfolded_ThePanelIsOpen_AndNeverFolded(ChatWindowSize before, ChatWindowSize after)
    {
        var kept = new KeptGuide { Size = before };

        kept.Unfold();

        Assert.That((kept.Open, kept.Size), Is.EqualTo((true, after)));
    }
}
