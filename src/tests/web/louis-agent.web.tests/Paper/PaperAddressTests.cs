using LouisAgent.Web.Paper;

namespace LouisAgent.Web.Tests.Paper;

/// <summary>
/// FX154: beside a link's QR code on paper, its address is written short, for someone who would rather type it:
/// without the scheme, the www, a mail link's prefix or a slash at the end.
/// </summary>
public class PaperAddressTests
{
    /// <summary>An address as a person would write it down.</summary>
    [TestCase("https://www.linkedin.com/in/louismarais86/", "linkedin.com/in/louismarais86")]
    [TestCase("https://github.com/bosbevk", "github.com/bosbevk")]
    [TestCase("http://example.org/", "example.org")]
    [TestCase("mailto:louis.marais@live.com", "louis.marais@live.com")]
    [TestCase("https://wa.me/27663912470", "wa.me/27663912470")]
    public void AnAddress_IsWrittenShort(string address, string written) =>
        Assert.That(PaperAddress.Short(address), Is.EqualTo(written));
}
