using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Shell;

namespace LouisAgent.Core.Tests.Agent.Commands;

// S (CL15): what the user types that the model never sees: /tools, /approve, /reject and /usage are answered here; anything
// else is a prompt. The approvals themselves are SkillAuthoringTests' and AgentBuiltToolsTests'.
public class UserCommandsTests
{
    private static UserCommands Commands()
    {
        string root = Path.GetTempPath();
        var python = new PythonTools(root);
        var powerShell = new PowerShellTools(root);
        var skills = new SkillTools(new CompositeSkillProvider(), new AgentOptions { WorkspaceRoot = root }, root, python, powerShell);
        var tools = new AgentBuiltTools(new ToolRegistry(), supportsTools: true, root, python, powerShell, () => null, () => new HashSet<string>());
        return new UserCommands(tools, skills, () => null);
    }

    [TestCase("hello")]
    [TestCase("")]
    [TestCase("/unknown thing")]
    [TestCase("tell me about /usage")]
    public void APrompt_IsNotACommand(string input) =>
        Assert.That(Commands().Handle(input, sessionUsage: null), Is.Null);

    [Test]
    public void Usage_WithoutTracking_SaysSo() =>
        Assert.That(Commands().Handle("/usage", sessionUsage: null), Is.EqualTo("Usage isn't tracked in this session."));

    [Test]
    public void Tools_ListsThem_AndSaysHowToApprove() =>
        Assert.That(Commands().Handle("  /TOOLS  ", sessionUsage: null), Does.EndWith("Approve with /approve <Name>, discard with /reject <Name>."));

    [Test]
    public void Approve_IsTheToolsOwnReply_HereThatToolBuildingIsOff() =>
        Assert.That(Commands().Handle("/approve", sessionUsage: null), Is.EqualTo("Tool building is not available."));
}
