using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Files;
using LouisAgent.Composition.Pricing;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Core.Tests.Composition.Packs.Files;

/// <summary>
/// What a pack may ask about the session's files (F171.1 step 4): a host that says nothing of files has none, and a
/// file's type is one of the five a model can read, by the name the wire gives it.
/// </summary>
public class PackContextFilesTests
{
    /// <summary>A host that says nothing of files, as every host written before them.</summary>
    private sealed class SaysNothingOfFiles : IPackContext
    {
        public string WorkspaceRoot => throw new NotSupportedException();

        public IWebBrowser? Browser => null;

        public IWebSearchProvider WebSearch => throw new NotSupportedException();

        public IPageRenderer Renderer => throw new NotSupportedException();

        public ICurrencyRates? CurrencyRates => null;

        public IPageFetcher NewFetcher() => throw new NotSupportedException();
    }

    /// <summary>A host that says nothing of files finds none, and forgets one without complaint.</summary>
    [Test]
    public void AHostThatSaysNothingOfFiles_FindsNone_AndForgetsWithoutComplaint()
    {
        IPackContext context = new SaysNothingOfFiles();

        Assert.Multiple(() =>
        {
            Assert.That(context.Files.Find("account.jpg"), Is.Null);
            Assert.DoesNotThrow(() => context.Files.Forget("account.jpg"));
        });
    }

    /// <summary>Each of the five types a model reads, by its wire name, reads in and writes back the same.</summary>
    [TestCase("image/jpeg", FileMediaType.Jpeg)]
    [TestCase("image/png", FileMediaType.Png)]
    [TestCase("image/gif", FileMediaType.Gif)]
    [TestCase("image/webp", FileMediaType.Webp)]
    [TestCase("application/pdf", FileMediaType.Pdf)]
    public void AFilesType_ReadsByItsWireName_AndWritesBackTheSame(string wire, FileMediaType type)
    {
        bool read = SettingChoice.TryParse(wire, out FileMediaType? parsed);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.True);
            Assert.That(parsed, Is.EqualTo(type));
            Assert.That(type.Wire(), Is.EqualTo(wire));
        });
    }

    /// <summary>
    /// No other kind is taken: not text, not a picture a model can't read, not a member's number. A member's own name
    /// is, case ignored, as every choice reads with the one parser (ADR 0024).
    /// </summary>
    [TestCase("text/plain")]
    [TestCase("image/svg+xml")]
    [TestCase("image/tiff")]
    [TestCase("4")]
    public void AnyOtherWord_IsNotAFilesType(string wire) =>
        Assert.That(SettingChoice.TryParse(wire, out FileMediaType? _), Is.False);
}
