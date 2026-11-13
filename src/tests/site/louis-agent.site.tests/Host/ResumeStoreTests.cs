using LouisAgent.Site.Content;
using LouisAgent.SiteApi.Resumes;

namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// F142, Louis's pick of three: the CV is kept as a file on the API, so an edit in the admin shows at once. The
/// embedded <c>cv.json</c> is the starting copy and the fallback, and with nowhere to keep a file nothing is saved.
/// </summary>
public class ResumeStoreTests
{
    /// <summary>A folder of this test's own, gone when it ends.</summary>
    private static string NewFolder() =>
        Directory.CreateTempSubdirectory("louis-agent-cv-").FullName;

    /// <summary>With no saved file, the CV is the one the site was built with.</summary>
    [Test]
    public void WithNothingSaved_TheCvIsTheEmbeddedOne()
    {
        string folder = NewFolder();
        try
        {
            var store = new ResumeStore(folder);

            Assert.That(store.Read().Basics.Name, Is.EqualTo(Cv.Resume.Basics.Name));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>A saved CV is the one read from then on, by this store and the next one on the same folder.</summary>
    [Test]
    public void ASavedCv_IsReadBack()
    {
        string folder = NewFolder();
        try
        {
            var edited = Cv.Resume with { Basics = Cv.Resume.Basics with { Label = "Edited in the admin" } };

            new ResumeStore(folder).Save(edited);

            Assert.Multiple(() =>
            {
                Assert.That(new ResumeStore(folder).Read().Basics.Label, Is.EqualTo("Edited in the admin"));
                Assert.That(File.Exists(Path.Combine(folder, "cv.json")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>With no folder set there is nowhere to save, and the CV stays the embedded one.</summary>
    [Test]
    public void WithNoFolder_TheStoreCantSave()
    {
        var store = new ResumeStore(folder: null);

        Assert.Multiple(() =>
        {
            Assert.That(store.CanSave, Is.False);
            Assert.That(store.Read().Basics.Name, Is.EqualTo(Cv.Resume.Basics.Name));
        });
    }

    /// <summary>The folder comes from SITE_DATA_DIRECTORY, none when it isn't set.</summary>
    [Test]
    public void TheFolder_ComesFromTheSetting()
    {
        var set = ResumeStore.FromEnvironment(name => name == "SITE_DATA_DIRECTORY" ? "/data" : null);
        var unset = ResumeStore.FromEnvironment(_ => null);

        Assert.Multiple(() =>
        {
            Assert.That(set.CanSave, Is.True);
            Assert.That(unset.CanSave, Is.False);
        });
    }
}
