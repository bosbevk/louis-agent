using LouisAgent.Core.Tools.Workspace.Listing;

namespace LouisAgent.Core.Tests.Tools.Workspace.Listing;

// S (CL15): a file's size as the listing tools say it: bytes, then KB, MB and GB to two decimals, never beyond GB.
public class FileSizeTextTests
{
    [TestCase(512, "512 B")]
    [TestCase(1536, "1.5 KB")]
    [TestCase(1024 * 1024 * 3, "3 MB")]
    [TestCase(1234567, "1.18 MB")]
    [TestCase(5L * 1024 * 1024 * 1024, "5 GB")]
    [TestCase(2048L * 1024 * 1024 * 1024, "2048 GB")]
    public void ASize_IsSaidInItsUnit(long bytes, string expected) =>
        Assert.That(FileSizeText.Format(bytes), Is.EqualTo(expected));
}
