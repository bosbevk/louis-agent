using LouisAgent.Packs.Files.Web.Reading;

namespace LouisAgent.Packs.Files.Web.Tests.Reading;

/// <summary>A file's size in the viewer's header: bytes, then KB and MB to one decimal.</summary>
public class FileSizeTests
{
    /// <summary>A size is said in the largest unit that fits.</summary>
    [TestCase(512, "512 B")]
    [TestCase(1536, "1.5 KB")]
    [TestCase(2048, "2 KB")]
    [TestCase(3 * 1024 * 1024, "3 MB")]
    public void ASize_IsSaidInItsUnit(long bytes, string expected) =>
        Assert.That(FileSize.Format(bytes), Is.EqualTo(expected));
}
