using LouisAgent.Site.Resumes;

namespace LouisAgent.Site.Content;

/// <summary>
/// Louis's CV as the site holds it (F142): <c>cv.json</c>, embedded in the site, read once into the
/// <see cref="Resumes.Resume"/> the <c>/cv</c> page draws and, in F143, the guide reads.
/// </summary>
/// <remarks>
/// Single source of truth: the one file, read here and nowhere else, so the page and the guide can't tell two stories.
/// To change the CV, edit <c>Content/cv.json</c>; <c>CvContentTests</c> keeps out what a public page mustn't hold.
/// </remarks>
public static class Cv
{
    /// <summary>The file as embedded, word for word.</summary>
    public static string Json { get; } = ReadText();

    /// <summary>The CV, read once.</summary>
    public static Resume Resume { get; } = Read();

    /// <summary>His photo, which is the site's file and not the CV's data.</summary>
    public const string Photo = "images/louis-marais.jpg";

    /// <summary>Where he'd work, said beside where he lives. The page's own words, as the photo is.</summary>
    public const string Availability = "open to remote and hybrid";

    /// <summary>The embedded file's text.</summary>
    private static string ReadText()
    {
        using var reader = new StreamReader(Open());
        return reader.ReadToEnd();
    }

    /// <summary>The embedded file, read into the records.</summary>
    private static Resume Read()
    {
        using var stream = Open();
        return ResumeReader.Read(stream);
    }

    /// <summary>The embedded file, by its logical name.</summary>
    private static Stream Open() => typeof(Cv).Assembly.GetManifestResourceStream("cv.json")!;
}
