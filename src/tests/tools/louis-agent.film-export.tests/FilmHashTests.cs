using LouisAgent.FilmExport.Films;

namespace LouisAgent.FilmExport.Tests;

/// <summary>A stale export must fail a test, and the same film must hash the same on every machine.</summary>
public class FilmHashTests
{
    private string _root = null!;
    private FilmSource _film = null!;

    /// <summary>A film of two files in a folder of its own.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("film-hash-").FullName;
        _film = new FilmSource(FilmName.Short, "/", ".movie", TimeSpan.FromSeconds(36), ["a.razor", "b.css"]);
        File.WriteAllText(Path.Combine(_root, "a.razor"), "one\ntwo\n");
        File.WriteAllText(Path.Combine(_root, "b.css"), "three\n");
    }

    /// <summary>Removes the folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    /// <summary>Windows and CI check the same files out with different line endings, and must agree.</summary>
    [Test]
    public void LineEndings_DontChangeTheHash()
    {
        string before = FilmHash.Of(_film, _root);

        File.WriteAllText(Path.Combine(_root, "a.razor"), "one\r\ntwo\r\n");

        Assert.That(FilmHash.Of(_film, _root), Is.EqualTo(before));
    }

    /// <summary>A change to any of the film's files means its export is stale.</summary>
    [TestCase("a.razor")]
    [TestCase("b.css")]
    public void AChangeToAnyFile_ChangesTheHash(string file)
    {
        string before = FilmHash.Of(_film, _root);

        File.AppendAllText(Path.Combine(_root, file), "more\n");

        Assert.That(FilmHash.Of(_film, _root), Is.Not.EqualTo(before));
    }

    /// <summary>Text moved from one file to the next is still a change.</summary>
    [Test]
    public void MovingTextBetweenFiles_ChangesTheHash()
    {
        string before = FilmHash.Of(_film, _root);

        File.WriteAllText(Path.Combine(_root, "a.razor"), "one\n");
        File.WriteAllText(Path.Combine(_root, "b.css"), "two\nthree\n");

        Assert.That(FilmHash.Of(_film, _root), Is.Not.EqualTo(before));
    }

    /// <summary>Exporting one film leaves the other's line alone.</summary>
    [Test]
    public void Recording_KeepsTheOtherFilmsLine()
    {
        string text = FilmHash.Record(null, FilmSource.Short, "aaa");
        text = FilmHash.Record(text, FilmSource.Detail, "bbb");
        text = FilmHash.Record(text, FilmSource.Short, "ccc");

        Assert.That(FilmHash.Read(text, FilmName.Short), Is.EqualTo("ccc"));
        Assert.That(FilmHash.Read(text, FilmName.Detail), Is.EqualTo("bbb"));
    }

    /// <summary>A film that was never exported has no hash.</summary>
    [Test]
    public void AFilmNeverExported_HasNoHash()
    {
        Assert.That(FilmHash.Read(null, FilmName.Short), Is.Null);
        Assert.That(FilmHash.Read(FilmHash.Record(null, FilmSource.Detail, "bbb"), FilmName.Short), Is.Null);
    }
}
