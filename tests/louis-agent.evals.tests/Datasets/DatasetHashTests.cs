using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Tests.Datasets;

// The dataset's hash (F18 step 3a): over scenarios/ only, the same on Windows and Linux. Written first: the hash threw
// NotImplementedException on the red run.
public class DatasetHashTests
{
    [Test]
    public void ThePinnedHash_OfAKnownFolder()
    {
        // Pinned from an independent Python run of the same rule: files sorted by path, each path and its content followed by a NUL,
        // content with CRLF read as LF. The CRLF file is the point: a Windows checkout hashes like Linux's.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", "{\"id\":\"a\"}\r\n")
            .Write("scenarios/held-out/b.json", "{\"id\":\"b\"}\n");

        Assert.That(DatasetHash.Of(Path.Combine(folder.Root, "scenarios")),
            Is.EqualTo("1767ae240523b249201b54411ae07a500e18a8fb1e28e5857c4036434ed82043"));
    }

    [Test]
    public void CrlfAndLfHashTheSame()
    {
        using var crlf = new DatasetFolder().Write("scenarios/dev/a.json", "{\r\n  \"id\": \"a\"\r\n}\r\n");
        using var lf = new DatasetFolder().Write("scenarios/dev/a.json", "{\n  \"id\": \"a\"\n}\n");

        Assert.That(DatasetHash.Of(Path.Combine(crlf.Root, "scenarios")),
            Is.EqualTo(DatasetHash.Of(Path.Combine(lf.Root, "scenarios"))));
    }

    [Test]
    public void TheManifestIsNotHashed()
    {
        using var folder = new DatasetFolder().Write("scenarios/dev/a.json", "{\"id\":\"a\"}\n");
        string before = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));

        folder.Write("dataset.json", "{\"version\": 2}");

        Assert.That(DatasetHash.Of(Path.Combine(folder.Root, "scenarios")), Is.EqualTo(before));
    }

    [Test]
    public void AFileMovedToAnotherSplitChangesTheHash()
    {
        using var folder = new DatasetFolder().Write("scenarios/dev/a.json", "{\"id\":\"a\"}\n");
        string before = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));

        Directory.CreateDirectory(Path.Combine(folder.Root, "scenarios", "held-out"));
        File.Move(Path.Combine(folder.Root, "scenarios", "dev", "a.json"),
            Path.Combine(folder.Root, "scenarios", "held-out", "a.json"));

        Assert.That(DatasetHash.Of(Path.Combine(folder.Root, "scenarios")), Is.Not.EqualTo(before));
    }
}
