using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;

namespace LouisAgent.Evals.Tests.Files;

/// <summary>
/// The evals are found where the prompts are: every <c>.eval.json</c> in an agent's folder, among the files the
/// catalogue already hands over, read once and held by its name. A wrong one stops it at the first fault.
/// </summary>
public class EvalCatalogTests
{
    private const string GuidePath = "agents/an-identity/guide.eval.json";

    /// <summary>An eval file in an agent's folder is held by its name; the voice beside it isn't one.</summary>
    [Test]
    public void AnEvalFileInAnAgentsFolder_IsFoundAndHeldByItsName_AndAMarkdownFileIsNot()
    {
        var catalog = Catalog(Identity("an-identity", (GuidePath, Eval("an-identity", "guide-what-is"))));

        var evals = EvalCatalog.Read(catalog);

        Assert.That(evals.Names, Is.EqualTo(new[] { "guide" }));
        var guide = evals.Find("guide")!;
        Assert.That(guide.Agent.Identity, Is.EqualTo("an-identity"));
        Assert.That(guide.Conversations.Single().Id, Is.EqualTo("guide-what-is"));
    }

    /// <summary>Two files with one name, in two agents' folders, are refused, and the reason names both.</summary>
    [Test]
    public void TwoFilesWithOneName_AreRefused_NamingBoth()
    {
        const string otherPath = "agents/another/guide.eval.json";
        var catalog = Catalog(Identity("an-identity", (GuidePath, Eval("an-identity", "c1"))),
            Identity("another", (otherPath, Eval("another", "c2"))));

        var refused = Assert.Throws<EvalFileException>(() => EvalCatalog.Read(catalog));

        Assert.That(refused!.Message, Does.Contain(GuidePath).And.Contain(otherPath));
    }

    /// <summary>A wrong file is refused with the reader's own reason and the file's path.</summary>
    [Test]
    public void AWrongFile_IsRefusedWithTheReadersReason_AndItsPath()
    {
        const string wrong = """{"agent":"an-identity","conversations":[{"id":"c1","turns":""" +
                             """[{"ask":"Hi?","facts":["notused ReadDoc"]}]}]}""";
        var catalog = Catalog(Identity("an-identity", (GuidePath, wrong)));

        var refused = Assert.Throws<EvalFileException>(() => EvalCatalog.Read(catalog));

        Assert.That(refused!.Place, Is.EqualTo(GuidePath));
        Assert.That(refused.Message, Does.Contain(GuidePath).And.Contain("'notused ReadDoc' is no fact"));
    }

    /// <summary>A file whose agent this host doesn't have is refused, and the reason names the agent.</summary>
    [Test]
    public void AFileWhoseAgentTheHostDoesntHave_IsRefused_NamingTheAgent()
    {
        var catalog = Catalog(Identity("an-identity", (GuidePath, Eval("nobody-here", "c1"))));

        var refused = Assert.Throws<EvalFileException>(() => EvalCatalog.Read(catalog));

        Assert.That(refused!.Message, Does.Contain("'nobody-here'").And.Contain(GuidePath));
    }

    /// <summary>A name that isn't there is none, and the names come back in order.</summary>
    [Test]
    public void FindOfANameThatIsntThere_IsNull_AndTheNamesAreInOrder()
    {
        var catalog = Catalog(Identity("an-identity",
            ("agents/an-identity/zeta.eval.json", Eval("an-identity", "c1")),
            ("agents/an-identity/alpha.eval.json", Eval("an-identity", "c2"))));

        var evals = EvalCatalog.Read(catalog);

        Assert.That(evals.Find("guide"), Is.Null);
        Assert.That(evals.Names, Is.EqualTo(new[] { "alpha", "zeta" }));
    }

    /// <summary>A catalogue of these identities and no packs.</summary>
    private static AgentCatalog Catalog(params FakeIdentity[] identities) => new(identities, []);

    /// <summary>An identity whose files are its voice and these.</summary>
    private static FakeIdentity Identity(string name, params (string Path, string Text)[] files)
    {
        var all = files.ToDictionary(file => file.Path, file => file.Text);
        all[$"agents/{name}/voice.md"] = $"# {name}\n\nA voice.\n";
        return new FakeIdentity(name, provider: new FakePrompts(all));
    }

    /// <summary>An eval of one conversation of one turn, for this agent.</summary>
    private static string Eval(string agent, string conversation) =>
        $$"""{"agent":"{{agent}}","conversations":[{"id":"{{conversation}}","turns":""" +
        """[{"ask":"Hi?","facts":["no-error"]}]}]}""";
}
