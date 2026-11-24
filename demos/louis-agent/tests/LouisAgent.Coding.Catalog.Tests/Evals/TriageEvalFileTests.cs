using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Coding.Tests.Evals;

/// <summary>
/// The triage manager's eval travels in its identity's assembly beside its voice (F126.5.3): a case for each path the
/// manager's voice names, on the sample order service, its routing proved in code by <c>routed-to</c> and
/// <c>continued</c>, and no case that reaches the coder, so a live run writes no code (Louis's pick, "Stop at the
/// decision").
/// </summary>
public class TriageEvalFileTests
{
    /// <summary>Where the identity's project embeds the file.</summary>
    private const string EvalPath = "agents/triage/triage.eval.json";

    /// <summary>A conversation for each path the manager's voice names, in the order it names them.</summary>
    private static readonly string[] FromTheVoice =
    [
        "triage-expected-is-ignored", "triage-payments-go-to-a-person", "triage-no-runbook-goes-to-a-person",
        "triage-a-bug-is-investigated-then-auto-fix", "triage-another-departments-is-handed-off",
        "triage-asks-the-investigator-again"
    ];

    /// <summary>The tools a fact may name: the manager's own, and AskAgent, which asks a role.</summary>
    private static readonly string[] ManagerTools = ["DecideCase", "RaiseToPerson", "ReadTriageRun", "AskAgent"];

    /// <summary>The file is embedded, is the triage manager's, and holds a conversation per path, in order.</summary>
    [Test]
    public void TheEvalFile_IsInItsAssembly_AndHoldsAConversationForEachPath()
    {
        Assert.That(new TriageIdentity().Prompts.GetFileInfo(EvalPath).Exists, Is.True,
            "the identity's project embeds its eval file");

        var eval = Read();

        Assert.That(eval.Agent.ToString(), Is.EqualTo("triage"));
        Assert.That(eval.Conversations.Select(conversation => conversation.Id), Is.EqualTo(FromTheVoice));
    }

    /// <summary>
    /// The routing is proved in code: the bug's case checks the investigator was asked, and the follow-up that it was
    /// asked again; every address a fact names is one of the team's own roles, never the coder.
    /// </summary>
    [Test]
    public void TheRouting_IsProvedInCode_AndNeverReachesTheCoder()
    {
        var eval = Read();
        var calls = new TriageIdentity().Calls.Select(call => call.ToString()).ToList();
        var addressed = Facts(eval).Where(fact =>
                fact.StartsWith("routed-to ", StringComparison.Ordinal) ||
                fact.StartsWith("continued ", StringComparison.Ordinal))
            .Select(fact => fact[(fact.IndexOf(' ') + 1)..]).Distinct().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(FactsOf(eval, "triage-a-bug-is-investigated-then-auto-fix"),
                Does.Contain("routed-to triage.investigator"));
            Assert.That(FactsOf(eval, "triage-asks-the-investigator-again"),
                Does.Contain("continued triage.investigator"));
            Assert.That(addressed, Is.SubsetOf(calls));
        });
    }

    /// <summary>Every tool a fact names is the manager's, so no fact checks a tool it can't call.</summary>
    [Test]
    public void EveryToolAFactNames_IsTheManagers()
    {
        var named = Facts(Read()).Where(fact =>
                fact.StartsWith("used ", StringComparison.Ordinal) ||
                fact.StartsWith("not-used ", StringComparison.Ordinal))
            .Select(fact => fact[(fact.IndexOf(' ') + 1)..]).Distinct();

        Assert.That(named, Is.SubsetOf(ManagerTools));
    }

    /// <summary>No case carries a path of the machine it was made on; its stack traces name the sample's.</summary>
    [Test]
    public void NoCase_CarriesAMachinesPath()
    {
        var asks = Read().Conversations.SelectMany(conversation => conversation.Turns).Select(turn => turn.Ask);

        Assert.That(asks, Has.None.Contain(":\\").And.None.Contain("/Users/").And.None.Contain("/home/"));
    }

    /// <summary>Every fact of every turn, as the file writes it.</summary>
    private static IEnumerable<string> Facts(EvalFile eval) =>
        eval.Conversations.SelectMany(conversation => conversation.Turns).SelectMany(turn => turn.Facts)
            .Select(fact => fact.ToString());

    /// <summary>The facts of one conversation's turns.</summary>
    private static IEnumerable<string> FactsOf(EvalFile eval, string id) =>
        eval.Conversations.Single(conversation => conversation.Id == id).Turns.SelectMany(turn => turn.Facts)
            .Select(fact => fact.ToString());

    /// <summary>The eval file, read as the host reads it at start-up.</summary>
    private static EvalFile Read()
    {
        using var reader = new StreamReader(new TriageIdentity().Prompts.GetFileInfo(EvalPath).CreateReadStream());
        return EvalFileReader.Read(reader.ReadToEnd());
    }
}
