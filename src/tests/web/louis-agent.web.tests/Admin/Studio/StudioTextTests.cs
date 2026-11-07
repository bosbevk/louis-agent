using LouisAgent.Web.Admin.Studio;

namespace LouisAgent.Web.Tests.Admin.Studio;

/// <summary>The studio's sentences: an eval's drafts are counted in words a person reads.</summary>
public class StudioTextTests
{
    /// <summary>One draft is a draft; more are drafts.</summary>
    [TestCase(1, "1 draft")]
    [TestCase(2, "2 drafts")]
    public void TheDrafts_AreCounted(int count, string text) => Assert.That(StudioText.Drafts(count), Is.EqualTo(text));
}
