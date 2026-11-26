using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Prompting.Headers.Agent.Library;

namespace LouisAgent.Core.Tests.Agent.Commands;

/// <summary>
/// S (CL15): what the user types that the model never sees: /skills, /approve, /reject and /usage are answered here;
/// anything else is a prompt. The drafts approved and rejected are SkillCommandsTests'.
/// </summary>
public class UserCommandsTests
{
    /// <summary>The commands over a skills folder that isn't there.</summary>
    private static UserCommands Commands()
    {
        var library = SkillLibrary.Read(Path.Combine(Path.GetTempPath(), "no-skills-here"));
        return new UserCommands(() => library, () => null);
    }

    /// <summary>Anything that isn't a command is a prompt, and gets no reply here.</summary>
    [TestCase("hello")]
    [TestCase("")]
    [TestCase("/unknown thing")]
    [TestCase("tell me about /usage")]
    public void APrompt_IsNotACommand(string input) =>
        Assert.That(Commands().Handle(input, sessionUsage: null), Is.Null);

    /// <summary>/usage without tracking says so.</summary>
    [Test]
    public void Usage_WithoutTracking_SaysSo() =>
        Assert.That(Commands().Handle("/usage", sessionUsage: null),
            Is.EqualTo("Usage isn't tracked in this session."));

    /// <summary>
    /// F97.1: /skills with nothing made says there are no drafts and no approved skills, not an empty list.
    /// </summary>
    [Test]
    public void Skills_WithNone_SaysSo() =>
        Assert.That(Commands().Handle("/skills", sessionUsage: null),
            Is.EqualTo("No drafts are waiting.\nNo skills are approved yet."));

    /// <summary>F97.1: /tools is gone with the agent-built tools, so it is a prompt like any other text.</summary>
    [Test]
    public void Tools_IsNoLongerACommand() =>
        Assert.That(Commands().Handle("  /TOOLS  ", sessionUsage: null), Is.Null);

    /// <summary>F97.1: /approve or /reject of a name no draft has says so, and where the drafts are listed.</summary>
    [TestCase("/approve line-count")]
    [TestCase("/reject line-count")]
    public void ANameWithNoDraft_SaysSo(string input) =>
        Assert.That(Commands().Handle(input, sessionUsage: null),
            Is.EqualTo("No draft named 'line-count' is waiting; /skills lists the drafts."));

    /// <summary>/approve or /reject with no name says how it is typed.</summary>
    [TestCase("/approve", "Usage: /approve <Name>")]
    [TestCase("/reject", "Usage: /reject <Name>")]
    public void NoName_SaysHowItIsTyped(string input, string usage) =>
        Assert.That(Commands().Handle(input, sessionUsage: null), Is.EqualTo(usage));
}
