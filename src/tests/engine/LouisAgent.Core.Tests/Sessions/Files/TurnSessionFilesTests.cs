using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Files;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Files;
using LouisAgent.Core.Web;

namespace LouisAgent.Core.Tests.Sessions.Files;

/// <summary>
/// F171.1 step 6: a pack's tool finds this turn's files through its pack context. The host sets them for the turn, as
/// it sets the text attachments, and the one context every session shares reads what is set when a tool asks.
/// </summary>
public class TurnSessionFilesTests
{
    /// <summary>The files a host keeps, as a dictionary: what the context should hand on, and nothing more.</summary>
    private sealed class KeptFiles(params SessionFile[] files) : ISessionFiles
    {
        private readonly Dictionary<string, SessionFile> _files = files.ToDictionary(file => file.Name);

        public SessionFile? Find(string name) => _files.GetValueOrDefault(name);

        public void Forget(string name) => _files.Remove(name);
    }

    /// <summary>The context a host builds once and hands every pack.</summary>
    private static IPackContext Context()
    {
        var options = new AgentOptions();
        return new ToolsetContext(options, WebAccess.Create(options));
    }

    /// <summary>A file of a few bytes.</summary>
    private static SessionFile File(string name, FileMediaType kind) => new(name, kind, new byte[] { 1, 2, 3 });

    /// <summary>Inside a turn whose host set two files, the context finds each, and nothing for another name.</summary>
    [Test]
    public void InATurnWithTwoFiles_TheContextFindsEach_AndNothingElse()
    {
        var context = Context();
        var photo = File("account.jpg", FileMediaType.Jpeg);
        var pdf = File("account.pdf", FileMediaType.Pdf);

        using var _ = SessionFiles.Use(new KeptFiles(photo, pdf));

        Assert.Multiple(() =>
        {
            Assert.That(context.Files.Find("account.jpg"), Is.SameAs(photo));
            Assert.That(context.Files.Find("account.pdf"), Is.SameAs(pdf));
            Assert.That(context.Files.Find("other.png"), Is.Null);
        });
    }

    /// <summary>A file the tool told it to forget is no longer found.</summary>
    [Test]
    public void AFileItWasToldToForget_IsNoLongerFound()
    {
        var context = Context();

        using var _ = SessionFiles.Use(new KeptFiles(File("account.jpg", FileMediaType.Jpeg)));
        context.Files.Forget("account.jpg");

        Assert.That(context.Files.Find("account.jpg"), Is.Null);
    }

    /// <summary>Outside a turn there are no files, and a forget is no error.</summary>
    [Test]
    public void OutsideATurn_ThereAreNoFiles()
    {
        var context = Context();

        Assert.Multiple(() =>
        {
            Assert.That(context.Files.Find("account.jpg"), Is.Null);
            Assert.DoesNotThrow(() => context.Files.Forget("account.jpg"));
        });
    }

    /// <summary>When the turn ends, its files go with it.</summary>
    [Test]
    public void WhenTheTurnEnds_ItsFilesGoWithIt()
    {
        var context = Context();

        using (SessionFiles.Use(new KeptFiles(File("account.jpg", FileMediaType.Jpeg))))
        {
        }

        Assert.That(context.Files.Find("account.jpg"), Is.Null);
    }
}
