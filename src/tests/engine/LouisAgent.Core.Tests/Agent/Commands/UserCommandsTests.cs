using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Composition.Tools.Scripts;

namespace LouisAgent.Core.Tests.Agent.Commands;

/// <summary>
/// S (CL15): what the user types that the model never sees: /tools, /approve, /reject and /usage are answered here;
/// anything else is a prompt. The approvals themselves are SkillAuthoringTests' and AgentBuiltToolsTests', and the
/// skills the agent made are SkillCommandsTests'.
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
        var tools = new AgentBuiltTools(new ToolRegistry(), supportsTools: true, root, python, powerShell, () => null,
            () => new HashSet<string>());
        var library = SkillLibrary.Read(Path.Combine(root, "no-skills-here"));
        return new UserCommands(tools, skills, () => library, () => null);
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

    /// <summary>/tools lists the waiting tools and says how to approve one.</summary>
    [Test]
    public void Tools_ListsThem_AndSaysHowToApprove() =>
        Assert.That(Commands().Handle("  /TOOLS  ", sessionUsage: null),
            Does.EndWith("Approve with /approve <Name>, discard with /reject <Name>."));

    /// <summary>/approve with nothing to approve is the tools' own reply, here that tool building is off.</summary>
    [Test]
    public void Approve_IsTheToolsOwnReply_HereThatToolBuildingIsOff() =>
        Assert.That(Commands().Handle("/approve", sessionUsage: null), Is.EqualTo("Tool building is not available."));
}
