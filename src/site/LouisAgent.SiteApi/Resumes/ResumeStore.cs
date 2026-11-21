using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;

namespace LouisAgent.SiteApi.Resumes;

/// <summary>
/// Keeps Louis's CV as a file on the host (F142, his pick of three): <c>cv.json</c> in the site's data folder, which
/// an edit in the admin replaces, so it shows at once with no deploy. Until one is saved, the CV is the one the site
/// was built with. With no folder set there is nowhere to keep a file, and it says so rather than lose an edit.
/// </summary>
/// <remarks>
/// A class, since it owns a file and a lock (ADR 0040). The file is written beside itself and moved into place, so a
/// read never meets half a file. It is read on each call: it is small, and an edit must show at once.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class ResumeStore(string? folder)
{
    /// <summary>The setting that names the folder the site's host keeps its own data in.</summary>
    public const string FolderSetting = "SITE_DATA_DIRECTORY";

    /// <summary>The file's name in the folder.</summary>
    private const string FileName = "cv.json";

    /// <summary>The file as a person can read it: indented, with its own letters and signs, not their codes.</summary>
    private static readonly JsonSerializerOptions FileJson = new(ResumeReader.Options)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Lock _gate = new();

    /// <summary>Whether there is a folder to keep an edit in.</summary>
    public bool CanSave => folder is not null;

    /// <summary>The store on the folder <see cref="FolderSetting"/> names; with none, one that can't save.</summary>
    public static ResumeStore FromEnvironment(Func<string, string?>? getVariable = null)
    {
        string? named = (getVariable ?? Environment.GetEnvironmentVariable)(FolderSetting);
        return new ResumeStore(string.IsNullOrWhiteSpace(named) ? null : named);
    }

    /// <summary>The saved CV, or the one the site was built with when none was saved.</summary>
    public Resume Read()
    {
        lock (_gate)
        {
            if (folder is null || !File.Exists(PathIn(folder))) return Cv.Resume;

            using var file = File.OpenRead(PathIn(folder));
            return ResumeReader.Read(file);
        }
    }

    /// <summary>Keeps this CV as the saved one. Call it only when <see cref="CanSave"/>.</summary>
    public void Save(Resume resume)
    {
        ArgumentNullException.ThrowIfNull(folder);
        lock (_gate)
        {
            Directory.CreateDirectory(folder);
            // Written beside the file and moved over it, so a read never meets half a file.
            string draft = PathIn(folder) + ".saving";
            File.WriteAllText(draft, JsonSerializer.Serialize(resume, FileJson));
            File.Move(draft, PathIn(folder), overwrite: true);
        }
    }

    /// <summary>The file's place in the folder.</summary>
    private static string PathIn(string folder) => Path.Combine(folder, FileName);
}
