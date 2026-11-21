using System.Reflection;
using LouisAgent.Composition.Web.Reading;

namespace LouisAgent.Core.Web.Tests.Fetching;

/// <summary>
/// F166.3: a pack reads a page's photo with the framework's <see cref="LeadImage"/>, as it reads a page's words with
/// <see cref="HtmlText"/>, so Puls's recipes pack, outside louis-agent, needs no copy of it (Louis's pick A of
/// finding 10).
/// </summary>
public class LeadImageOpenToAPackTests
{
    /// <summary>The class and its one reading method are public; what it reads with stays its own.</summary>
    [Test]
    public void ThePagesPhoto_CanBeReadByAPack()
    {
        var of = typeof(LeadImage).GetMethod(nameof(LeadImage.Of), BindingFlags.Public | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(LeadImage).IsPublic, Is.True);
            Assert.That(of, Is.Not.Null);
        });
    }
}
