using System.Text.Json;
using LouisAgent.Web.Components.Site.Meta.Schema;

namespace LouisAgent.Web.Tests.Components.Site.Meta;

/// <summary>
/// The structured data a page carries (F123) is written as JSON that stays inside its script element, whatever words
/// it holds.
/// </summary>
public class SchemaJsonTests
{
    /// <summary>Words that would close the script can't: the JSON escapes the angle brackets.</summary>
    [Test]
    public void AClosingTagInTheWords_StaysInsideTheScript()
    {
        string json = SchemaJson.Write(new SchemaWebSite("</script><b>x</b>", "https://example.test/"));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("</script>"));
            Assert.That(JsonDocument.Parse(json).RootElement.GetProperty("name").GetString(),
                Is.EqualTo("</script><b>x</b>"));
        });
    }
}
