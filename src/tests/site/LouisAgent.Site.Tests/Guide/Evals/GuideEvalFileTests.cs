using LouisAgent.Evals.Files.Json;
using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide.Evals;

/// <summary>
/// The guide's eval travels in its assembly beside its voice (F126.3.1), and reads as an eval of the guide.
/// </summary>
public class GuideEvalFileTests
{
    private const string EvalPath = "agents/louis-agent-guide/guide.eval.json";

    /// <summary>The guide's file is embedded, and holds its first conversation for the guide.</summary>
    [Test]
    public void TheGuidesEvalFile_IsInItsAssembly_AndHoldsOneConversationForTheGuide()
    {
        var file = new GuideIdentity().Prompts.GetFileInfo(EvalPath);
        Assert.That(file.Exists, Is.True, "the guide's project embeds its eval file");

        using var reader = new StreamReader(file.CreateReadStream());
        var eval = EvalFileReader.Read(reader.ReadToEnd());

        Assert.That(eval.Agent.Identity, Is.EqualTo("louis-agent-guide"));
        Assert.That(eval.Conversations.Select(conversation => conversation.Id), Is.EqualTo(new[] { "guide-what-is" }));
    }

    /// <summary>FX229: the guide names no doc, so no criterion of its eval asks it to.</summary>
    [Test]
    public void NoCriterion_AsksTheGuideToNameADoc()
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts.GetFileInfo(EvalPath).CreateReadStream());
        var eval = EvalFileReader.Read(reader.ReadToEnd());

        var criteria = eval.Conversations.SelectMany(conversation => conversation.Turns)
            .SelectMany(turn => turn.Criteria);

        Assert.That(criteria, Has.None.Contains("Names the doc"));
    }
}
