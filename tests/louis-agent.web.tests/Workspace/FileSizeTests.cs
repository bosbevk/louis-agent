using LouisAgent.Web.Workspace;

namespace LouisAgent.Web.Tests.Workspace;

// W2b: a file's size in the viewer's header: bytes, then KB and MB to one decimal.
public class FileSizeTests
{
    [TestCase(512, "512 B")]
    [TestCase(1536, "1.5 KB")]
    [TestCase(2048, "2 KB")]
    [TestCase(3 * 1024 * 1024, "3 MB")]
    public void ASize_IsSaidInItsUnit(long bytes, string expected) =>
        Assert.That(FileSize.Format(bytes), Is.EqualTo(expected));
}
