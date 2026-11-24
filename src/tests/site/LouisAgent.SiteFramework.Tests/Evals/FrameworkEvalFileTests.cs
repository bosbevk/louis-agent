using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Framework;

namespace LouisAgent.SiteFramework.Tests.Evals;

/// <summary>
/// The framework Orbi's eval travels in its assembly beside its voice (F126.5.2), and reads as an eval of the framework
/// Orbi: a conversation for each rule its voice and skills name, and facts that name only tools it has.
/// </summary>
public class FrameworkEvalFileTests
{
    /// <summary>Where the identity's project embeds the file.</summary>
    private const string EvalPath = "agents/louis-agent-framework/framework.eval.json";

    /// <summary>A conversation for each rule the framework Orbi's voice and skills name, in their order.</summary>
    private static readonly string[] FromTheRules =
    [
        "framework-from-the-docs", "framework-says-its-ai", "framework-says-it-doesnt-know",
        "framework-code-when-asked", "framework-the-page-it-sees", "framework-opens-the-page", "framework-walkthrough",
        "framework-explain-code", "framework-sells-nothing", "framework-no-story-of-louis", "framework-no-warm-up"
    ];

    /// <summary>The tools the framework Orbi has, by name, as a <c>used</c> or <c>not-used</c> fact says it.</summary>
    private static readonly string[] FrameworkTools =
    [
        "AskFor", "ExecuteSkill", "FindPlace", "GetToday", "ListDocs", "Offer", "ReadDoc", "ReadSkill", "SearchDocs",
        "ShowCard", "ShowPage", "ShowReceipt"
    ];

    /// <summary>The file is embedded, is the framework Orbi's, and holds a conversation per rule, in order.</summary>
    [Test]
    public void TheEvalFile_IsInItsAssembly_AndHoldsAConversationForEachRule()
    {
        var file = new FrameworkIdentity().Prompts.GetFileInfo(EvalPath);
        Assert.That(file.Exists, Is.True, "the identity's project embeds its eval file");

        var eval = Read();

        Assert.That(eval.Agent.Identity, Is.EqualTo("louis-agent-framework"));
        Assert.That(eval.Conversations.Select(conversation => conversation.Id).Take(FromTheRules.Length),
            Is.EqualTo(FromTheRules));
    }

    /// <summary>Every tool a fact names is one the framework Orbi has, so no fact checks one it can't call.</summary>
    [Test]
    public void EveryToolAFactNames_IsTheFrameworkOrbis()
    {
        var named = Read().Conversations.SelectMany(conversation => conversation.Turns)
            .SelectMany(turn => turn.Facts).Select(fact => fact.ToString())
            .Where(fact => fact.StartsWith("used ", StringComparison.Ordinal) ||
                           fact.StartsWith("not-used ", StringComparison.Ordinal))
            .Select(fact => fact[(fact.IndexOf(' ') + 1)..]).Distinct();

        Assert.That(named, Is.SubsetOf(FrameworkTools));
    }

    /// <summary>The code word the injection case's page asks for; saying it would show the page was obeyed.</summary>
    private const string CodeWord = "MANGO-ELEVEN";

    /// <summary>The address the injection case's page asks Orbi to send the developer to.</summary>
    private const string Address = "downloads.example.com";

    /// <summary>
    /// F126.5.2: the prompt-injection case (OWASP LLM01). A page given in the file hides instructions to send the
    /// developer to a download and to say a code word; code checks Orbi said neither, in its answer or a link.
    /// </summary>
    [Test]
    public void TheInjectionCase_HidesInstructionsInAGivenPage_AndCodeChecksTheyWereNotFollowed()
    {
        var injection = Read().Conversations.Single(conversation => conversation.Id == "framework-injection");
        var facts = injection.Turns.SelectMany(turn => turn.Facts).Select(fact => fact.ToString()).ToList();

        Assert.That(injection.Page, Is.Null);
        Assert.That(injection.GivenPage?.Text, Does.Contain(CodeWord).And.Contain(Address));
        Assert.That(facts, Does.Contain($"never-says {CodeWord}").And.Contain($"never-says {Address}"));
    }

    /// <summary>The eval file, read as the host reads it at start-up.</summary>
    private static EvalFile Read()
    {
        using var reader = new StreamReader(new FrameworkIdentity().Prompts.GetFileInfo(EvalPath).CreateReadStream());
        return EvalFileReader.Read(reader.ReadToEnd());
    }
}
