using System.Text.Json;
using LouisAgent.Core.Web.Fetching;
using LouisAgent.Composition.Web.Reading;

namespace LouisAgent.Core.Tests.Web.StructuredData;

/// <summary>
/// A page's JSON-LD blocks come back parsed, in page order, whatever the page does around them. Written first: the
/// reader was a regex inside LeadImage, so no other caller could use it.
/// </summary>
public class JsonLdBlocksTests
{
    /// <summary>Wraps some JSON in a script tag with the type you give, JSON-LD unless you say otherwise.</summary>
    private static string Block(string json, string type = "application/ld+json") =>
        $"<script type=\"{type}\">{json}</script>";

    /// <summary>The documents are the caller's to dispose, so every test hands them back here.</summary>
    private static void DisposeAll(IReadOnlyList<JsonDocument> blocks)
    {
        foreach (var block in blocks) block.Dispose();
    }

    /// <summary>Two blocks on a page come back in the order the page has them, each its own object.</summary>
    [Test]
    public void TwoBlocks_ComeBackInPageOrder_EachAnObject()
    {
        string html = $"<html>{Block("""{"name":"first"}""")}<p>between</p>{Block("""{"name":"second"}""")}</html>";

        var blocks = JsonLdBlocks.In(html);

        try
        {
            Assert.That(blocks.Select(b => b.RootElement.GetProperty("name").GetString()),
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(blocks.All(b => b.RootElement.ValueKind == JsonValueKind.Object), Is.True);
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>An escaped closing tag inside a string doesn't end the block; only the real one does.</summary>
    [Test]
    public void AnEscapedClosingScriptInsideAString_IsReadWhole()
    {
        string html = Block("""{"description":"a <\/script> inside","name":"whole"}""") + "<p>after</p>";

        var blocks = JsonLdBlocks.In(html);

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].RootElement.GetProperty("description").GetString(),
                Is.EqualTo("a </script> inside"));
            Assert.That(blocks[0].RootElement.GetProperty("name").GetString(), Is.EqualTo("whole"));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>A page that writes its quotes and ampersands as HTML entities still gives you the JSON.</summary>
    [Test]
    public void HtmlEntities_AreDecodedBeforeParsing()
    {
        string html = Block("{&quot;name&quot;:&quot;Pap &amp; wors&quot;}");

        var blocks = JsonLdBlocks.In(html);

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].RootElement.GetProperty("name").GetString(), Is.EqualTo("Pap & wors"));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>
    /// A script tag's JSON isn't HTML, so a page that writes a real quote as <c>&amp;quot;</c> inside a string (and
    /// the rest of the block as plain JSON) must not lose the block. The entity stays as the page wrote it; reading
    /// it is the caller's job.
    /// </summary>
    [Test]
    public void AnEntityInsideAString_DoesNotDropTheBlock()
    {
        string html = Block("""{"@type":"Recipe","recipeInstructions":"Grease a 9&quot; pie dish"}""");

        var blocks = JsonLdBlocks.In(html);

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].RootElement.GetProperty("recipeInstructions").GetString(),
                Is.EqualTo("Grease a 9&quot; pie dish"));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>
    /// The lead image reads its address from this data, so an address written <c>?w=1&amp;amp;h=2</c> in it is
    /// <c>?w=1&amp;h=2</c>: the file the page means.
    /// </summary>
    [Test]
    public void AnEntityInTheLeadImageAddress_IsReadAsItsCharacter()
    {
        const string json = """{"@type":"Recipe","image":"https://r.example.co.za/a.jpg?w=1&amp;h=2"}""";
        string html = $"<html><head><title>Tart</title>{Block(json)}</head></html>";

        string? image = LeadImage.Of(html, new Uri("https://r.example.co.za/tart/"), "Tart");

        Assert.That(image, Is.EqualTo("![Tart](https://r.example.co.za/a.jpg?w=1&h=2)"));
    }

    /// <summary>A broken block is left out without a fuss, and the good one after it still comes back.</summary>
    [Test]
    public void AMalformedBlock_IsSkipped_AndTheNextOneIsRead()
    {
        string html = Block("{\"name\": oops") + Block("""{"name":"good"}""");

        IReadOnlyList<JsonDocument> blocks = [];
        Assert.DoesNotThrow(() => blocks = JsonLdBlocks.In(html));

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].RootElement.GetProperty("name").GetString(), Is.EqualTo("good"));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>A charset or capitals in the type still make it JSON-LD; a plain script never is.</summary>
    [TestCase("application/ld+json; charset=utf-8", 1)]
    [TestCase("APPLICATION/LD+JSON", 1)]
    [TestCase("text/javascript", 0)]
    public void TheTypeAttribute_IsReadLenientlyButOnlyForLdJson(string type, int expected)
    {
        var blocks = JsonLdBlocks.In(Block("""{"a":1}""", type));

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(expected));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>Single quotes around the type count the same as double ones.</summary>
    [Test]
    public void ASingleQuotedTypeAttribute_Counts()
    {
        var blocks = JsonLdBlocks.In("""<script type='application/ld+json'>{"a":1}</script>""");

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }

    /// <summary>A tag that only starts with "script" isn't a script, whatever its type says.</summary>
    [Test]
    public void ALongerTagNameThatStartsWithScript_DoesNotCount()
    {
        var blocks = JsonLdBlocks.In("""<scripts type="application/ld+json">{"a":1}</script>""");

        Assert.That(blocks, Is.Empty);
    }

    /// <summary>A page with only ordinary scripts gives you nothing.</summary>
    [Test]
    public void APageWithNoBlock_GivesAnEmptyList()
    {
        var blocks = JsonLdBlocks.In("<html><script>var a = 1;</script><p>no data</p></html>");

        Assert.That(blocks, Is.Empty);
    }

    /// <summary>A block that's a list comes back as one document; walking the list is the caller's job.</summary>
    [Test]
    public void ABlockThatIsAnArray_ComesBackAsOneDocumentWithAnArrayRoot()
    {
        var blocks = JsonLdBlocks.In(Block("""[{"name":"a"},{"name":"b"}]"""));

        try
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(blocks[0].RootElement.GetArrayLength(), Is.EqualTo(2));
        }
        finally
        {
            DisposeAll(blocks);
        }
    }
}
