using System.Text.Json;
using LouisAgent.Core.Tools.Web.StructuredData;

namespace LouisAgent.Core.Tests.Tools.Web.StructuredData;

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
