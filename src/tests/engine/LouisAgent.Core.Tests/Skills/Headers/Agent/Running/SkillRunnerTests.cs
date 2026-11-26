using System.Text.Json;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Running;
using LouisAgent.Core.Skills.Languages;

namespace LouisAgent.Core.Tests.Skills.Headers.Agent.Running;

/// <summary>
/// F97.1 step 4: a skill's script runs in its language with the call's arguments as JSON on stdin, never in its
/// command line or its environment, and what it printed, or why it failed, is the answer the model reads. Each run is
/// told apart as succeeded or not, since a draft is kept only when its example succeeds.
/// </summary>
public class SkillRunnerTests
{
    private static readonly JsonElement Arguments = JsonDocument.Parse("""{ "text": "a b" }""").RootElement;

    /// <summary>The script runs in the skill's language, with the arguments on stdin, inside the time limit.</summary>
    [Test]
    public void TheScript_RunsInItsLanguage_WithTheArgumentsOnStdin()
    {
        (ScriptLanguage Language, string Script, string Input, TimeSpan Limit)? asked = null;
        var runner = new SkillRunner((ScriptLanguage language, string script, string input, TimeSpan limit,
            out string? notInstalled) =>
        {
            asked = (language, script, input, limit);
            notInstalled = null;
            return new ProcessRunner.Result(0, "2\n", false);
        });

        var answer = runner.Run(WordCount(), Arguments);

        Assert.That(asked, Is.EqualTo((ScriptLanguage.Python, "/skills/word-count/scripts/count.py",
            Arguments.GetRawText(), SkillRunner.TimeLimit)));
        Assert.That(answer, Is.EqualTo(new SkillAnswer(true, "2")));
    }

    /// <summary>A run that didn't succeed says why, and is told apart from one that did.</summary>
    [TestCase(3, false, null, "[Exit Code 3]")]
    [TestCase(0, true, null, "60 seconds")]
    [TestCase(0, false, "Python isn't installed.", "Python isn't installed.")]
    public void ARunThatFailed_SaysWhy(int exitCode, bool timedOut, string? notInstalledReason, string named)
    {
        var runner = new SkillRunner((ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = notInstalledReason;
            return notInstalledReason is null ? new ProcessRunner.Result(exitCode, "oops", timedOut) : null;
        });

        var answer = runner.Run(WordCount(), Arguments);

        Assert.That((answer.Succeeded, answer.Text.Contains(named)), Is.EqualTo((false, true)), answer.Text);
    }

    /// <summary>A run that printed nothing succeeded, and says so rather than answering with nothing.</summary>
    [Test]
    public void ARunThatPrintedNothing_SaysSo()
    {
        var runner = new SkillRunner((ScriptLanguage _, string _, string _, TimeSpan _, out string? notInstalled) =>
        {
            notInstalled = null;
            return new ProcessRunner.Result(0, "  \n", false);
        });

        var answer = runner.Run(WordCount(), Arguments);

        Assert.That((answer.Succeeded, answer.Text), Is.EqualTo((true, answer.Text)));
        Assert.That(answer.Text, Does.Contain("word-count"));
    }

    /// <summary>A skill as the reader gives it, its script a Python file.</summary>
    private static Skill WordCount() => new("word-count", "Counts words.", "", "/skills/word-count",
        "/skills/word-count/scripts/count.py", ScriptLanguage.Python,
        new SkillParameters(Arguments, new Dictionary<string, SkillParameterType>(), []), SkillState.Approved);
}
