using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Composition.Tools.Scripts;

namespace LouisAgent.Core.Tests.Agent.Commands;

/// <summary>
/// S (CL15): what the user types that the model never sees: /skills, /approve, /reject and /usage are answered here;
/// anything else is a prompt. The skill files' approvals are SkillAuthoringTests', and the skills the agent made
/// SkillCommandsTests'.
/// </summary>
public class UserCommandsTests
{
    /// <summary>The commands over an empty temporary folder, with no skills folder.</summary>
    private static UserCommands Commands()
    {
        string root = Path.GetTempPath();
        var python = new PythonRunner(root);
        var powerShell = new PowerShellRunner(root);
        var options = new AgentOptions { WorkspaceRoot = root };
        var skills = new SkillTools(new CompositeSkillProvider(), options, root, python, powerShell);
        var library = SkillLibrary.Read(Path.Combine(root, "no-skills-here"));
        return new UserCommands(skills, () => library, () => null);
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

    /// <summary>
    /// /approve with no draft by that name is the skill files' own reply, here that authoring is off.
    /// </summary>
    [Test]
    public void Approve_WithNoDraft_IsTheSkillFilesOwnReply() =>
        Assert.That(Commands().Handle("/approve", sessionUsage: null),
            Is.EqualTo("Error: skill authoring is not available (no skills folder was found)."));
}
