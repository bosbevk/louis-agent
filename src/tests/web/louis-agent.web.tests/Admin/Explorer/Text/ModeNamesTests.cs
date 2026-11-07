using LouisAgent.Web.Admin.Explorer.Text;
using LouisAgent.Web.Admin.Explorer.Tree;

namespace LouisAgent.Web.Tests.Admin.Explorer.Text;

/// <summary>What the explorer calls a mode and its specialists.</summary>
public class ModeNamesTests
{
    /// <summary>A mode is the agent with the roles it handed work to, or the agent alone.</summary>
    [Test]
    public void AMode_IsNamedForItsRoles()
    {
        Assert.That(ModeNames.Who(Mode(SampleRole.Alpha, null, SampleRole.Beta, SampleRole.Alpha)),
            Is.EqualTo($"The agent with {SampleRole.Alpha} and {SampleRole.Beta}"));
        Assert.That(ModeNames.Who(Mode(null, "")), Is.EqualTo("The agent alone"));
    }

    /// <summary>The specialists' name is the roles across the modes, each once, or "specialist" with none.</summary>
    [Test]
    public void TheSpecialists_AreTheRolesAcrossTheModes()
    {
        Assert.That(ModeNames.Roles([Mode(SampleRole.Alpha), Mode(SampleRole.Beta, SampleRole.Alpha)]),
            Is.EqualTo($"{SampleRole.Alpha} / {SampleRole.Beta}"));
        Assert.That(ModeNames.Roles([Mode((string?)null)]), Is.EqualTo("specialist"));
    }

    /// <summary>A turn was answered by its role, or carried on a specialist's session, or by the agent.</summary>
    [Test]
    public void ATurnsAnswerer_IsItsRole_ThenTheSessionItCarriedOn_ThenTheAgent()
    {
        Assert.That(ModeNames.Answerer(new TurnNode
        {
            Role = SampleRole.Alpha,
            Continued = "s1"
        }),
            Is.EqualTo(SampleRole.Alpha));
        Assert.That(ModeNames.Answerer(new TurnNode { Continued = "s1" }), Is.EqualTo("continued · s1"));
        Assert.That(ModeNames.Answerer(new TurnNode()), Is.EqualTo("The agent itself"));
    }

    /// <summary>A mode with one conversation whose turns were answered by these roles.</summary>
    private static ModeNode Mode(params string?[] roles)
    {
        var conversation = new ConversationNode { Task = "chat" };
        conversation.Turns.AddRange(roles.Select((role, i) => new TurnNode
        {
            N = i + 1,
            Role = role
        }));
        var mode = new ModeNode { Name = SampleMode.Alpha };
        mode.Conversations.Add(conversation);
        return mode;
    }
}
